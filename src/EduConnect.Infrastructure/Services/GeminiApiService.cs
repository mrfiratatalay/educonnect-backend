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
        "Sen EduConnect kampüs asistanısın. " +
        "Üniversite öğrencilerine Türkçe yardım ediyorsun. " +
        "Kısa, net ve samimi cevaplar ver. " +
        "Bilmediğini açıkça söyle ve ilgili birimi yönlendir. " +
        "Cevaplarında emoji kullanma. Markdown biçimlendirme kullanabilirsin.";

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
                foreach (var (role, content) in history.TakeLast(10))
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

            return response.Text ?? "Üzgünüm, şu an yanıt üretemiyorum.";
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
