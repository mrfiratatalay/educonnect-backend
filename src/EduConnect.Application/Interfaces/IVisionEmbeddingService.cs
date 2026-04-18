namespace EduConnect.Application.Interfaces;

public interface IVisionEmbeddingService
{
    Task<VisionEmbeddingResult> ExtractFeaturesAsync(byte[] imageBytes, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<double>> ComputeSimilarityAsync(
        IReadOnlyCollection<double> queryFeatures,
        IReadOnlyCollection<IReadOnlyCollection<double>> candidateFeatures,
        CancellationToken cancellationToken = default);
}

public sealed class VisionEmbeddingResult
{
    public IReadOnlyCollection<double> Features { get; init; } = [];
    public int Dimension { get; init; }
    public string ModelUsed { get; init; } = string.Empty;
}
