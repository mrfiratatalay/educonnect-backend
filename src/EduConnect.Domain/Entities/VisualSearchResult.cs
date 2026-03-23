using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class VisualSearchResult : AuditableEntity
{
    public Guid SearchHistoryId { get; set; }

    public Guid ProductId { get; set; }

    public double SimilarityScore { get; set; }

    public int Rank { get; set; }

    public VisualSearchHistory SearchHistory { get; set; } = null!;

    public Product Product { get; set; } = null!;
}
