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
    private const double GroundedKbThreshold = 0.40;

    public async Task<ChatbotReply> GetReplyAsync(
        string message,
        IReadOnlyCollection<(string Role, string Content)>? conversationHistory = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Eger kullanici takip sorusu soruyorsa (ornek: "biraz daha aciklar misin"), tek basina anlamsiz olabilir.
            // Son 2 user mesajini birlestirerek NLP'ye gondererek intent classification'a baglam kazandiriyoruz.
            var nlpInputMessage = BuildContextualMessageForNlp(message, conversationHistory);
            var nlpResult = await nlpService.ClassifyAsync(nlpInputMessage, cancellationToken);
            var kbHit = nlpResult.KbAnswer is not null && nlpResult.KbAnswer.Score >= GroundedKbThreshold;
            var isFallback = nlpResult.ModelUsed is "keyword-fallback" or "fallback" or "hybrid-fallback";

            try
            {
                var enrichedPrompt = BuildEnrichedPrompt(message, nlpResult, conversationHistory);
                var aiResponse = await geminiApiService.GenerateTextAsync(
                    enrichedPrompt, conversationHistory, cancellationToken);
                aiResponse = NormalizeResponseLinks(aiResponse);

                stopwatch.Stop();

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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Gemini generation failed; using local NLP/KB response");
                stopwatch.Stop();

                return new ChatbotReply
                {
                    Content = BuildLocalResponse(nlpResult),
                    IntentDetected = nlpResult.Intent,
                    Confidence = nlpResult.Confidence,
                    ConfidenceBand = nlpResult.ConfidenceBand,
                    NeedsReview = nlpResult.NeedsReview,
                    ModelUsed = kbHit ? $"{nlpResult.ModelUsed}+local-kb" : $"{nlpResult.ModelUsed}+local-fallback",
                    KbScore = nlpResult.KbAnswer?.Score,
                    KbHit = kbHit,
                    IsFallback = !kbHit || isFallback,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                };
            }
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

    private string BuildLocalResponse(NlpClassifyResult nlpResult)
    {
        if (IsGrounded(nlpResult))
        {
            var kb = nlpResult.KbAnswer!;
            var response = kb.Answer.Trim();

            if (kb.TimeSensitive)
            {
                response += "\n\nBu bilgi zamanla değişebilir; kesin tarih, ücret veya başvuru süreci için resmî kaynağı kontrol etmen daha güvenli olur.";
            }

            if (!string.IsNullOrWhiteSpace(kb.SourceUrl))
            {
                response += $"\n\nKaynak: {kb.SourceUrl}";
            }

            return NormalizeResponseLinks(response);
        }

        if (nlpResult.KbAnswer is not null)
        {
            var kb = nlpResult.KbAnswer;
            var response = $"Tam emin değilim ama bilgi tabanında şuna benzer bir cevap buldum:\n\n{kb.Answer.Trim()}";
            if (!string.IsNullOrWhiteSpace(kb.SourceUrl))
            {
                response += $"\n\nDaha kesin bilgi için: {kb.SourceUrl}";
            }
            return NormalizeResponseLinks(response);
        }

        return "Şu an AI servisine ulaşamıyorum. Sorunu birkaç saniye sonra tekrar yazabilir misin?";
    }

    private string BuildEnrichedPrompt(
        string originalMessage,
        NlpClassifyResult nlpResult,
        IReadOnlyCollection<(string Role, string Content)>? conversationHistory)
    {
        var entityInfo = nlpResult.Entities.Count > 0
            ? string.Join(", ", nlpResult.Entities.Select(e => $"{e.Type}: {e.Value}"))
            : "yok";

        var isFollowUp = LooksLikeFollowUp(originalMessage) && conversationHistory is { Count: > 0 };

        var prompt =
            $"Kullanici sorusu: \"{originalMessage}\"\n" +
            $"Tespit edilen niyet: {nlpResult.Intent} (guven: {nlpResult.Confidence:P0})\n" +
            $"Guven bandi: {nlpResult.ConfidenceBand}\n" +
            $"Tespit edilen varliklar: {entityInfo}\n";

        if (isFollowUp)
        {
            prompt +=
                "\nUYARI: Bu mesaj onceki konusmanin DEVAMI gibi gorunuyor (\"biraz daha aciklar misin\", " +
                "\"tam olarak nasil\", \"yani\" gibi takip ifadeleri var). Konusma gecmisine bak; " +
                "onceki cevabini aynen tekrarlama, kullanici neyi merak ediyorsa o detaya odaklan ve " +
                "yeni bir aci/genisletme ile yanitla.\n";
        }

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

    private static bool LooksLikeFollowUp(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var lower = message.Trim().ToLowerInvariant();
        string[] followUpPatterns =
        {
            "biraz daha", "tam olarak", "yani", "peki", "ornek", "nasil yani",
            "neden", "anlamadim", "aciklar misin", "detay", "devam", "baska", "ya da",
            "iste", "evet ama", "tamam ama"
        };
        return followUpPatterns.Any(p => lower.Contains(p)) || message.Length < 25;
    }

    private static string BuildContextualMessageForNlp(
        string originalMessage,
        IReadOnlyCollection<(string Role, string Content)>? conversationHistory)
    {
        if (!LooksLikeFollowUp(originalMessage) || conversationHistory is null || conversationHistory.Count == 0)
        {
            return originalMessage;
        }

        // Son user mesajini bul ve yenisinin onune koy ki intent classifier baglami yakalayabilsin.
        var lastUserMessage = conversationHistory
            .Where(x => x.Role == "user")
            .Select(x => x.Content)
            .LastOrDefault();

        if (string.IsNullOrWhiteSpace(lastUserMessage))
        {
            return originalMessage;
        }

        return $"{lastUserMessage} {originalMessage}";
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
            ? "NOT: Bu konuda kesin tarih/ucret gibi degisken bilgi varsa cevabin sonunda kisaca 'guncel bilgi icin resmi kaynaktan dogrula' demek yeterli; ama cevabi bununla baslatma, asil bilgiyi ver.\n"
            : string.Empty;

        return
            $"\nBILGI TABANI ESLESMESI:\n" +
            $"  Konu: {kb.Topic}\n" +
            $"  Kapsam: {kb.FacultyScope}\n" +
            $"  KB sorusu: {kb.Question}\n" +
            $"  KB cevabi: {kb.Answer}\n" +
            (string.IsNullOrWhiteSpace(kb.SourceUrl) ? string.Empty : $"  Kaynak: {kb.SourceUrl}\n") +
            cautionLine +
            "\nGOREVIN:\n" +
            "1. KB cevabini ANA kaynak olarak kullan, ama oldugu gibi kopyalama.\n" +
            "2. Kullanicinin sordugu spesifik aciya odaklanarak, KB icerigini onun diliyle yeniden anlat.\n" +
            "3. Kullanici takip sorusu soruyorsa (konusma gecmisine bak), ayni cevabi tekrarlama; " +
            "KB cevabini farkli bir aciya gore genislet, ornek ver, somut adimlar sun.\n" +
            "4. Eger KB cevabi kullanicinin asil sorusunu birebir karsilamiyorsa, neyi karsiladigini soyle " +
            "ve ek olarak ne yapabilecegini de ekle.\n" +
            "5. 'Kaynaktan kontrol et' diyerek kacma; once bildigin somut bilgiyi paylas. " +
            "Kaynak URL'i KB'de varsa cevabin sonuna ekle, cevabin tamami kaynaga yonlendirme olmasin.\n" +
            "6. Kisa, akici, dogrudan Turkce ver.\n";
    }

    private string BuildFallbackPrompt(NlpClassifyResult nlpResult)
    {
        var kbContext = nlpResult.KbAnswer is null
            ? "Bilgi tabaninda spesifik eslesme bulunamadi.\n"
            : $"Bilgi tabaninda zayif bir eslesme var (skor: {nlpResult.KbAnswer.Score:P0}). Ipucu olarak bakabilirsin ama tek dayanak yapma.\n";

        return
            "\n" + kbContext +
            "GOREVIN:\n" +
            "1. Kullaniciya YARDIM ET. Geneliyle bildigin RTEU veya universite hayati bilgisini kullan.\n" +
            "2. Konusma gecmisine bak; bu mesaj onceki konusmanin devamiysa o baglami koru, sifirdan baslama.\n" +
            "3. Spesifik kesin bilmedigin bir sey (tam tarih, kesin ucret, oda numarasi gibi) varsa o tek noktayi " +
            "'bunu kesin bilemiyorum, X biriminden teyit etmen lazim' diyerek isaretle - ama ust katmanda yardimci cevap ver.\n" +
            "4. Tum cevabi 'bilgi tabaninda yok, birime sor' diye gecirstirme; bu kabul edilemez.\n" +
            "5. Kullanici cidden bir cevap istiyor; en azindan baslangic, yon ve sorabilecegi sorulari ver.\n" +
            "6. Kisa, net, samimi Turkce yaz.\n";
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
