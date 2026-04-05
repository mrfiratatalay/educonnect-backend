using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class PostBookmark : AuditableEntity
{
    public Guid PostId { get; set; }

    public Guid UserId { get; set; }

    public Post Post { get; set; } = null!;

    public User User { get; set; } = null!;
}
