using System.ComponentModel.DataAnnotations;
using EduConnect.Application.Contracts.Common;

namespace EduConnect.Application.Contracts.Posts;

public sealed class CreatePostRequest
{
    public Guid? GroupId { get; init; }

    [Required, StringLength(1500, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;

    [Url]
    public string? ImageUrl { get; init; }
}

public sealed class UpdatePostRequest
{
    [Required, StringLength(1500, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;

    [Url]
    public string? ImageUrl { get; init; }
}

public sealed class CreatePostCommentRequest
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

public sealed class PostCommentResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string Content { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class PostResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid? GroupId { get; init; }

    public string? GroupName { get; init; }

    public string? GroupSlug { get; init; }

    public string? GroupAvatarUrl { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string Content { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public int LikesCount { get; init; }

    public int CommentsCount { get; init; }

    public int ViewsCount { get; init; }

    public bool LikedByCurrentUser { get; init; }

    public bool BookmarkedByCurrentUser { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class PostBookmarkStateResponse
{
    public bool IsBookmarked { get; init; }
}

public sealed class PostViewTrackingResponse
{
    public int ViewsCount { get; init; }
}

public sealed class PostDetailResponse
{
    public PostResponse Post { get; init; } = new();

    public IReadOnlyCollection<PostCommentResponse> Comments { get; init; } = [];
}
