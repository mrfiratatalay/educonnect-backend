using System.ComponentModel.DataAnnotations;

namespace EduConnect.Application.Contracts.Groups;

public sealed class CreateGroupRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Category { get; init; } = string.Empty;
}

public sealed class GroupResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public Guid CreatorUserId { get; init; }

    public string CreatorName { get; init; } = string.Empty;

    public int MemberCount { get; init; }

    public bool JoinedByCurrentUser { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
