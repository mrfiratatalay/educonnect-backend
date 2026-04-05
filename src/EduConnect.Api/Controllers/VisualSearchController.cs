using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.VisualSearch;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("gemini")]
[Route("api/[controller]")]
public sealed class VisualSearchController(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IVisualSearchService visualSearchService) : ControllerBase
{
    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/jpg", "image/png", "image/webp"];

    [AllowAnonymous]
    [HttpPost("searches")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<VisualSearchSearchResponse>> Search(
        [FromForm] IFormFile image,
        [FromForm] VisualSearchSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (image is null || image.Length == 0)
        {
            return BadRequest("Gorsel yuklenmedi.");
        }

        if (!AllowedMimeTypes.Contains(image.ContentType?.ToLowerInvariant() ?? string.Empty))
        {
            return BadRequest("Desteklenmeyen format. JPEG, PNG veya WEBP kullanin.");
        }

        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice.Value > request.MaxPrice.Value)
        {
            return BadRequest("Minimum fiyat maksimum fiyattan buyuk olamaz.");
        }

        if (request.CategoryId.HasValue &&
            !await dbContext.Categories.AnyAsync(category => category.Id == request.CategoryId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Kategori bulunamadi." });
        }

        await using var ms = new MemoryStream();
        await image.CopyToAsync(ms, cancellationToken);

        var response = await visualSearchService.SearchAsync(
            ms.ToArray(),
            image.ContentType!,
            request,
            cancellationToken);

        var userId = currentUserService.UserId;
        if (userId.HasValue)
        {
            var history = new VisualSearchHistory
            {
                UserId = userId.Value,
                QueryImageUrl = $"uploaded:{image.FileName}",
                ResultCount = response.TotalFound
            };

            history.Results = response.Results
                .Select(result => new VisualSearchResult
                {
                    ProductId = result.ProductId,
                    SimilarityScore = result.SimilarityScore,
                    Rank = result.Rank
                })
                .ToArray();

            dbContext.VisualSearchHistories.Add(history);
            await dbContext.SaveChangesAsync(cancellationToken);
            response.SearchId = history.Id;
        }

        return Ok(response);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyCollection<VisualSearchHistoryResponse>>> GetHistory(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var history = await dbContext.VisualSearchHistories
            .AsNoTracking()
            .AsSplitQuery()
            .Where(item => item.UserId == userId.Value)
            .Include(item => item.Results)
            .ThenInclude(result => result.Product)
            .ThenInclude(product => product.Images)
            .Include(item => item.Results)
            .ThenInclude(result => result.Product)
            .ThenInclude(product => product.Category)
            .Include(item => item.Results)
            .ThenInclude(result => result.Product)
            .ThenInclude(product => product.Seller)
            .OrderByDescending(item => item.SearchedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        return Ok(history.Select(item => item.ToResponse()).ToArray());
    }
}
