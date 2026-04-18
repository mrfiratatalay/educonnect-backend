using System.ComponentModel.DataAnnotations;
using EduConnect.Domain.Enums;

namespace EduConnect.Application.Contracts.Users;

public sealed class UpdateMyProfileRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string FullName { get; init; } = string.Empty;

    [StringLength(150)]
    public string Department { get; init; } = string.Empty;

    [Range(1, 8)]
    public int Year { get; init; } = 1;

    [StringLength(500)]
    public string? Bio { get; init; }

    public Guid? UniversityId { get; init; }
}

public sealed class UploadAvatarRequest
{
    [Required, Url]
    public string AvatarUrl { get; init; } = string.Empty;
}

public sealed class UserProfileResponse
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public string Department { get; init; } = string.Empty;

    public int Year { get; init; }

    public string? Bio { get; init; }

    public string? AvatarUrl { get; init; }

    public string? CoverImageUrl { get; init; }

    public Guid? UniversityId { get; init; }

    public string? UniversityName { get; init; }

    public int FollowersCount { get; init; }

    public int FollowingCount { get; init; }
}

public sealed class PublicUserProfileResponse
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public string Department { get; init; } = string.Empty;

    public int Year { get; init; }

    public string? Bio { get; init; }

    public string? AvatarUrl { get; init; }

    public string? CoverImageUrl { get; init; }

    public Guid? UniversityId { get; init; }

    public string? UniversityName { get; init; }

    public int FollowersCount { get; init; }

    public int FollowingCount { get; init; }

    public bool IsFollowedByCurrentUser { get; init; }
}

public sealed class FollowStateResponse
{
    public bool IsFollowing { get; init; }
}

public sealed class FollowSuggestionResponse
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string? Department { get; init; }

    public string? UniversityName { get; init; }

    public int MutualGroupCount { get; init; }

    public string ReasonLabel { get; init; } = string.Empty;

    public bool IsFollowedByCurrentUser { get; init; }
}

public sealed class UserConnectionResponse
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string? Department { get; init; }

    public string? UniversityName { get; init; }

    public bool IsFollowedByCurrentUser { get; init; }
}

public sealed class UserSearchResult
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string? Department { get; init; }

    public string? UniversityName { get; init; }
}
