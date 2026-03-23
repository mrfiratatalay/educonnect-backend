using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class Post : AuditableEntity
{
    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public bool IsDeleted { get; set; }

    public User User { get; set; } = null!;

    public ICollection<PostComment> Comments { get; set; } = [];

    public ICollection<PostLike> Likes { get; set; } = [];
}
