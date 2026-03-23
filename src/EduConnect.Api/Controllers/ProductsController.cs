using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.Common;
using EduConnect.Application.Contracts.Products;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Domain.Enums;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ProductsController(AppDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetAll(
        [FromQuery] ProductFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var query = QueryProducts().Where(x => x.IsActive);

        if (filters.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == filters.CategoryId.Value);
        }

        if (filters.MinPrice.HasValue)
        {
            query = query.Where(x => x.Price >= filters.MinPrice.Value);
        }

        if (filters.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= filters.MaxPrice.Value);
        }

        if (filters.Condition.HasValue)
        {
            query = query.Where(x => x.Condition == filters.Condition.Value);
        }

        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 50);
        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<ProductResponse>
        {
            Items = products.Select(x => x.ToResponse()).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        if (request.CategoryId.HasValue &&
            !await dbContext.Categories.AnyAsync(x => x.Id == request.CategoryId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Kategori bulunamadı." });
        }

        var product = new Product
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            CategoryId = request.CategoryId,
            SellerId = userId.Value,
            Condition = request.Condition,
            City = request.City.Trim(),
            IsNegotiable = request.IsNegotiable,
            Images = request.ImageUrls
                .Distinct()
                .Select((url, index) => new ProductImage
                {
                    Url = url.Trim(),
                    SortOrder = index
                })
                .ToArray()
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        product = await QueryProducts().FirstAsync(x => x.Id == product.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product.ToResponse());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await QueryProducts().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return product is null ? NotFound() : Ok(product.ToResponse());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var product = await dbContext.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var isPrivileged = User.IsInRole(UserRole.Admin.ToString()) || User.IsInRole(UserRole.Moderator.ToString());
        if (!isPrivileged && product.SellerId != userId.Value)
        {
            return Forbid();
        }

        if (request.CategoryId.HasValue &&
            !await dbContext.Categories.AnyAsync(x => x.Id == request.CategoryId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Kategori bulunamadı." });
        }

        product.Title = request.Title.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.Condition = request.Condition;
        product.City = request.City.Trim();
        product.IsNegotiable = request.IsNegotiable;

        dbContext.ProductImages.RemoveRange(product.Images);
        product.Images = request.ImageUrls
            .Distinct()
            .Select((url, index) => new ProductImage
            {
                ProductId = product.Id,
                Url = url.Trim(),
                SortOrder = index
            })
            .ToArray();

        await dbContext.SaveChangesAsync(cancellationToken);

        product = await QueryProducts().FirstAsync(x => x.Id == product.Id, cancellationToken);
        return Ok(product.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        var isPrivileged = User.IsInRole(UserRole.Admin.ToString()) || User.IsInRole(UserRole.Moderator.ToString());
        if (!isPrivileged && product.SellerId != userId.Value)
        {
            return Forbid();
        }

        product.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<Product> QueryProducts()
    {
        return dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Category)
            .Include(x => x.Seller)
            .Include(x => x.Images);
    }
}
