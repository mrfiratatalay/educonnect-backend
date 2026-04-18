namespace EduConnect.Application.Contracts.Chat;

/// <summary>
/// Kullanıcı geri bildirim isteği (yardımcı oldu / olmadı).
/// </summary>
public sealed class SubmitChatFeedbackRequest
{
    public bool IsHelpful { get; init; }

    public string? Comment { get; init; }
}

/// <summary>
/// Admin analiz özeti yanıtı.
/// </summary>
public sealed class EduAiAnalyticsSummaryResponse
{
    public int TotalSessions { get; init; }

    public int TotalBotMessages { get; init; }

    public int FeedbackCount { get; init; }

    public int HelpfulCount { get; init; }

    public int NotHelpfulCount { get; init; }

    public double HelpfulRate { get; init; }

    public double AvgConfidence { get; init; }

    public double AvgLatencyMs { get; init; }

    public int KbHitCount { get; init; }

    public double KbHitRate { get; init; }

    public int FallbackCount { get; init; }

    public double FallbackRate { get; init; }

    public IReadOnlyCollection<IntentBreakdown> IntentDistribution { get; init; } = [];

    public IReadOnlyCollection<UnresolvedQueryInfo> TopUnresolvedQueries { get; init; } = [];
}

public sealed class IntentBreakdown
{
    public string Intent { get; init; } = string.Empty;

    public int Count { get; init; }

    public double AvgConfidence { get; init; }
}

public sealed class UnresolvedQueryInfo
{
    public string UserMessage { get; init; } = string.Empty;

    public string Intent { get; init; } = string.Empty;

    public double Confidence { get; init; }

    public DateTime TimestampUtc { get; init; }
}
