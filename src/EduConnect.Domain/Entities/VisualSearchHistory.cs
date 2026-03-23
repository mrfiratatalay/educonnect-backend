using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class VisualSearchHistory : AuditableEntity
{
    public Guid UserId { get; set; }

    public string QueryImageUrl { get; set; } = string.Empty;

    public DateTime SearchedAtUtc { get; set; } = DateTime.UtcNow;

    public int ResultCount { get; set; }

    public User User { get; set; } = null!;

    public ICollection<VisualSearchResult> Results { get; set; } = [];
}
