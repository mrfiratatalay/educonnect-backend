using System.ComponentModel.DataAnnotations;
using EduConnect.Application.Contracts.Common;

namespace EduConnect.Application.Contracts.Posts;

public sealed class CreatePostRequest
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

    public string UserName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string Content { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public int LikesCount { get; init; }

    public int CommentsCount { get; init; }

    public bool LikedByCurrentUser { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class PostDetailResponse
{
    public PostResponse Post { get; init; } = new();

    public IReadOnlyCollection<PostCommentResponse> Comments { get; init; } = [];
}
