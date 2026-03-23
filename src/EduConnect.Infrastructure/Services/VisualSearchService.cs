using System.Security.Cryptography;
using System.Text.Json;
using EduConnect.Application.Contracts.VisualSearch;
using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduConnect.Infrastructure.Services;

public sealed class VisualSearchService(
    IGeminiApiService geminiApiService,
    AppDbContext dbContext,
    ILogger<VisualSearchService> logger) : IVisualSearchService
{
    private const string VisualSearchPrompt =
        "Bu görseli analiz et ve aşağıdaki JSON formatında yanıt ver. " +
        "SADECE JSON döndür, başka hiçbir şey yazma. " +
        "{\"productName\": \"Ürünün tam adı (marka+model)\", " +
        "\"category\": \"Elektronik|Kitap|Ders Materyali|Giyim|Aksesuar|Diğer\", " +
        "\"keywords\": [\"arama kelimesi 1\", \"kw2\", \"kw3\", \"kw4\", \"kw5\"], " +
        "\"description\": \"2-3 cümle açıklama\", " +
        "\"estimatedPriceRange\": \"Türkiye ikinci el fiyatı\", " +
        "\"condition\": \"Yeni|İyi|Orta|Eski\"}";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<VisualSearchAnalysisResponse> SearchByImageAsync(
        byte[] imageBytes,
        string mimeType,
        int maxResults = 8,
        CancellationToken cancellationToken = default)
    {
        var rawJson = await geminiApiService.AnalyzeImageAsync(
            imageBytes, mimeType, VisualSearchPrompt, cancellationToken);

        var cleanJson = CleanJsonResponse(rawJson);

        GeminiImageAnalysis analysis;
        try
        {
            analysis = JsonSerializer.Deserialize<GeminiImageAnalysis>(cleanJson, JsonOptions)
                       ?? new GeminiImageAnalysis();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Gemini JSON parse failed. Raw: {RawJson}", cleanJson);
            analysis = new GeminiImageAnalysis
            {
                ProductName = "Bilinmeyen Ürün",
                Category = "Diğer",
                Keywords = [],
                Description = "Görsel analiz sonucu işlenemedi."
            };
        }

        var products = await SearchProductsInDatabase(analysis, maxResults, cancellationToken);

        return new VisualSearchAnalysisResponse
        {
            Analysis = analysis,
            Products = products,
            TotalFound = products.Count
        };
    }

    public Task<IReadOnlyCollection<VisualSearchMatch>> SearchAsync(
        string queryImageUrl,
        IReadOnlyCollection<VisualSearchCandidate> candidates,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var matches = candidates
            .Select(candidate =>
            {
                var score = CalculatePseudoSimilarity(queryImageUrl, candidate.ProductId);
                return new VisualSearchMatch
                {
                    ProductId = candidate.ProductId,
                    SimilarityScore = score,
                    Rank = 0
                };
            })
            .OrderByDescending(x => x.SimilarityScore)
            .Take(maxResults)
            .Select((match, index) => new VisualSearchMatch
            {
                ProductId = match.ProductId,
                SimilarityScore = match.SimilarityScore,
                Rank = index + 1
            })
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<VisualSearchMatch>>(matches);
    }

    private async Task<IReadOnlyCollection<VisualSearchResultResponse>> SearchProductsInDatabase(
        GeminiImageAnalysis analysis,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var keywords = analysis.Keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.ToLower().Trim())
            .ToList();

        var query = dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.Images)
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(analysis.Category) && analysis.Category != "Diğer")
        {
            query = query.Where(p => p.Category != null && p.Category.Name == analysis.Category);
        }

        var allProducts = await query.ToListAsync(cancellationToken);

        var scored = allProducts
            .Select(p =>
            {
                var score = CalculateRelevanceScore(p.Title, p.Description, keywords);
                return new { Product = p, Score = score };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select((x, index) => new VisualSearchResultResponse
            {
                ProductId = x.Product.Id,
                Title = x.Product.Title,
                Price = x.Product.Price,
                ImageUrl = x.Product.Images
                    .OrderBy(img => img.SortOrder)
                    .Select(img => img.Url)
                    .FirstOrDefault(),
                SimilarityScore = Math.Round(x.Score, 4),
                Rank = index + 1
            })
            .ToArray();

        return scored;
    }

    private static double CalculateRelevanceScore(string title, string description, List<string> keywords)
    {
        if (keywords.Count == 0) return 0;

        var titleLower = title.ToLower();
        var descLower = description.ToLower();
        var matchCount = 0;

        foreach (var kw in keywords)
        {
            if (titleLower.Contains(kw))
                matchCount += 3; // title match weighted higher
            else if (descLower.Contains(kw))
                matchCount += 1;
        }

        var maxPossible = keywords.Count * 3;
        return (double)matchCount / maxPossible;
    }

    private static double CalculatePseudoSimilarity(string queryImageUrl, Guid productId)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{queryImageUrl}:{productId}"));
        var value = BitConverter.ToUInt32(bytes, 0);
        var normalized = value / (double)uint.MaxValue;
        return Math.Round(0.55 + (normalized * 0.44), 4);
    }

    private static string CleanJsonResponse(string raw)
    {
        if (raw.Contains("```json"))
        {
            var start = raw.IndexOf("```json", StringComparison.Ordinal) + 7;
            var end = raw.LastIndexOf("```", StringComparison.Ordinal);
            if (end > start) return raw[start..end].Trim();
        }

        if (raw.Contains("```"))
        {
            var start = raw.IndexOf("```", StringComparison.Ordinal) + 3;
            var end = raw.LastIndexOf("```", StringComparison.Ordinal);
            if (end > start) return raw[start..end].Trim();
        }

        return raw.Trim();
    }
}
