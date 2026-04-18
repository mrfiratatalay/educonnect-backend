using EduConnect.Application.Contracts.Products;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CategoriesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryResponse { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }
}
