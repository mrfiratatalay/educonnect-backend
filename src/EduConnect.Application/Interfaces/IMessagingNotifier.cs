using EduConnect.Application.Contracts.Messaging;

namespace EduConnect.Application.Interfaces;

public interface IMessagingNotifier
{
    Task NotifyNewMessageAsync(Guid receiverId, ConversationResponse response, CancellationToken cancellationToken = default);
    Task NotifyMessageReadAsync(Guid senderId, Guid conversationId, CancellationToken cancellationToken = default);
}
