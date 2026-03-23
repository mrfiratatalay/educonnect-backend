using EduConnect.Domain.Common;
using EduConnect.Domain.Enums;

namespace EduConnect.Domain.Entities;

public sealed class GroupMember : AuditableEntity
{
    public Guid GroupId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;

    public Group Group { get; set; } = null!;

    public User User { get; set; } = null!;
}
