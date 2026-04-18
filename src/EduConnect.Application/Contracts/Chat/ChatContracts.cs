using EduConnect.Domain.Enums;

namespace EduConnect.Application.Contracts.Chat;

public sealed class ChatbotReply
{
    public string Content { get; init; } = string.Empty;

    public string? IntentDetected { get; init; }

    public double? Confidence { get; init; }

    public string? ConfidenceBand { get; init; }

    public bool NeedsReview { get; init; }

    public string? ModelUsed { get; init; }

    public double? KbScore { get; init; }

    public bool KbHit { get; init; }

    public bool IsFallback { get; init; }

    public long LatencyMs { get; init; }
}

public sealed class ChatSessionStartedResponse
{
    public Guid SessionId { get; init; }

    public DateTime StartedAtUtc { get; init; }
}

public sealed class SendMessageRequest
{
    public string Message { get; init; } = string.Empty;
}

public sealed class ChatSessionResponse
{
    public Guid SessionId { get; init; }

    public DateTime StartedAtUtc { get; init; }

    public DateTime? EndedAtUtc { get; init; }

    public int TotalMessages { get; init; }

    public bool IsActive { get; init; }
}

public sealed class ChatMessageResponse
{
    public Guid Id { get; init; }

    public SenderType SenderType { get; init; }

    public string Content { get; init; } = string.Empty;

    public string? IntentDetected { get; init; }

    public double? Confidence { get; init; }

    public string? ConfidenceBand { get; init; }

    public bool NeedsReview { get; init; }

    public string? ModelUsed { get; init; }

    public double? KbScore { get; init; }

    public bool? KbHit { get; init; }

    public bool? IsFallback { get; init; }

    public long? LatencyMs { get; init; }

    public DateTime TimestampUtc { get; init; }

    public bool? HasFeedback { get; init; }

    public bool? FeedbackIsHelpful { get; init; }
}
