using EduConnect.Domain.Enums;

namespace EduConnect.Application.Contracts.Notifications;

public sealed class NotificationResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool IsRead { get; init; }

    public NotificationType Type { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string? TargetPath { get; init; }
}
