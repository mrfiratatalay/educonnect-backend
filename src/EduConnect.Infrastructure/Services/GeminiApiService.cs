using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Options;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduConnect.Infrastructure.Services;

public sealed class GeminiApiService : IGeminiApiService, IDisposable
{
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiApiService> _logger;
    private readonly Client _client;

    private const string SystemPrompt =
        "Sen EduConnect platformunun yapay zeka asistanisin. " +
        "Recep Tayyip Erdogan Universitesi ogrencilerine Turkce yardim ediyorsun. " +
        "RTEU Zihni Derin Yerleskesi, Fener Mah., 53100 Rize'dedir. " +
        "Temel iletisim: Ogrenci Isleri 444 01 99 (oidb@erdogan.edu.tr), " +
        "Bilgi Islem 0464 223 3180 (bidb.erdogan.edu.tr), SKS sks@erdogan.edu.tr, " +
        "Ogrenci Destek 0464 223 4170 (odk.erdogan.edu.tr). " +
        "OBS: obs.erdogan.edu.tr (REBIS). Kutuphane: kutuphanedb.erdogan.edu.tr. " +
        "TEMEL DAVRANIS: Yardimci ve dogrudan ol. Gecirstirici cevap verme. " +
        "Bilgi tabani (KB) eslesmesi gelmisse onu kullanarak SOMUT cevap ver; sadece url'e yonlendirip kacma. " +
        "KB cevabini oldugu gibi tekrarlamak yerine kullanicinin dilini ve baglamini kullanarak yeniden ifade et, " +
        "gerekirse kendi genel bilginle (RTEU veya universite hayati hakkinda) tamamla. " +
        "KONUSMA TAKIBI: Kullanici onceki cevabina ek soru sorduysa konusma gecmisine bak; " +
        "ayni cevabi tekrarlama, kullanici neyin pesindeyse oraya odaklan, " +
        "varsayilan/genel yanit yerine spesifik yanit ver. " +
        "Eger kullanici 'biraz daha aciklar misin', 'tam olarak nasil', 'ornek verir misin' gibi takip sorusu sorduysa " +
        "onceki cevabini genisleterek devam et, sifirdan baslama. " +
        "BELIRSIZLIK: Sadece kesin emin olmadigin tarih, ucret veya surec ayrintisinda 'resmi kaynak kontrol et' de. " +
        "Bunun disinda kafadan atip 'birim oner, duyuru takip et' deme. " +
        "Bir baglanti vereceksen yalnizca tam ve mutlak URL ver; https:// ile baslat. " +
        "Asla localhost, goreli yol veya protokolsuz domain verme. " +
        "Markdown link kullanirsan href de mutlaka https:// ile baslasin. " +
        "Kisa, net ve samimi cevaplar ver. Cevaplarinda emoji kullanma. Markdown kullanabilirsin.";

    public GeminiApiService(IOptions<GeminiOptions> options, ILogger<GeminiApiService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Client(apiKey: _options.ApiKey);
    }

    public async Task<string> GenerateTextAsync(
        string userMessage,
        IReadOnlyCollection<(string Role, string Content)>? history = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contents = new List<Content>();

            if (history is { Count: > 0 })
            {
                foreach (var (role, content) in history.TakeLast(20))
                {
                    var geminiRole = role == "assistant" ? "model" : "user";
                    contents.Add(new Content
                    {
                        Role = geminiRole,
                        Parts = [new Part { Text = content }]
                    });
                }
            }

            contents.Add(new Content
            {
                Role = "user",
                Parts = [new Part { Text = userMessage }]
            });

            var config = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Parts = [new Part { Text = SystemPrompt }]
                },
                MaxOutputTokens = _options.MaxOutputTokens,
                Temperature = _options.Temperature
            };

            var response = await _client.Models.GenerateContentAsync(
                model: _options.ChatModel,
                contents: contents,
                config: config,
                cancellationToken: cancellationToken);

            return response.Text ?? "Uzgunum, su an yanit uretemiyorum.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini text generation failed");
            throw;
        }
    }

    public async Task<string> AnalyzeImageAsync(
        byte[] imageBytes,
        string mimeType,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contents = new List<Content>
            {
                new()
                {
                    Role = "user",
                    Parts =
                    [
                        new Part
                        {
                            InlineData = new Blob
                            {
                                MimeType = mimeType,
                                Data = imageBytes
                            }
                        },
                        new Part { Text = prompt }
                    ]
                }
            };

            var config = new GenerateContentConfig
            {
                ResponseMimeType = "application/json",
                MaxOutputTokens = 1024,
                Temperature = 0.2
            };

            var response = await _client.Models.GenerateContentAsync(
                model: _options.VisionModel,
                contents: contents,
                config: config,
                cancellationToken: cancellationToken);

            return response.Text ?? "{}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini vision analysis failed");
            throw;
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
