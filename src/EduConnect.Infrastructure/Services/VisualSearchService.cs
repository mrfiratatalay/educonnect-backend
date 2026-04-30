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
    IHttpClientFactory httpClientFactory,
    ILogger<VisualSearchService> logger) : IVisualSearchService
{
    private const string VisualSearchPrompt =
        "Bu gorseli analiz et ve sadece JSON don. " +
        "JSON alani su olsun: " +
        "{\"productName\":\"...\",\"categoryLabel\":\"Elektronik|Ders Kitaplari|Kirtasiye|Etkinlik Biletleri|Diger\"," +
        "\"keywords\":[\"kw1\",\"kw2\",\"kw3\"],\"description\":\"...\",\"estimatedPriceRange\":\"...\",\"conditionLabel\":\"Sifir|Yeni gibi|Iyi|Orta\"}";

    private const double EmbeddingWeight = 0.55;
    private const double HeuristicWeight = 0.45;

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
            var scoreParts = ComputeScoreParts(product, analysis, keywordTerms, targetPrice);
            var heuristicScore = scoreParts.HeuristicScore;
            var embeddingScore = embeddingScores.Count > index ? embeddingScores.ElementAt(index) : 0.0;

            var hasProductEmbedding = embeddingScore > 0;
            var totalScore = queryEmbedding.Features.Count > 0 && hasProductEmbedding
                ? (heuristicScore * HeuristicWeight) + (embeddingScore * EmbeddingWeight)
                : heuristicScore;

            var signals = BuildMatchedSignals(product, analysis, keywordTerms, heuristicScore, embeddingScore);
            var breakdown = BuildBreakdown(product, analysis, keywordTerms, targetPrice, embeddingScore);

            return new
            {
                Product = product,
                Score = totalScore,
                scoreParts.TextScore,
                EmbeddingScore = embeddingScore,
                Signals = signals,
                Breakdown = breakdown
            };
        }).OrderByDescending(r => r.Score).ToList();

        var threshold = searchMode == "discovery" ? 0.32 : 0.42;
        var shortlisted = scoredResults
            .Where(r => r.Score >= threshold)
            .Where(r => searchMode == "discovery" || r.EmbeddingScore >= 0.55 || r.TextScore >= 0.08)
            .Take(request.MaxResults)
            .ToList();

        if (shortlisted.Count == 0 && searchMode == "discovery")
            shortlisted = scoredResults.Take(request.MaxResults).ToList();

        return shortlisted.Select((r, i) => new VisualSearchResultResponse
        {
            ProductId = r.Product.Id,
            Title = r.Product.Title,
            Description = r.Product.Description,
            Price = r.Product.Price,
            ImageUrl = r.Product.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault(),
            CategoryLabel = r.Product.Category?.Name ?? "Diğer",
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
            if (primaryImage is null)
            {
                candidateFeatures.Add([]);
                continue;
            }

            if (primaryImage.EmbeddingJson is not null)
            {
                try
                {
                    var features = JsonSerializer.Deserialize<double[]>(primaryImage.EmbeddingJson);
                    candidateFeatures.Add(features ?? []);
                    continue;
                }
                catch { /* fall through */ }
            }

            candidateFeatures.Add(await ExtractCandidateImageFeaturesAsync(primaryImage, cancellationToken));
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

    private async Task<IReadOnlyCollection<double>> ExtractCandidateImageFeaturesAsync(
        ProductImage image,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            var imageBytes = await httpClient.GetByteArrayAsync(image.Url, cancellationToken);
            var result = await visionEmbeddingService.ExtractFeaturesAsync(imageBytes, cancellationToken);
            if (result.Features.Count > 0)
            {
                var embeddingJson = JsonSerializer.Serialize(result.Features);
                await dbContext.ProductImages
                    .Where(productImage => productImage.Id == image.Id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(productImage => productImage.EmbeddingJson, embeddingJson),
                        cancellationToken);
            }
            return result.Features;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract on-demand visual embedding for product image {ImageId}", image.Id);
            return [];
        }
    }

    private static ScoreParts ComputeScoreParts(
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
        var heuristicScore = (textScore * 0.4) + (categoryScore * 0.3) + (conditionScore * 0.15) + (priceScore * 0.15);

        return new ScoreParts(heuristicScore, textScore);
    }

    private sealed record ScoreParts(double HeuristicScore, double TextScore);

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
            signals.Add("Görsel benzerlik");

        if (signals.Count == 0)
            signals.Add("Benzer ürün");

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
            items.Add(new VisualSearchBreakdownItem { Label = "Görsel", Value = (int)Math.Round(embeddingScore * 100) });

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
                a.ProductName = "Bilinmeyen ürün";

            if (string.IsNullOrWhiteSpace(a.Description))
                a.Description = "Görselden otomatik olarak çıkarılan bilgi sınırlı.";

            if (string.IsNullOrWhiteSpace(a.EstimatedPriceRange))
                a.EstimatedPriceRange = "Belirsiz";

            return a;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Gemini JSON parse failed during visual search analysis");
            return CreateFallbackAnalysis("Görsel analiz sonucu işlenemedi.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Visual search image analysis failed, falling back to heuristic-only search");
            return CreateFallbackAnalysis("Görsel analiz servisine geçici olarak ulaşılamadı.");
        }
    }

    private static VisualSearchAnalysis CreateFallbackAnalysis(string description) => new()
    {
        ProductName = "Bilinmeyen ürün",
        CategoryLabel = "Diğer",
        Description = description,
        ConditionLabel = "İyi",
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
        if (!string.IsNullOrWhiteSpace(analysis.CategoryLabel) && analysis.CategoryLabel != "Diğer") confidence += 10;
        confidence += Math.Min(analysis.Keywords.Count * 4, 16);
        if (results.Count > 0) confidence += (int)Math.Round(results.Take(3).Average(r => r.SimilarityScore) * 20);
        return Math.Clamp(confidence, 55, 96);
    }
}
