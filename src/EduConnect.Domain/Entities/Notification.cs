using EduConnect.Domain.Common;
using EduConnect.Domain.Enums;

namespace EduConnect.Domain.Entities;

public sealed class Notification : AuditableEntity
{
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public NotificationType Type { get; set; } = NotificationType.General;

    public User User { get; set; } = null!;
}
