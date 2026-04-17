using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class DirectMessage : AuditableEntity
{
    public Guid ConversationId { get; set; }
    
    public Guid SenderId { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    public bool IsRead { get; set; }
    
    public DateTime? ReadAtUtc { get; set; }

    public Conversation Conversation { get; set; } = null!;
    
    public User Sender { get; set; } = null!;
}
