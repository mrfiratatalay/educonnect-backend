using System.Net.Http.Headers;
using System.Net.Http.Json;
using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduConnect.Infrastructure.Services;

public sealed class VisionEmbeddingApiService : IVisionEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VisionEmbeddingApiService> _logger;

    public VisionEmbeddingApiService(
        HttpClient httpClient,
        IOptions<NlpServiceOptions> options,
        ILogger<VisionEmbeddingApiService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    public async Task<VisionEmbeddingResult> ExtractFeaturesAsync(
        byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "image", "query.jpg");

            var response = await _httpClient.PostAsync("/api/vision/extract", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VisionEmbeddingResult>(cancellationToken);
            return result ?? EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vision embedding extraction failed");
            return EmptyResult();
        }
    }

    public async Task<IReadOnlyCollection<double>> ComputeSimilarityAsync(
        IReadOnlyCollection<double> queryFeatures,
        IReadOnlyCollection<IReadOnlyCollection<double>> candidateFeatures,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                query_features = queryFeatures,
                candidate_features = candidateFeatures
            };

            var response = await _httpClient.PostAsJsonAsync("/api/vision/similarity", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SimilarityApiResponse>(cancellationToken);
            return result?.Similarities ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vision similarity computation failed");
            return [];
        }
    }

    private static VisionEmbeddingResult EmptyResult() => new()
    {
        Features = [],
        Dimension = 0,
        ModelUsed = "unavailable"
    };

    private sealed class SimilarityApiResponse
    {
        public IReadOnlyCollection<double> Similarities { get; init; } = [];
    }
}
