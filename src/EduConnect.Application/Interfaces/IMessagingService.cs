using EduConnect.Application.Contracts.Common;
using EduConnect.Application.Contracts.Messaging;

namespace EduConnect.Application.Interfaces;

public interface IMessagingService
{
    Task<ConversationResponse> SendMessageAsync(Guid senderId, SendMessageRequest request, CancellationToken cancellationToken = default);
    
    Task<CursorPagedResponse<ConversationResponse>> GetConversationsAsync(Guid userId, DateTime? cursor = null, int limit = 20, CancellationToken cancellationToken = default);
    
    Task<CursorPagedResponse<DirectMessageResponse>> GetMessagesAsync(Guid userId, Guid conversationId, DateTime? cursor = null, int limit = 50, CancellationToken cancellationToken = default);
    
    Task MarkAsReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
}
