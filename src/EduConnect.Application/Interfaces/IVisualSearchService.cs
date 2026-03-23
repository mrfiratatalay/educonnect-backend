using EduConnect.Application.Contracts.VisualSearch;

namespace EduConnect.Application.Interfaces;

public interface IVisualSearchService
{
    Task<IReadOnlyCollection<VisualSearchMatch>> SearchAsync(
        string queryImageUrl,
        IReadOnlyCollection<VisualSearchCandidate> candidates,
        int maxResults,
        CancellationToken cancellationToken = default);
}
