using EduConnect.Domain.Common;
using EduConnect.Domain.Enums;

namespace EduConnect.Domain.Entities;

public sealed class EventParticipant : AuditableEntity
{
    public Guid EventId { get; set; }

    public Guid UserId { get; set; }

    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;

    public EventParticipantStatus Status { get; set; } = EventParticipantStatus.Registered;

    public Event Event { get; set; } = null!;

    public User User { get; set; } = null!;
}
