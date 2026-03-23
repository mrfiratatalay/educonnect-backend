using System.ComponentModel.DataAnnotations;

namespace EduConnect.Application.Contracts.Events;

public sealed class CreateEventRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(1500, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Required, StringLength(250)]
    public string Location { get; init; } = string.Empty;

    [Required]
    public DateTime StartDateUtc { get; init; }

    [Required]
    public DateTime EndDateUtc { get; init; }

    public Guid? GroupId { get; init; }

    [Range(1, 5000)]
    public int MaxParticipants { get; init; } = 100;

    [Required, StringLength(100)]
    public string Category { get; init; } = string.Empty;
}

public sealed class EventResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public DateTime StartDateUtc { get; init; }

    public DateTime EndDateUtc { get; init; }

    public Guid CreatorUserId { get; init; }

    public string CreatorName { get; init; } = string.Empty;

    public Guid? GroupId { get; init; }

    public string? GroupName { get; init; }

    public int MaxParticipants { get; init; }

    public int ParticipantCount { get; init; }

    public bool RegisteredByCurrentUser { get; init; }

    public string Category { get; init; } = string.Empty;
}
