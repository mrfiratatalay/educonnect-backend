using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class ChatMessageFeedback : AuditableEntity
{
    public Guid ChatMessageId { get; set; }

    public Guid UserId { get; set; }

    public bool IsHelpful { get; set; }

    public string? Comment { get; set; }

    public ChatMessage ChatMessage { get; set; } = null!;

    public User User { get; set; } = null!;
}
