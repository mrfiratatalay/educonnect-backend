using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EduConnect.Application.Contracts.VisualSearch;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Domain.Enums;
using EduConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduConnect.Infrastructure.Services;

public sealed partial class VisualSearchService(
    IGeminiApiService geminiApiService,
    AppDbContext dbContext,
    ILogger<VisualSearchService> logger) : IVisualSearchService
{
    private const string VisualSearchPrompt =
        "Bu gorseli analiz et ve sadece JSON don. " +
        "JSON alani su olsun: " +
        "{\"productName\":\"...\",\"categoryLabel\":\"Elektronik|Ders Kitaplari|Kirtasiye|Etkinlik Biletleri|Diger\"," +
        "\"keywords\":[\"kw1\",\"kw2\",\"kw3\"],\"description\":\"...\",\"estimatedPriceRange\":\"...\",\"conditionLabel\":\"Sifir|Yeni gibi|Iyi|Orta\"}";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<ProductCondition, double> ConditionWeights = new()
    {
        [ProductCondition.New] = 1,
        [ProductCondition.LikeNew] = 0.88,
        [ProductCondition.Good] = 0.72,
        [ProductCondition.Fair] = 0.54
    };

    public async Task<VisualSearchSearchResponse> SearchAsync(
        byte[] imageBytes,
        string mimeType,
        VisualSearchSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sanitizedRequest = SanitizeRequest(request);
        var analysis = await AnalyzeImageAsync(imageBytes, mimeType, cancellationToken);

        var candidateQuery = dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Where(product => product.IsActive && product.Images.Any())
            .Include(product => product.Images)
            .Include(product => product.Category)
            .Include(product => product.Seller)
            .AsQueryable();

        var candidateCount = await candidateQuery.CountAsync(cancellationToken);
        candidateQuery = ApplyFilters(candidateQuery, sanitizedRequest);

        var candidates = await candidateQuery.ToListAsync(cancellationToken);
        var results = RankProducts(candidates, analysis, sanitizedRequest);

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

    private async Task<VisualSearchAnalysis> AnalyzeImageAsync(
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken)
    {
        var rawJson = await geminiApiService.AnalyzeImageAsync(
            imageBytes,
            mimeType,
            VisualSearchPrompt,
            cancellationToken);

        var cleanJson = CleanJsonResponse(rawJson);

        try
        {
            var analysis = JsonSerializer.Deserialize<VisualSearchAnalysis>(cleanJson, JsonOptions)
                           ?? new VisualSearchAnalysis();

            analysis.ProductName = analysis.ProductName.Trim();
            analysis.CategoryLabel = NormalizeCategoryLabel(analysis.CategoryLabel);
            analysis.Description = analysis.Description.Trim();
            analysis.EstimatedPriceRange = analysis.EstimatedPriceRange.Trim();
            analysis.ConditionLabel = NormalizeConditionLabel(analysis.ConditionLabel);
            analysis.Keywords = analysis.Keywords
                .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
                .Select(keyword => keyword.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            return analysis;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Gemini JSON parse failed. Raw: {RawJson}", cleanJson);

            return new VisualSearchAnalysis
            {
                ProductName = "Bilinmeyen urun",
                CategoryLabel = "Diger",
                Description = "Gorsel analiz sonucu islenemedi.",
                ConditionLabel = "Iyi",
                EstimatedPriceRange = "Belirsiz"
            };
        }
    }

    private static VisualSearchSearchRequest SanitizeRequest(VisualSearchSearchRequest request)
    {
        return new VisualSearchSearchRequest
        {
            MaxResults = Math.Clamp(request.MaxResults, 1, 20),
            CategoryId = request.CategoryId,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            Mode = NormalizeMode(request.Mode)
        };
    }

    private static IQueryable<Product> ApplyFilters(
        IQueryable<Product> query,
        VisualSearchSearchRequest request)
    {
        if (request.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == request.CategoryId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(product => product.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(product => product.Price <= request.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(product => product.City == request.City);
        }

        return query;
    }

    private static IReadOnlyCollection<VisualSearchResultResponse> RankProducts(
        IReadOnlyCollection<Product> candidates,
        VisualSearchAnalysis analysis,
        VisualSearchSearchRequest request)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var searchMode = NormalizeMode(request.Mode);
        var keywordTerms = BuildKeywords(analysis);
        var targetPrice = ParseEstimatedPrice(analysis.EstimatedPriceRange);

        var scoredResults = candidates
            .Select(product => ScoreProduct(product, analysis, keywordTerms, targetPrice))
            .OrderByDescending(result => result.Score)
            .ToList();

        var threshold = searchMode == "discovery" ? 0.24 : 0.34;
        var shortlisted = scoredResults
            .Where(result => result.Score >= threshold)
            .Take(request.MaxResults)
            .ToList();

        if (shortlisted.Count == 0)
        {
            shortlisted = scoredResults.Take(request.MaxResults).ToList();
        }

        return shortlisted
            .Select((result, index) => new VisualSearchResultResponse
            {
                ProductId = result.Product.Id,
                Title = result.Product.Title,
                Description = result.Product.Description,
                Price = result.Product.Price,
                ImageUrl = result.Product.Images.OrderBy(image => image.SortOrder).Select(image => image.Url).FirstOrDefault(),
                CategoryLabel = result.Product.Category?.Name ?? "Diger",
                SellerName = result.Product.Seller.FullName,
                Condition = result.Product.Condition,
                ConditionLabel = GetConditionLabel(result.Product.Condition),
                City = result.Product.City,
                SimilarityScore = Math.Round(result.Score, 4),
                Rank = index + 1,
                MatchedSignals = result.MatchedSignals,
                Breakdown = result.Breakdown
            })
            .ToArray();
    }

    private static ScoredProduct ScoreProduct(
        Product product,
        VisualSearchAnalysis analysis,
        IReadOnlyCollection<string> keywordTerms,
        decimal? targetPrice)
    {
        var normalizedTitle = NormalizeForSearch(product.Title);
        var normalizedDescription = NormalizeForSearch(product.Description);
        var normalizedCategory = NormalizeForSearch(product.Category?.Name ?? string.Empty);
        var productNameTokens = Tokenize(analysis.ProductName);

        var categoryScore = GetCategoryScore(normalizedCategory, analysis.CategoryLabel);
        var textScore = GetTextScore(normalizedTitle, normalizedDescription, keywordTerms, productNameTokens);
        var conditionScore = GetConditionScore(product.Condition, analysis.ConditionLabel);
        var priceScore = GetPriceScore(product.Price, targetPrice);

        var totalScore =
            (textScore * 0.4) +
            (categoryScore * 0.3) +
            (conditionScore * 0.15) +
            (priceScore * 0.15);

        var matchedSignals = new List<string>();

        if (categoryScore >= 0.7)
        {
            matchedSignals.Add("Kategori");
        }

        var matchedKeyword = keywordTerms.FirstOrDefault(keyword =>
            normalizedTitle.Contains(keyword, StringComparison.Ordinal) ||
            normalizedDescription.Contains(keyword, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(matchedKeyword))
        {
            matchedSignals.Add(ToTitleCase(matchedKeyword));
        }

        if (conditionScore >= 0.8)
        {
            matchedSignals.Add("Durum");
        }

        if (priceScore >= 0.72)
        {
            matchedSignals.Add("Fiyat");
        }

        if (matchedSignals.Count == 0)
        {
            matchedSignals.Add("Benzer urun");
        }

        var breakdown = new[]
        {
            new VisualSearchBreakdownItem { Label = "Kategori", Value = (int)Math.Round(categoryScore * 100) },
            new VisualSearchBreakdownItem { Label = "Metin", Value = (int)Math.Round(textScore * 100) },
            new VisualSearchBreakdownItem { Label = "Durum", Value = (int)Math.Round(conditionScore * 100) },
            new VisualSearchBreakdownItem { Label = "Fiyat", Value = (int)Math.Round(priceScore * 100) }
        };

        return new ScoredProduct(
            product,
            totalScore,
            matchedSignals.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToArray(),
            breakdown);
    }

    private static IReadOnlyCollection<string> BuildKeywords(VisualSearchAnalysis analysis)
    {
        var keywords = new List<string>();
        keywords.AddRange(Tokenize(analysis.ProductName));
        keywords.AddRange(analysis.Keywords.Select(NormalizeForSearch));

        return keywords
            .Where(keyword => keyword.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    private static double GetCategoryScore(string normalizedCategory, string analysisCategory)
    {
        var normalizedAnalysisCategory = NormalizeForSearch(analysisCategory);
        if (string.IsNullOrWhiteSpace(normalizedAnalysisCategory) || normalizedAnalysisCategory == "diger")
        {
            return 0.45;
        }

        if (normalizedCategory.Contains(normalizedAnalysisCategory, StringComparison.Ordinal))
        {
            return 1;
        }

        if ((normalizedCategory.Contains("kitap", StringComparison.Ordinal) && normalizedAnalysisCategory.Contains("kitap", StringComparison.Ordinal)) ||
            (normalizedCategory.Contains("elektronik", StringComparison.Ordinal) && normalizedAnalysisCategory.Contains("elektronik", StringComparison.Ordinal)) ||
            (normalizedCategory.Contains("kirtasiye", StringComparison.Ordinal) && normalizedAnalysisCategory.Contains("kirtasiye", StringComparison.Ordinal)) ||
            (normalizedCategory.Contains("etkinlik", StringComparison.Ordinal) && normalizedAnalysisCategory.Contains("bilet", StringComparison.Ordinal)))
        {
            return 0.82;
        }

        return 0.18;
    }

    private static double GetTextScore(
        string normalizedTitle,
        string normalizedDescription,
        IReadOnlyCollection<string> keywords,
        IReadOnlyCollection<string> productNameTokens)
    {
        var searchableText = $"{normalizedTitle} {normalizedDescription}";
        var keywordHits = keywords.Count(keyword => searchableText.Contains(keyword, StringComparison.Ordinal));
        var productNameHits = productNameTokens.Count(token => normalizedTitle.Contains(token, StringComparison.Ordinal));

        var keywordScore = keywords.Count == 0 ? 0.2 : (double)keywordHits / keywords.Count;
        var productNameScore = productNameTokens.Count == 0 ? 0.2 : (double)productNameHits / productNameTokens.Count;

        return Math.Clamp((keywordScore * 0.65) + (productNameScore * 0.35), 0, 1);
    }

    private static double GetConditionScore(ProductCondition productCondition, string conditionLabel)
    {
        var requestedCondition = ParseCondition(conditionLabel) ?? ProductCondition.Good;
        var distance = Math.Abs(ConditionWeights[productCondition] - ConditionWeights[requestedCondition]);
        return Math.Clamp(1 - (distance / 0.5), 0.2, 1);
    }

    private static double GetPriceScore(decimal price, decimal? targetPrice)
    {
        if (!targetPrice.HasValue || targetPrice.Value <= 0)
        {
            return 0.55;
        }

        var deltaRatio = Math.Abs(price - targetPrice.Value) / targetPrice.Value;
        return Math.Clamp(1 - (double)deltaRatio, 0.15, 1);
    }

    private static int CalculateConfidence(
        VisualSearchAnalysis analysis,
        IReadOnlyCollection<VisualSearchResultResponse> results)
    {
        var confidence = 55;

        if (!string.IsNullOrWhiteSpace(analysis.CategoryLabel) && analysis.CategoryLabel != "Diger")
        {
            confidence += 10;
        }

        confidence += Math.Min(analysis.Keywords.Count * 4, 16);

        if (results.Count > 0)
        {
            confidence += (int)Math.Round(results.Take(3).Average(result => result.SimilarityScore) * 20);
        }

        return Math.Clamp(confidence, 55, 96);
    }

    private static decimal? ParseEstimatedPrice(string estimatedPriceRange)
    {
        if (string.IsNullOrWhiteSpace(estimatedPriceRange))
        {
            return null;
        }

        var matches = NumberPattern().Matches(estimatedPriceRange);
        var numbers = matches
            .Select(match => decimal.TryParse(match.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : (decimal?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        if (numbers.Length == 0)
        {
            return null;
        }

        return numbers.Average();
    }

    private static string NormalizeCategoryLabel(string category)
    {
        var normalized = NormalizeForSearch(category);

        if (normalized.Contains("elektronik", StringComparison.Ordinal))
        {
            return "Elektronik";
        }

        if (normalized.Contains("kitap", StringComparison.Ordinal) || normalized.Contains("ders materyal", StringComparison.Ordinal))
        {
            return "Ders Kitaplari";
        }

        if (normalized.Contains("kirtasiye", StringComparison.Ordinal))
        {
            return "Kirtasiye";
        }

        if (normalized.Contains("etkinlik", StringComparison.Ordinal) || normalized.Contains("bilet", StringComparison.Ordinal))
        {
            return "Etkinlik Biletleri";
        }

        return string.IsNullOrWhiteSpace(category) ? "Diger" : category.Trim();
    }

    private static string NormalizeConditionLabel(string condition)
    {
        return ParseCondition(condition) switch
        {
            ProductCondition.New => "Sifir",
            ProductCondition.LikeNew => "Yeni gibi",
            ProductCondition.Good => "Iyi",
            ProductCondition.Fair => "Orta",
            _ => string.IsNullOrWhiteSpace(condition) ? "Iyi" : condition.Trim()
        };
    }

    private static ProductCondition? ParseCondition(string condition)
    {
        var normalized = NormalizeForSearch(condition);

        if (normalized.Contains("sifir", StringComparison.Ordinal) || normalized.Contains("yeni", StringComparison.Ordinal))
        {
            return normalized.Contains("gibi", StringComparison.Ordinal) ? ProductCondition.LikeNew : ProductCondition.New;
        }

        if (normalized.Contains("iyi", StringComparison.Ordinal))
        {
            return ProductCondition.Good;
        }

        if (normalized.Contains("orta", StringComparison.Ordinal) || normalized.Contains("eski", StringComparison.Ordinal))
        {
            return ProductCondition.Fair;
        }

        return null;
    }

    private static string GetConditionLabel(ProductCondition condition)
    {
        return condition switch
        {
            ProductCondition.New => "Sifir",
            ProductCondition.LikeNew => "Yeni gibi",
            ProductCondition.Good => "Iyi",
            ProductCondition.Fair => "Orta",
            _ => "Bilinmiyor"
        };
    }

    private static string NormalizeMode(string? mode)
    {
        return string.Equals(mode, "discovery", StringComparison.OrdinalIgnoreCase)
            ? "discovery"
            : "strict";
    }

    private static IReadOnlyCollection<string> Tokenize(string value)
    {
        return NormalizeForSearch(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeForSearch(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (char.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal);
    }

    private static string CleanJsonResponse(string raw)
    {
        if (raw.Contains("```json", StringComparison.Ordinal))
        {
            var start = raw.IndexOf("```json", StringComparison.Ordinal) + 7;
            var end = raw.LastIndexOf("```", StringComparison.Ordinal);
            if (end > start) return raw[start..end].Trim();
        }

        if (raw.Contains("```", StringComparison.Ordinal))
        {
            var start = raw.IndexOf("```", StringComparison.Ordinal) + 3;
            var end = raw.LastIndexOf("```", StringComparison.Ordinal);
            if (end > start) return raw[start..end].Trim();
        }

        return raw.Trim();
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
    }

    [GeneratedRegex(@"\d+(?:[.,]\d+)?", RegexOptions.Compiled)]
    private static partial Regex NumberPattern();

    private sealed record ScoredProduct(
        Product Product,
        double Score,
        IReadOnlyCollection<string> MatchedSignals,
        IReadOnlyCollection<VisualSearchBreakdownItem> Breakdown);
}
