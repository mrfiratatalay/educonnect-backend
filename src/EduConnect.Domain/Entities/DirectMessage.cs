using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class DirectMessage : Entity
{
    public Guid ConversationId { get; set; }

    public Guid SenderUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    public DirectConversation Conversation { get; set; } = null!;

    public User Sender { get; set; } = null!;
}
