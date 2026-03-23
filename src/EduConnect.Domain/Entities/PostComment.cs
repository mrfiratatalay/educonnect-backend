using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class PostComment : AuditableEntity
{
    public Guid PostId { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public Post Post { get; set; } = null!;

    public User User { get; set; } = null!;
}
