using System.ComponentModel.DataAnnotations;
using EduConnect.Domain.Enums;

namespace EduConnect.Application.Contracts.VisualSearch;

public sealed class VisualSearchSearchRequest
{
    [Range(1, 20)]
    public int MaxResults { get; init; } = 4;

    public Guid? CategoryId { get; init; }

    [Range(0, 10_000_000)]
    public decimal? MinPrice { get; init; }

    [Range(0, 10_000_000)]
    public decimal? MaxPrice { get; init; }

    [StringLength(120)]
    public string? City { get; init; }

    [StringLength(20)]
    public string Mode { get; init; } = "strict";
}

public sealed class VisualSearchAnalysis
{
    public string ProductName { get; set; } = string.Empty;

    public string CategoryLabel { get; set; } = string.Empty;

    public List<string> Keywords { get; set; } = [];

    public string Description { get; set; } = string.Empty;

    public string EstimatedPriceRange { get; set; } = string.Empty;

    public string ConditionLabel { get; set; } = string.Empty;

    public int Confidence { get; set; }
}

public sealed class VisualSearchBreakdownItem
{
    public string Label { get; init; } = string.Empty;

    public int Value { get; init; }
}

public sealed class VisualSearchResultResponse
{
    public Guid ProductId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public string CategoryLabel { get; init; } = string.Empty;

    public string SellerName { get; init; } = string.Empty;

    public ProductCondition Condition { get; init; }

    public string ConditionLabel { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public double SimilarityScore { get; init; }

    public int Rank { get; init; }

    public IReadOnlyCollection<string> MatchedSignals { get; init; } = [];

    public IReadOnlyCollection<VisualSearchBreakdownItem> Breakdown { get; init; } = [];
}

public sealed class VisualSearchSearchResponse
{
    public Guid? SearchId { get; set; }

    public VisualSearchAnalysis Analysis { get; init; } = new();

    public IReadOnlyCollection<VisualSearchResultResponse> Results { get; init; } = [];

    public int TotalFound { get; init; }

    public int CandidateCount { get; init; }

    public int FilteredCount { get; init; }

    public long ElapsedMs { get; init; }
}

public sealed class VisualSearchHistoryResponse
{
    public Guid Id { get; init; }

    public string QueryImageUrl { get; init; } = string.Empty;

    public DateTime SearchedAtUtc { get; init; }

    public int ResultCount { get; init; }

    public IReadOnlyCollection<VisualSearchResultResponse> Results { get; init; } = [];
}
