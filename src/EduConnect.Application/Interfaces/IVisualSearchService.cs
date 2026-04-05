using EduConnect.Application.Contracts.VisualSearch;

namespace EduConnect.Application.Interfaces;

public interface IVisualSearchService
{
    Task<VisualSearchSearchResponse> SearchAsync(
        byte[] imageBytes,
        string mimeType,
        VisualSearchSearchRequest request,
        CancellationToken cancellationToken = default);
}
