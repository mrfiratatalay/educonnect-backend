using EduConnect.Api.Hubs;
using EduConnect.Application.Contracts.Messaging;
using EduConnect.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EduConnect.Api.Services;

public sealed class MessagingNotifier(IHubContext<MessagingHub> hubContext) : IMessagingNotifier
{
    public Task NotifyNewMessageAsync(Guid receiverId, ConversationResponse response, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.User(receiverId.ToString())
            .SendAsync("ReceiveDirectMessage", response, cancellationToken);
    }

    public Task NotifyMessageReadAsync(Guid senderId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.User(senderId.ToString())
            .SendAsync("ConversationRead", conversationId, cancellationToken);
    }
}
