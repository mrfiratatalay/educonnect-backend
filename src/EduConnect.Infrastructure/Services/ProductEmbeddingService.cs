using System.Text.Json;
using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduConnect.Infrastructure.Services;

public sealed class ProductEmbeddingService(
    AppDbContext dbContext,
    IVisionEmbeddingService visionService,
    IHttpClientFactory httpClientFactory,
    ILogger<ProductEmbeddingService> logger) : IProductEmbeddingService
{
    public async Task ExtractAndStoreAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var images = await dbContext.ProductImages
            .Where(x => x.ProductId == productId && x.EmbeddingJson == null)
            .ToListAsync(cancellationToken);

        if (images.Count == 0) return;

        using var httpClient = httpClientFactory.CreateClient();

        foreach (var image in images)
        {
            try
            {
                var imageBytes = await httpClient.GetByteArrayAsync(image.Url, cancellationToken);
                var result = await visionService.ExtractFeaturesAsync(imageBytes, cancellationToken);

                if (result.Features.Count > 0)
                {
                    image.EmbeddingJson = JsonSerializer.Serialize(result.Features);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to extract embedding for image {ImageId}", image.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
