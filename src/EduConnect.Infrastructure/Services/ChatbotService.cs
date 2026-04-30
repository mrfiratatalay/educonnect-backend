using EduConnect.Application.Contracts.Chat;
using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EduConnect.Infrastructure.Services;

public sealed class ChatbotService(
    IGeminiApiService geminiApiService,
    INlpService nlpService,
    IOptions<NlpServiceOptions> nlpOptions,
    ILogger<ChatbotService> logger) : IChatbotService
{
    private readonly double _confidenceThreshold = nlpOptions.Value.ConfidenceThreshold;
    private const double GroundedKbThreshold = 0.25;

    public async Task<ChatbotReply> GetReplyAsync(
        string message,
        IReadOnlyCollection<(string Role, string Content)>? conversationHistory = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var nlpResult = await nlpService.ClassifyAsync(message, cancellationToken);

            var enrichedPrompt = BuildEnrichedPrompt(message, nlpResult);
            var aiResponse = await geminiApiService.GenerateTextAsync(
                enrichedPrompt, conversationHistory, cancellationToken);
            aiResponse = NormalizeResponseLinks(aiResponse);

            stopwatch.Stop();

            var kbHit = nlpResult.KbAnswer is not null && nlpResult.KbAnswer.Score >= GroundedKbThreshold;
            var isFallback = nlpResult.ModelUsed is "keyword-fallback" or "fallback" or "hybrid-fallback";

            return new ChatbotReply
            {
                Content = aiResponse,
                IntentDetected = nlpResult.Intent,
                Confidence = nlpResult.Confidence,
                ConfidenceBand = nlpResult.ConfidenceBand,
                NeedsReview = nlpResult.NeedsReview,
                ModelUsed = nlpResult.ModelUsed,
                KbScore = nlpResult.KbAnswer?.Score,
                KbHit = kbHit,
                IsFallback = isFallback,
                LatencyMs = stopwatch.ElapsedMilliseconds,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Chatbot reply generation failed");

            return new ChatbotReply
            {
                Content = "Üzgünüm, şu an yanıt üretilirken bir sorun oluştu. Lütfen biraz sonra tekrar dene.",
                IntentDetected = "Error",
                Confidence = 0,
                ConfidenceBand = "low",
                NeedsReview = true,
                ModelUsed = "error",
                KbHit = false,
                IsFallback = true,
                LatencyMs = stopwatch.ElapsedMilliseconds,
            };
        }
    }

    private string BuildEnrichedPrompt(string originalMessage, NlpClassifyResult nlpResult)
    {
        var entityInfo = nlpResult.Entities.Count > 0
            ? string.Join(", ", nlpResult.Entities.Select(e => $"{e.Type}: {e.Value}"))
            : "yok";

        var prompt =
            $"Kullanici sorusu: \"{originalMessage}\"\n" +
            $"Tespit edilen niyet: {nlpResult.Intent} (guven: {nlpResult.Confidence:P0})\n" +
            $"Guven bandi: {nlpResult.ConfidenceBand}\n" +
            $"Inceleme gerekli mi: {(nlpResult.NeedsReview ? "evet" : "hayir")}\n" +
            $"Tespit edilen varliklar: {entityInfo}\n" +
            $"Model: {nlpResult.ModelUsed}\n";

        if (IsGrounded(nlpResult))
        {
            prompt += BuildGroundedPrompt(nlpResult);
        }
        else
        {
            prompt += BuildFallbackPrompt(nlpResult);
        }

        return prompt;
    }

    private bool IsGrounded(NlpClassifyResult nlpResult)
    {
        if (nlpResult.KbAnswer is null)
        {
            return false;
        }

        if (nlpResult.KbAnswer.Score < GroundedKbThreshold)
        {
            return false;
        }

        if (nlpResult.KbAnswer.Confidence == "medium" && nlpResult.KbAnswer.TimeSensitive)
        {
            return false;
        }

        return nlpResult.Confidence >= _confidenceThreshold || nlpResult.KbAnswer.Score >= 0.35;
    }

    private string BuildGroundedPrompt(NlpClassifyResult nlpResult)
    {
        var kb = nlpResult.KbAnswer!;
        var cautionLine = kb.TimeSensitive
            ? "Bu bilgi zaman bagimli olabilir. Kesin tarih/ucret gerekiyorsa resmi kaynak kontrolunu oner.\n"
            : string.Empty;
        var reviewLine = nlpResult.NeedsReview
            ? "NLP guveni dusuk oldugu icin cevabi daha temkinli kur ve resmi kaynagi vurgula.\n"
            : string.Empty;

        return
            $"\nBilgi tabani eslesmesi bulundu.\n" +
            $"KB eslesme skoru: {kb.Score:P0}\n" +
            $"KB guven seviyesi: {kb.Confidence}\n" +
            $"Konu: {kb.Topic}\n" +
            $"Kapsam: {kb.FacultyScope}\n" +
            $"Kaynak basligi: {kb.SourceTitle}\n" +
            $"Kaynak URL: {kb.SourceUrl}\n" +
            $"KB sorusu: {kb.Question}\n" +
            $"KB cevabi: {kb.Answer}\n" +
            cautionLine +
            reviewLine +
            "\nGorevin:\n" +
            "1. Cevabini oncelikle yukaridaki KB cevabina dayandir.\n" +
            "2. KB cevabini degistirme, genisletme veya uydurma bilgi ekleme.\n" +
            "3. Gerekiyorsa dili daha dogal ve ogrenci dostu hale getir.\n" +
            "4. Zaman bagimli bilgi varsa kisa bir resmi kaynak kontrol notu ekle.\n" +
            "5. Kisa, net ve dogrudan Turkce cevap ver.\n";
    }

    private string BuildFallbackPrompt(NlpClassifyResult nlpResult)
    {
        var kbContext = nlpResult.KbAnswer is null
            ? "Bilgi tabaninda guvenilir bir eslesme bulunamadi.\n"
            : $"Bilgi tabaninda zayif/esik alti bir eslesme bulundu (skor: {nlpResult.KbAnswer.Score:P0}, guven: {nlpResult.KbAnswer.Confidence}). Bu eslesmeyi kesin bilgi gibi sunma.\n";

        var reviewLine = nlpResult.NeedsReview
            ? "Bu soruda sistem guveni dusuk. Kesin konusma ve ilgili resmi birime yonlendirme ekle.\n"
            : string.Empty;

        return
            "\n" + kbContext +
            reviewLine +
            "Gorevin:\n" +
            "1. Sadece emin oldugun genel bilgileri kullan.\n" +
            "2. Kesin tarih, ucret, surec ayrintisi uydurma.\n" +
            "3. Belirsizlik varsa acikca bunu belirt.\n" +
            "4. Uygun oldugunda ilgili resmi birime yonlendir.\n" +
            "5. Mumkunse OIDB, BIDB, SKS, ODK veya kutuphane gibi ilgili birimi oner.\n" +
            "6. Kisa, net ve temkinli Turkce cevap ver.\n";
    }

    private static string NormalizeResponseLinks(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        var normalized = Regex.Replace(
            content,
            @"\]\((?!https?://|mailto:|tel:|//)([^)\s]+)\)",
            match => $"](https://{match.Groups[1].Value.TrimStart('/')})");

        normalized = Regex.Replace(
            normalized,
            @"(?<![@/\w:.\-])((?:[a-z0-9-]+\.)+[a-z]{2,}(?:/[^\s)\]]*)?)",
            match =>
            {
                var value = match.Groups[1].Value;
                if (value.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }

                return $"https://{value}";
            },
            RegexOptions.IgnoreCase);

        return normalized;
    }
}
