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

    public ChatSession Session { get; set; } = null!;
}
