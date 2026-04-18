using System.Net.Http.Json;
using System.Text.Json;
using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduConnect.Infrastructure.Services;

public sealed class NlpApiService : INlpService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<NlpApiService> _logger;

    public NlpApiService(HttpClient httpClient, IOptions<NlpServiceOptions> options, ILogger<NlpApiService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
        _logger = logger;
    }

    public async Task<NlpClassifyResult> ClassifyAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/nlp/classify",
                new { text },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<NlpClassifyResult>(JsonOptions, cancellationToken);
            return result ?? FallbackResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NLP service call failed, using fallback");
            return FallbackResult();
        }
    }

    private static NlpClassifyResult FallbackResult()
    {
        return new NlpClassifyResult
        {
            Intent = "student_services",
            Confidence = 0.3,
            ConfidenceBand = "low",
            NeedsReview = true,
            ResolverUsed = false,
            Entities = [],
            ModelUsed = "fallback"
        };
    }
}
