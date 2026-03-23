using EduConnect.Application.Contracts.Chat;
using EduConnect.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduConnect.Infrastructure.Services;

public sealed class ChatbotService(
    IGeminiApiService geminiApiService,
    ILogger<ChatbotService> logger) : IChatbotService
{
    public async Task<ChatbotReply> GetReplyAsync(
        string message,
        IReadOnlyCollection<(string Role, string Content)>? conversationHistory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var aiResponse = await geminiApiService.GenerateTextAsync(
                message,
                conversationHistory,
                cancellationToken);

            var intent = DetectIntent(message);

            return new ChatbotReply
            {
                Content = aiResponse,
                IntentDetected = intent.Name,
                Confidence = intent.Score
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chatbot reply generation failed for message: {MessagePreview}",
                message.Length > 50 ? message[..50] + "..." : message);

            return new ChatbotReply
            {
                Content = "Üzgünüm, şu an yanıt üretirken bir sorun oluştu. Lütfen biraz sonra tekrar dene.",
                IntentDetected = "Error",
                Confidence = 0
            };
        }
    }

    private static (string Name, double Score) DetectIntent(string message)
    {
        var normalized = message.Trim().ToLowerInvariant();

        if (normalized.Contains("vize") || normalized.Contains("final") || normalized.Contains("sınav") || normalized.Contains("ders"))
            return ("ExamSupport", 0.92);

        if (normalized.Contains("etkinlik") || normalized.Contains("kulüp") || normalized.Contains("toplantı"))
            return ("CampusEvents", 0.89);

        if (normalized.Contains("indirim") || normalized.Contains("kupon") || normalized.Contains("kampanya"))
            return ("Discounts", 0.90);

        if (normalized.Contains("ürün") || normalized.Contains("satış") || normalized.Contains("pazar"))
            return ("Marketplace", 0.88);

        if (normalized.Contains("profil") || normalized.Contains("hesap") || normalized.Contains("şifre"))
            return ("AccountSupport", 0.85);

        return ("GeneralSupport", 0.74);
    }
}
