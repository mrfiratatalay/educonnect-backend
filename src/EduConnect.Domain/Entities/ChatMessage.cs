using EduConnect.Domain.Common;
using EduConnect.Domain.Enums;

namespace EduConnect.Domain.Entities;

public sealed class ChatMessage : AuditableEntity
{
    public Guid SessionId { get; set; }

    public SenderType SenderType { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public string? IntentDetected { get; set; }

    public double? Confidence { get; set; }

    // --- Faz 5: Analitik alanları ---
    public string? ModelUsed { get; set; }

    public double? KbScore { get; set; }

    public bool? KbHit { get; set; }

    public bool? IsFallback { get; set; }

    public long? LatencyMs { get; set; }

    public ChatSession Session { get; set; } = null!;

    public ChatMessageFeedback? Feedback { get; set; }
}
