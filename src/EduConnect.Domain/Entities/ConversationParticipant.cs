using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class ConversationParticipant : AuditableEntity
{
    public Guid ConversationId { get; set; }
    
    public Guid UserId { get; set; }
    
    public bool HasUnreadMessages { get; set; }
    
    public int UnreadCount { get; set; }

    public Conversation Conversation { get; set; } = null!;
    
    public User User { get; set; } = null!;
}
