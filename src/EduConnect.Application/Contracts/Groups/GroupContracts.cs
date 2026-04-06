using System.ComponentModel.DataAnnotations;
using EduConnect.Domain.Enums;

namespace EduConnect.Application.Contracts.Groups;

public sealed class CreateGroupRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    [StringLength(220, MinimumLength = 3)]
    public string? ShortDescription { get; init; }

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Url, StringLength(500)]
    public string? AvatarUrl { get; init; }

    [Url, StringLength(500)]
    public string? BannerUrl { get; init; }

    [Required, StringLength(100)]
    public string Category { get; init; } = string.Empty;
}

public sealed class UpdateGroupRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    [StringLength(220, MinimumLength = 3)]
    public string? ShortDescription { get; init; }

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Url, StringLength(500)]
    public string? AvatarUrl { get; init; }

    [Url, StringLength(500)]
    public string? BannerUrl { get; init; }

    [Required, StringLength(100)]
    public string Category { get; init; } = string.Empty;
}

public class GroupResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string ShortDescription { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string? BannerUrl { get; init; }

    public string Category { get; init; } = string.Empty;

    public Guid CreatorUserId { get; init; }

    public string CreatorName { get; init; } = string.Empty;

    public int MemberCount { get; init; }

    public IReadOnlyCollection<GroupMemberPreviewResponse> PreviewMembers { get; init; } = [];

    public bool JoinedByCurrentUser { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class GroupMemberPreviewResponse
{
    public Guid UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string? Department { get; init; }

    public GroupMemberRole Role { get; init; }
}

public sealed class GroupDetailResponse : GroupResponse
{
    public int PostCount { get; init; }

    public int EventCount { get; init; }

    public bool CanCurrentUserPost { get; init; }

    public IReadOnlyCollection<GroupMemberPreviewResponse> ModeratorPreviewMembers { get; init; } = [];
}
