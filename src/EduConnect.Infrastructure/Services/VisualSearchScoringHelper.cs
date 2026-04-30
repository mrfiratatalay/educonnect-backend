using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EduConnect.Application.Contracts.VisualSearch;
using EduConnect.Domain.Enums;

namespace EduConnect.Infrastructure.Services;

internal static partial class VisualSearchScoringHelper
{
    private static readonly Dictionary<ProductCondition, double> ConditionWeights = new()
    {
        [ProductCondition.New] = 1,
        [ProductCondition.LikeNew] = 0.88,
        [ProductCondition.Good] = 0.72,
        [ProductCondition.Fair] = 0.54
    };

    internal static double GetCategoryScore(string normalizedCategory, string analysisCategory)
    {
        var normalizedAnalysis = NormalizeForSearch(analysisCategory);
        if (string.IsNullOrWhiteSpace(normalizedAnalysis) || normalizedAnalysis == "diger")
            return 0.45;

        if (normalizedCategory.Contains(normalizedAnalysis, StringComparison.Ordinal))
            return 1;

        if ((normalizedCategory.Contains("kitap", StringComparison.Ordinal) && normalizedAnalysis.Contains("kitap", StringComparison.Ordinal)) ||
            (normalizedCategory.Contains("elektronik", StringComparison.Ordinal) && normalizedAnalysis.Contains("elektronik", StringComparison.Ordinal)) ||
            (normalizedCategory.Contains("kirtasiye", StringComparison.Ordinal) && normalizedAnalysis.Contains("kirtasiye", StringComparison.Ordinal)) ||
            (normalizedCategory.Contains("etkinlik", StringComparison.Ordinal) && normalizedAnalysis.Contains("bilet", StringComparison.Ordinal)))
            return 0.82;

        return 0.18;
    }

    internal static double GetTextScore(
        string normalizedTitle, string normalizedDescription,
        IReadOnlyCollection<string> keywords, IReadOnlyCollection<string> productNameTokens)
    {
        var searchableText = $"{normalizedTitle} {normalizedDescription}";
        var keywordHits = keywords.Count(kw => searchableText.Contains(kw, StringComparison.Ordinal));
        var nameHits = productNameTokens.Count(t => normalizedTitle.Contains(t, StringComparison.Ordinal));

        var kwScore = keywords.Count == 0 ? 0.2 : (double)keywordHits / keywords.Count;
        var nameScore = productNameTokens.Count == 0 ? 0.2 : (double)nameHits / productNameTokens.Count;

        return Math.Clamp((kwScore * 0.65) + (nameScore * 0.35), 0, 1);
    }

    internal static double GetConditionScore(ProductCondition condition, string conditionLabel)
    {
        var requested = ParseCondition(conditionLabel) ?? ProductCondition.Good;
        var distance = Math.Abs(ConditionWeights[condition] - ConditionWeights[requested]);
        return Math.Clamp(1 - (distance / 0.5), 0.2, 1);
    }

    internal static double GetPriceScore(decimal price, decimal? targetPrice)
    {
        if (!targetPrice.HasValue || targetPrice.Value <= 0) return 0.55;
        var deltaRatio = Math.Abs(price - targetPrice.Value) / targetPrice.Value;
        return Math.Clamp(1 - (double)deltaRatio, 0.15, 1);
    }

    internal static IReadOnlyCollection<string> BuildKeywords(VisualSearchAnalysis analysis)
    {
        var keywords = new List<string>();
        keywords.AddRange(Tokenize(analysis.ProductName));
        keywords.AddRange(analysis.Keywords.Select(NormalizeForSearch));
        return keywords.Where(kw => kw.Length >= 2).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToArray();
    }

    internal static decimal? ParseEstimatedPrice(string estimatedPriceRange)
    {
        if (string.IsNullOrWhiteSpace(estimatedPriceRange)) return null;
        var matches = NumberPattern().Matches(estimatedPriceRange);
        var numbers = matches
            .Select(m => decimal.TryParse(m.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : (decimal?)null)
            .Where(v => v.HasValue).Select(v => v!.Value).ToArray();
        return numbers.Length == 0 ? null : numbers.Average();
    }

    internal static string NormalizeCategoryLabel(string category)
    {
        var n = NormalizeForSearch(category);
        if (n.Contains("elektronik", StringComparison.Ordinal)) return "Elektronik";
        if (n.Contains("kitap", StringComparison.Ordinal) || n.Contains("ders materyal", StringComparison.Ordinal)) return "Ders Kitapları";
        if (n.Contains("kirtasiye", StringComparison.Ordinal)) return "Kırtasiye";
        if (n.Contains("etkinlik", StringComparison.Ordinal) || n.Contains("bilet", StringComparison.Ordinal)) return "Etkinlik Biletleri";
        return string.IsNullOrWhiteSpace(category) ? "Diğer" : category.Trim();
    }

    internal static string NormalizeConditionLabel(string condition) => ParseCondition(condition) switch
    {
        ProductCondition.New => "Sıfır",
        ProductCondition.LikeNew => "Yeni gibi",
        ProductCondition.Good => "İyi",
        ProductCondition.Fair => "Orta",
        _ => string.IsNullOrWhiteSpace(condition) ? "İyi" : condition.Trim()
    };

    internal static string GetConditionLabel(ProductCondition condition) => condition switch
    {
        ProductCondition.New => "Sıfır",
        ProductCondition.LikeNew => "Yeni gibi",
        ProductCondition.Good => "İyi",
        ProductCondition.Fair => "Orta",
        _ => "Bilinmiyor"
    };

    internal static string NormalizeForSearch(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
            if (char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        return builder.ToString().Normalize(NormalizationForm.FormC)
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal);
    }

    internal static IReadOnlyCollection<string> Tokenize(string value) =>
        NormalizeForSearch(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    internal static string NormalizeMode(string? mode) =>
        string.Equals(mode, "discovery", StringComparison.OrdinalIgnoreCase) ? "discovery" : "strict";

    internal static string CleanJsonResponse(string raw)
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

    internal static string ToTitleCase(string value) =>
        string.IsNullOrWhiteSpace(value) ? value : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);

    private static ProductCondition? ParseCondition(string condition)
    {
        var n = NormalizeForSearch(condition);
        if (n.Contains("sifir", StringComparison.Ordinal) || n.Contains("yeni", StringComparison.Ordinal))
            return n.Contains("gibi", StringComparison.Ordinal) ? ProductCondition.LikeNew : ProductCondition.New;
        if (n.Contains("iyi", StringComparison.Ordinal)) return ProductCondition.Good;
        if (n.Contains("orta", StringComparison.Ordinal) || n.Contains("eski", StringComparison.Ordinal)) return ProductCondition.Fair;
        return null;
    }

    [GeneratedRegex(@"\d+(?:[.,]\d+)?", RegexOptions.Compiled)]
    private static partial Regex NumberPattern();
}
