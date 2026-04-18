using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class DirectConversation : AuditableEntity
{
    public Guid UserLowerId { get; set; }

    public Guid UserHigherId { get; set; }

    public DateTime LastMessageAtUtc { get; set; }

    public User UserLower { get; set; } = null!;

    public User UserHigher { get; set; } = null!;

    public ICollection<DirectMessage> Messages { get; set; } = [];
}
