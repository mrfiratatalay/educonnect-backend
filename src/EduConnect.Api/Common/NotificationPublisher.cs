using EduConnect.Api.Hubs;
using EduConnect.Api.Mappings;
using EduConnect.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace EduConnect.Api.Common;

public sealed class NotificationPublisher(IHubContext<NotificationHub> notificationHub)
{
    public Task PublishAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        return notificationHub.Clients.User(notification.UserId.ToString()).SendAsync(
            "NotificationReceived",
            notification.ToResponse(),
            cancellationToken);
    }
}
