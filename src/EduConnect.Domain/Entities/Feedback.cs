using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class Feedback : AuditableEntity
{
    public Guid UserId { get; set; }

    public string FeatureArea { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public User User { get; set; } = null!;
}
