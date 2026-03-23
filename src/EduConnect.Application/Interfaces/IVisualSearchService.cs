using EduConnect.Application.Contracts.VisualSearch;

namespace EduConnect.Application.Interfaces;

public interface IVisualSearchService
{
    Task<VisualSearchAnalysisResponse> SearchByImageAsync(
        byte[] imageBytes,
        string mimeType,
        int maxResults = 8,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VisualSearchMatch>> SearchAsync(
        string queryImageUrl,
        IReadOnlyCollection<VisualSearchCandidate> candidates,
        int maxResults,
        CancellationToken cancellationToken = default);
}
