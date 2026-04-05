using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class Group : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? BannerUrl { get; set; }

    public Guid CreatorUserId { get; set; }

    public string Category { get; set; } = string.Empty;

    public User CreatorUser { get; set; } = null!;

    public ICollection<GroupMember> Members { get; set; } = [];

    public ICollection<Post> Posts { get; set; } = [];

    public ICollection<Event> Events { get; set; } = [];
}
