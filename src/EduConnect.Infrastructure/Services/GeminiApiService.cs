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
        "Recep Tayyip Erdogan Universitesi (RTEU) ogrencilerine Turkce yardim ediyorsun. " +
        "RTEU Zihni Derin Yerleskesi, Fener Mah., 53100 Rize'dedir. " +
        "Iletisim noktalari (KULLANICI SORARSA SOYLE, kendiliginden yapistirma): " +
        "OIDB 444 01 99, oidb@erdogan.edu.tr; BIDB 0464 223 3180; SKS sks@erdogan.edu.tr; " +
        "ODK 0464 223 4170; OBS obs.erdogan.edu.tr; Kutuphane kutuphanedb.erdogan.edu.tr. " +
        "\n" +
        "TEMEL DAVRANIS:\n" +
        "- Yardimci, dogrudan ve faydali ol. Asla geciktirici/kacis cevap verme.\n" +
        "- 'Resmi kaynak kontrol et', 'X biriminden bilgi al', 'duyurulardan takip et' gibi kalip ifadeleri " +
        "ana cevap olarak KULLANMA. Kullanici bunlari zaten biliyor.\n" +
        "- Eger gercekten bilmiyorsan, en azindan TIPIK / GENEL bir tahmin ver " +
        "(ornek: 'Turk universitelerinde yemekhaneler genelde 11:30-13:30 ogle, 17:00-19:00 aksam saatlerinde acik olur, " +
        "RTEU icin de buna yakin olmasi muhtemel'). Kullanicinin elinin bos donmemesi sart.\n" +
        "- Spesifik kesin bilmedigin nokta varsa (kesin tarih, kesin ucret, kesin oda no): " +
        "cevabin SONUNDA tek cumle ile 'bu spesifik detayi resmi kaynaktan teyit etmek faydali olur' " +
        "diyebilirsin - ama cevabin tamami bu uyari olmasin.\n" +
        "\n" +
        "BILGI TABANI (KB) ESLESMESI:\n" +
        "- KB cevabi varsa ve icerikte SOMUT bilgi varsa, onu kullanicinin diliyle yeniden ifade et, gerekirse genislet.\n" +
        "- KB cevabi yalnizca 'X biriminden bilgi alabilirsin', 'duyurulardan takip edilmelidir' gibi " +
        "yonlendirme iceriyorsa, bunu YOK SAY ve kendi genel bilginle somut cevap ver. " +
        "KB'deki url'i cevabin sonunda kaynak olarak verebilirsin ama ana bilgi olmasin.\n" +
        "\n" +
        "KONUSMA TAKIBI:\n" +
        "- Konusma gecmisine bak. Kullanici onceki cevaba ek soru sorduysa (\"biraz daha aciklar misin\", " +
        "\"peki ya\", \"yani\", \"ornek ver\") onceki cevabini TEKRARLAMA. " +
        "Farkli bir aciya odaklan, somut ornek/adim ekle, daha detayli ac.\n" +
        "\n" +
        "BIcim:\n" +
        "- Kisa, net, samimi Turkce. Emoji yok. Markdown kullanabilirsin.\n" +
        "- Link verirken https:// ile basla, localhost veya goreli yol yazma.";

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
