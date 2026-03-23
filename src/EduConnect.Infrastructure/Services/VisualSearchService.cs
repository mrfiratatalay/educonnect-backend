using System.Security.Cryptography;
using System.Text;
using EduConnect.Application.Contracts.VisualSearch;
using EduConnect.Application.Interfaces;

namespace EduConnect.Infrastructure.Services;

public sealed class VisualSearchService : IVisualSearchService
{
    public Task<IReadOnlyCollection<VisualSearchMatch>> SearchAsync(
        string queryImageUrl,
        IReadOnlyCollection<VisualSearchCandidate> candidates,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var matches = candidates
            .Select(candidate =>
            {
                var score = CalculatePseudoSimilarity(queryImageUrl, candidate.ProductId);
                return new VisualSearchMatch
                {
                    ProductId = candidate.ProductId,
                    SimilarityScore = score,
                    Rank = 0
                };
            })
            .OrderByDescending(x => x.SimilarityScore)
            .Take(maxResults)
            .Select((match, index) => new VisualSearchMatch
            {
                ProductId = match.ProductId,
                SimilarityScore = match.SimilarityScore,
                Rank = index + 1
            })
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<VisualSearchMatch>>(matches);
    }

    private static double CalculatePseudoSimilarity(string queryImageUrl, Guid productId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{queryImageUrl}:{productId}"));
        var value = BitConverter.ToUInt32(bytes, 0);
        var normalized = value / (double)uint.MaxValue;
        return Math.Round(0.55 + (normalized * 0.44), 4);
    }
}
