using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class Event : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public Guid CreatorUserId { get; set; }

    public Guid? GroupId { get; set; }

    public int MaxParticipants { get; set; }

    public string Category { get; set; } = string.Empty;

    public User CreatorUser { get; set; } = null!;

    public Group? Group { get; set; }

    public ICollection<EventParticipant> Participants { get; set; } = [];
}
