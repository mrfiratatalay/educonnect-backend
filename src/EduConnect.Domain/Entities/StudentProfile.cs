using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class StudentProfile : AuditableEntity
{
    public Guid UserId { get; set; }

    public string Department { get; set; } = string.Empty;

    public int Year { get; set; }

    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }

    public string? CoverImageUrl { get; set; }

    public User User { get; set; } = null!;
}
