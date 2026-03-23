using EduConnect.Application.Contracts.Chat;

namespace EduConnect.Application.Interfaces;

public interface IChatbotService
{
    Task<ChatbotReply> GetReplyAsync(string message, CancellationToken cancellationToken = default);
}
