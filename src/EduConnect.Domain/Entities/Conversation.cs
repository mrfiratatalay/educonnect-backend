using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class Conversation : AuditableEntity
{
    public DateTime? LastMessageAtUtc { get; set; }
    
    public string? LastMessagePreview { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = [];
    
    public ICollection<DirectMessage> Messages { get; set; } = [];
}
