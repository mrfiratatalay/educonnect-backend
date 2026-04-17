namespace EduConnect.Application.Contracts.Messaging;

public sealed record SendMessageRequest(Guid? ConversationId, Guid? ReceiverId, string Content);

public sealed record ConversationResponse(
    Guid Id, 
    DateTime? LastMessageAtUtc, 
    string? LastMessagePreview, 
    int UnreadCount, 
    ParticipantResponse OtherParticipant);

public sealed record ParticipantResponse(Guid Id, string FullName, string? AvatarUrl);

public sealed record DirectMessageResponse(
    Guid Id, 
    Guid SenderId, 
    string Content, 
    DateTime SentAtUtc, 
    bool IsRead);
