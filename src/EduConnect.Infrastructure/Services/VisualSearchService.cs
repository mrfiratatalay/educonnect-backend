using System.Diagnostics;
using System.Text.Json;
using EduConnect.Application.Contracts.VisualSearch;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduConnect.Infrastructure.Services;

public sealed class VisualSearchService(
    IGeminiApiService geminiApiService,
    IVisionEmbeddingService visionEmbeddingService,
    AppDbContext dbContext,
    ILogger<VisualSearchService> logger) : IVisualSearchService
{
    private const string VisualSearchPrompt =
        "Bu gorseli analiz et ve sadece JSON don. " +
        "JSON alani su olsun: " +
        "{\"productName\":\"...\",\"categoryLabel\":\"Elektronik|Ders Kitaplari|Kirtasiye|Etkinlik Biletleri|Diger\"," +
        "\"keywords\":[\"kw1\",\"kw2\",\"kw3\"],\"description\":\"...\",\"estimatedPriceRange\":\"...\",\"conditionLabel\":\"Sifir|Yeni gibi|Iyi|Orta\"}";

    private const double EmbeddingWeight = 0.35;
    private const double HeuristicWeight = 0.65;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<VisualSearchSearchResponse> SearchAsync(
        byte[] imageBytes, string mimeType,
        VisualSearchSearchRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sanitizedRequest = SanitizeRequest(request);

        var analysisTask = AnalyzeImageAsync(imageBytes, mimeType, cancellationToken);
        var embeddingTask = visionEmbeddingService.ExtractFeaturesAsync(imageBytes, cancellationToken);

        await Task.WhenAll(analysisTask, embeddingTask);

        var analysis = analysisTask.Result;
        var queryEmbedding = embeddingTask.Result;

        var candidateQuery = dbContext.Products
            .AsNoTracking().AsSplitQuery()
            .Where(p => p.IsActive && p.Images.Any())
            .Include(p => p.Images).Include(p => p.Category).Include(p => p.Seller)
            .AsQueryable();

        var candidateCount = await candidateQuery.CountAsync(cancellationToken);
        candidateQuery = ApplyFilters(candidateQuery, sanitizedRequest);

        var candidates = await candidateQuery.ToListAsync(cancellationToken);
        var results = await RankProductsAsync(candidates, analysis, sanitizedRequest, queryEmbedding, cancellationToken);

        analysis.Confidence = CalculateConfidence(analysis, results);
        stopwatch.Stop();

        return new VisualSearchSearchResponse
        {
            Analysis = analysis,
            Results = results,
            TotalFound = results.Count,
            CandidateCount = candidateCount,
            FilteredCount = candidates.Count,
            ElapsedMs = stopwatch.ElapsedMilliseconds
        };
    }

    private async Task<IReadOnlyCollection<VisualSearchResultResponse>> RankProductsAsync(
        IReadOnlyCollection<Product> candidates,
        VisualSearchAnalysis analysis,
        VisualSearchSearchRequest request,
        VisionEmbeddingResult queryEmbedding,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0) return [];

        var searchMode = VisualSearchScoringHelper.NormalizeMode(request.Mode);
        var keywordTerms = VisualSearchScoringHelper.BuildKeywords(analysis);
        var targetPrice = VisualSearchScoringHelper.ParseEstimatedPrice(analysis.EstimatedPriceRange);

        var embeddingScores = await ComputeEmbeddingSimilaritiesAsync(queryEmbedding, candidates, cancellationToken);

        var scoredResults = candidates.Select((product, index) =>
        {
            var heuristicScore = ComputeHeuristicScore(product, analysis, keywordTerms, targetPrice);
            var embeddingScore = embeddingScores.Count > index ? embeddingScores.ElementAt(index) : 0.0;

            var totalScore = queryEmbedding.Features.Count > 0
                ? (heuristicScore * HeuristicWeight) + (embeddingScore * EmbeddingWeight)
                : heuristicScore;

            var signals = BuildMatchedSignals(product, analysis, keywordTerms, heuristicScore, embeddingScore);
            var breakdown = BuildBreakdown(product, analysis, keywordTerms, targetPrice, embeddingScore);

            return new { Product = product, Score = totalScore, Signals = signals, Breakdown = breakdown };
        }).OrderByDescending(r => r.Score).ToList();

        var threshold = searchMode == "discovery" ? 0.24 : 0.34;
        var shortlisted = scoredResults.Where(r => r.Score >= threshold).Take(request.MaxResults).ToList();

        if (shortlisted.Count == 0)
            shortlisted = scoredResults.Take(request.MaxResults).ToList();

        return shortlisted.Select((r, i) => new VisualSearchResultResponse
        {
            ProductId = r.Product.Id,
            Title = r.Product.Title,
            Description = r.Product.Description,
            Price = r.Product.Price,
            ImageUrl = r.Product.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault(),
            CategoryLabel = r.Product.Category?.Name ?? "Diger",
            SellerName = r.Product.Seller.FullName,
            Condition = r.Product.Condition,
            ConditionLabel = VisualSearchScoringHelper.GetConditionLabel(r.Product.Condition),
            City = r.Product.City,
            SimilarityScore = Math.Round(r.Score, 4),
            Rank = i + 1,
            MatchedSignals = r.Signals,
            Breakdown = r.Breakdown
        }).ToArray();
    }

    private async Task<IReadOnlyCollection<double>> ComputeEmbeddingSimilaritiesAsync(
        VisionEmbeddingResult queryEmbedding,
        IReadOnlyCollection<Product> candidates,
        CancellationToken cancellationToken)
    {
        if (queryEmbedding.Features.Count == 0) return [];

        var candidateFeatures = new List<IReadOnlyCollection<double>>();
        foreach (var product in candidates)
        {
            var primaryImage = product.Images.OrderBy(img => img.SortOrder).FirstOrDefault();
            if (primaryImage?.EmbeddingJson is not null)
            {
                try
                {
                    var features = JsonSerializer.Deserialize<double[]>(primaryImage.EmbeddingJson);
                    candidateFeatures.Add(features ?? []);
                    continue;
                }
                catch { /* fall through */ }
            }
            candidateFeatures.Add([]);
        }

        if (candidateFeatures.All(f => f.Count == 0)) return [];

        try
        {
            return await visionEmbeddingService.ComputeSimilarityAsync(
                queryEmbedding.Features, candidateFeatures, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Embedding similarity computation failed");
            return [];
        }
    }

    private static double ComputeHeuristicScore(
        Product product, VisualSearchAnalysis analysis,
        IReadOnlyCollection<string> keywordTerms, decimal? targetPrice)
    {
        var title = VisualSearchScoringHelper.NormalizeForSearch(product.Title);
        var desc = VisualSearchScoringHelper.NormalizeForSearch(product.Description);
        var cat = VisualSearchScoringHelper.NormalizeForSearch(product.Category?.Name ?? "");
        var nameTokens = VisualSearchScoringHelper.Tokenize(analysis.ProductName);

        var categoryScore = VisualSearchScoringHelper.GetCategoryScore(cat, analysis.CategoryLabel);
        var textScore = VisualSearchScoringHelper.GetTextScore(title, desc, keywordTerms, nameTokens);
        var conditionScore = VisualSearchScoringHelper.GetConditionScore(product.Condition, analysis.ConditionLabel);
        var priceScore = VisualSearchScoringHelper.GetPriceScore(product.Price, targetPrice);

        return (textScore * 0.4) + (categoryScore * 0.3) + (conditionScore * 0.15) + (priceScore * 0.15);
    }

    private static IReadOnlyCollection<string> BuildMatchedSignals(
        Product product, VisualSearchAnalysis analysis,
        IReadOnlyCollection<string> keywordTerms, double heuristicScore, double embeddingScore)
    {
        var signals = new List<string>();
        var title = VisualSearchScoringHelper.NormalizeForSearch(product.Title);
        var desc = VisualSearchScoringHelper.NormalizeForSearch(product.Description);
        var cat = VisualSearchScoringHelper.NormalizeForSearch(product.Category?.Name ?? "");

        if (VisualSearchScoringHelper.GetCategoryScore(cat, analysis.CategoryLabel) >= 0.7)
            signals.Add("Kategori");

        var matchedKw = keywordTerms.FirstOrDefault(kw =>
            title.Contains(kw, StringComparison.Ordinal) || desc.Contains(kw, StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(matchedKw))
            signals.Add(VisualSearchScoringHelper.ToTitleCase(matchedKw));

        if (embeddingScore >= 0.7)
            signals.Add("Gorsel benzerlik");

        if (signals.Count == 0)
            signals.Add("Benzer urun");

        return signals.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToArray();
    }

    private static IReadOnlyCollection<VisualSearchBreakdownItem> BuildBreakdown(
        Product product, VisualSearchAnalysis analysis,
        IReadOnlyCollection<string> keywordTerms, decimal? targetPrice, double embeddingScore)
    {
        var title = VisualSearchScoringHelper.NormalizeForSearch(product.Title);
        var desc = VisualSearchScoringHelper.NormalizeForSearch(product.Description);
        var cat = VisualSearchScoringHelper.NormalizeForSearch(product.Category?.Name ?? "");
        var nameTokens = VisualSearchScoringHelper.Tokenize(analysis.ProductName);

        var items = new List<VisualSearchBreakdownItem>
        {
            new() { Label = "Kategori", Value = (int)Math.Round(VisualSearchScoringHelper.GetCategoryScore(cat, analysis.CategoryLabel) * 100) },
            new() { Label = "Metin", Value = (int)Math.Round(VisualSearchScoringHelper.GetTextScore(title, desc, keywordTerms, nameTokens) * 100) },
            new() { Label = "Durum", Value = (int)Math.Round(VisualSearchScoringHelper.GetConditionScore(product.Condition, analysis.ConditionLabel) * 100) },
            new() { Label = "Fiyat", Value = (int)Math.Round(VisualSearchScoringHelper.GetPriceScore(product.Price, targetPrice) * 100) },
        };

        if (embeddingScore > 0)
            items.Add(new VisualSearchBreakdownItem { Label = "Gorsel", Value = (int)Math.Round(embeddingScore * 100) });

        return items;
    }

    private async Task<VisualSearchAnalysis> AnalyzeImageAsync(
        byte[] imageBytes, string mimeType, CancellationToken cancellationToken)
    {
        try
        {
            var rawJson = await geminiApiService.AnalyzeImageAsync(imageBytes, mimeType, VisualSearchPrompt, cancellationToken);
            var cleanJson = VisualSearchScoringHelper.CleanJsonResponse(rawJson);
            var a = JsonSerializer.Deserialize<VisualSearchAnalysis>(cleanJson, JsonOptions) ?? new VisualSearchAnalysis();

            a.ProductName = (a.ProductName ?? string.Empty).Trim();
            a.CategoryLabel = VisualSearchScoringHelper.NormalizeCategoryLabel(a.CategoryLabel ?? string.Empty);
            a.Description = (a.Description ?? string.Empty).Trim();
            a.EstimatedPriceRange = (a.EstimatedPriceRange ?? string.Empty).Trim();
            a.ConditionLabel = VisualSearchScoringHelper.NormalizeConditionLabel(a.ConditionLabel ?? string.Empty);
            a.Keywords = (a.Keywords ?? [])
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList();

            if (string.IsNullOrWhiteSpace(a.ProductName))
                a.ProductName = "Bilinmeyen urun";

            if (string.IsNullOrWhiteSpace(a.Description))
                a.Description = "Gorselden otomatik olarak cikarilan bilgi sinirli.";

            if (string.IsNullOrWhiteSpace(a.EstimatedPriceRange))
                a.EstimatedPriceRange = "Belirsiz";

            return a;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Gemini JSON parse failed during visual search analysis");
            return CreateFallbackAnalysis("Gorsel analiz sonucu islenemedi.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Visual search image analysis failed, falling back to heuristic-only search");
            return CreateFallbackAnalysis("Gorsel analiz servisine gecici olarak ulasilamadi.");
        }
    }

    private static VisualSearchAnalysis CreateFallbackAnalysis(string description) => new()
    {
        ProductName = "Bilinmeyen urun",
        CategoryLabel = "Diger",
        Description = description,
        ConditionLabel = "Iyi",
        EstimatedPriceRange = "Belirsiz",
        Keywords = []
    };

    private static VisualSearchSearchRequest SanitizeRequest(VisualSearchSearchRequest request) => new()
    {
        MaxResults = Math.Clamp(request.MaxResults, 1, 20),
        CategoryId = request.CategoryId,
        MinPrice = request.MinPrice,
        MaxPrice = request.MaxPrice,
        City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
        Mode = VisualSearchScoringHelper.NormalizeMode(request.Mode)
    };

    private static IQueryable<Product> ApplyFilters(IQueryable<Product> query, VisualSearchSearchRequest request)
    {
        if (request.CategoryId.HasValue) query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        if (request.MinPrice.HasValue) query = query.Where(p => p.Price >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue) query = query.Where(p => p.Price <= request.MaxPrice.Value);
        if (!string.IsNullOrWhiteSpace(request.City)) query = query.Where(p => p.City == request.City);
        return query;
    }

    private static int CalculateConfidence(VisualSearchAnalysis analysis, IReadOnlyCollection<VisualSearchResultResponse> results)
    {
        var confidence = 55;
        if (!string.IsNullOrWhiteSpace(analysis.CategoryLabel) && analysis.CategoryLabel != "Diger") confidence += 10;
        confidence += Math.Min(analysis.Keywords.Count * 4, 16);
        if (results.Count > 0) confidence += (int)Math.Round(results.Take(3).Average(r => r.SimilarityScore) * 20);
        return Math.Clamp(confidence, 55, 96);
    }
}
