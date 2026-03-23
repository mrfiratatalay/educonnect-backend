namespace EduConnect.Application.Contracts.Chat;

public sealed class ChatbotReply
{
    public string Content { get; init; } = string.Empty;

    public string? IntentDetected { get; init; }

    public double? Confidence { get; init; }
}

public sealed class ChatSessionStartedResponse
{
    public Guid SessionId { get; init; }

    public DateTime StartedAtUtc { get; init; }
}
