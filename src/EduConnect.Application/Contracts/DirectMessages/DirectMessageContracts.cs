using System.ComponentModel.DataAnnotations;

namespace EduConnect.Application.Contracts.DirectMessages;

public sealed class StartConversationRequest
{
    [Required]
    public Guid OtherUserId { get; init; }
}

public sealed class SendDirectMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

public sealed class ConversationSummaryResponse
{
    public required Guid Id { get; init; }

    public required Guid OtherUserId { get; init; }

    public required string OtherUserName { get; init; }

    public string? OtherUserAvatarUrl { get; init; }

    public string? LastMessagePreview { get; init; }

    public required DateTime LastMessageAtUtc { get; init; }
}

public sealed class DirectMessageItemResponse
{
    public required Guid Id { get; init; }

    public required Guid SenderUserId { get; init; }

    public required string Content { get; init; }

    public required DateTime SentAtUtc { get; init; }
}

public sealed class PagedMessagesResponse
{
    public required IReadOnlyList<DirectMessageItemResponse> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }
}

public sealed class StartConversationResponse
{
    public required Guid ConversationId { get; init; }

    public required bool Created { get; init; }
}
