namespace EduConnect.Application.Interfaces;

public interface IProductEmbeddingService
{
    Task ExtractAndStoreAsync(Guid productId, CancellationToken cancellationToken = default);
}
