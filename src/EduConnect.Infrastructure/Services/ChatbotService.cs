using EduConnect.Application.Contracts.Chat;
using EduConnect.Application.Interfaces;

namespace EduConnect.Infrastructure.Services;

public sealed class ChatbotService : IChatbotService
{
    public Task<ChatbotReply> GetReplyAsync(string message, CancellationToken cancellationToken = default)
    {
        var normalized = message.Trim().ToLowerInvariant();

        if (normalized.Contains("vize") || normalized.Contains("final") || normalized.Contains("sınav"))
        {
            return Task.FromResult(new ChatbotReply
            {
                Content = "Sınav hazırlığı için ders notlarını, çıkmış soruları ve haftalık tekrar planını birlikte kullanmanı öneririm.",
                IntentDetected = "ExamSupport",
                Confidence = 0.92
            });
        }

        if (normalized.Contains("etkinlik") || normalized.Contains("kulüp"))
        {
            return Task.FromResult(new ChatbotReply
            {
                Content = "Etkinlikler bölümünden tarih ve kategori filtreleriyle sana uygun kampüs etkinliklerini görebilirsin.",
                IntentDetected = "CampusEvents",
                Confidence = 0.89
            });
        }

        if (normalized.Contains("indirim") || normalized.Contains("kupon"))
        {
            return Task.FromResult(new ChatbotReply
            {
                Content = "İndirimler sayfasında aktif kampanyaları görebilir, kodları doğrudan kopyalayabilirsin.",
                IntentDetected = "Discounts",
                Confidence = 0.90
            });
        }

        return Task.FromResult(new ChatbotReply
        {
            Content = "Sorunu anladım. Sana en uygun yönlendirmeyi yapmak için ders, etkinlik, pazar veya kampüs yaşamı ile ilgili daha net bir ifade yazabilirsin.",
            IntentDetected = "GeneralSupport",
            Confidence = 0.74
        });
    }
}
