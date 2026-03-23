using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.VisualSearch;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class VisualSearchController(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IVisualSearchService visualSearchService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IReadOnlyCollection<VisualSearchResultResponse>>> Search(
        [FromBody] VisualSearchRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Include(x => x.Images)
            .ToListAsync(cancellationToken);

        var candidates = products
            .Select(x => new VisualSearchCandidate
            {
                ProductId = x.Id,
                Title = x.Title,
                Price = x.Price,
                ImageUrl = x.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault()
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .ToArray();

        var matches = await visualSearchService.SearchAsync(
            request.QueryImageUrl,
            candidates,
            request.MaxResults,
            cancellationToken);

        var productMap = products.ToDictionary(x => x.Id);
        var history = new VisualSearchHistory
        {
            UserId = userId.Value,
            QueryImageUrl = request.QueryImageUrl,
            ResultCount = matches.Count
        };

        history.Results = matches
            .Select(match => new VisualSearchResult
            {
                ProductId = match.ProductId,
                SimilarityScore = match.SimilarityScore,
                Rank = match.Rank
            })
            .ToArray();

        dbContext.VisualSearchHistories.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = matches
            .Select(match =>
            {
                var product = productMap[match.ProductId];
                return new VisualSearchResultResponse
                {
                    ProductId = product.Id,
                    Title = product.Title,
                    Price = product.Price,
                    ImageUrl = product.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault(),
                    SimilarityScore = match.SimilarityScore,
                    Rank = match.Rank
                };
            })
            .ToArray();

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
            .Where(x => x.UserId == userId.Value)
            .Include(x => x.Results)
            .ThenInclude(x => x.Product)
            .ThenInclude(x => x.Images)
            .OrderByDescending(x => x.SearchedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        return Ok(history.Select(x => x.ToResponse()).ToArray());
    }
}
