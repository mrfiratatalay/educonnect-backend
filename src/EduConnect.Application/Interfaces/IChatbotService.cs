using EduConnect.Application.Contracts.Chat;

namespace EduConnect.Application.Interfaces;

public interface IChatbotService
{
    Task<ChatbotReply> GetReplyAsync(
        string message,
        IReadOnlyCollection<(string Role, string Content)>? conversationHistory = null,
        CancellationToken cancellationToken = default);
}
