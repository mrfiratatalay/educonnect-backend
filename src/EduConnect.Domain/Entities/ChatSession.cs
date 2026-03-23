using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class ChatSession : AuditableEntity
{
    public Guid UserId { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAtUtc { get; set; }

    public int TotalMessages { get; set; }

    public bool IsActive => EndedAtUtc is null;

    public User User { get; set; } = null!;

    public ICollection<ChatMessage> Messages { get; set; } = [];
}
