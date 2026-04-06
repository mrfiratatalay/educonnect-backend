using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class UserFollow : AuditableEntity
{
    public Guid FollowerUserId { get; set; }

    public Guid FollowedUserId { get; set; }

    public User FollowerUser { get; set; } = null!;

    public User FollowedUser { get; set; } = null!;
}
