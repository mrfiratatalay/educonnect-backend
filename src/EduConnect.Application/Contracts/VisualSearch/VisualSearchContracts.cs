using System.ComponentModel.DataAnnotations;

namespace EduConnect.Application.Contracts.VisualSearch;

public sealed class VisualSearchRequest
{
    [Required, Url]
    public string QueryImageUrl { get; init; } = string.Empty;

    [Range(1, 20)]
    public int MaxResults { get; init; } = 8;
}

public sealed class VisualSearchCandidate
{
    public Guid ProductId { get; init; }

    public string Title { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }
}

public sealed class VisualSearchMatch
{
    public Guid ProductId { get; init; }

    public double SimilarityScore { get; init; }

    public int Rank { get; init; }
}

public sealed class VisualSearchResultResponse
{
    public Guid ProductId { get; init; }

    public string Title { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public double SimilarityScore { get; init; }

    public int Rank { get; init; }
}

public sealed class VisualSearchHistoryResponse
{
    public Guid Id { get; init; }

    public string QueryImageUrl { get; init; } = string.Empty;

    public DateTime SearchedAtUtc { get; init; }

    public int ResultCount { get; init; }

    public IReadOnlyCollection<VisualSearchResultResponse> Results { get; init; } = [];
}

public sealed class GeminiImageAnalysis
{
    public string ProductName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public List<string> Keywords { get; set; } = [];

    public string Description { get; set; } = string.Empty;

    public string EstimatedPriceRange { get; set; } = string.Empty;

    public string Condition { get; set; } = string.Empty;
}

public sealed class VisualSearchAnalysisResponse
{
    public GeminiImageAnalysis Analysis { get; init; } = new();

    public IReadOnlyCollection<VisualSearchResultResponse> Products { get; init; } = [];

    public int TotalFound { get; init; }
}
