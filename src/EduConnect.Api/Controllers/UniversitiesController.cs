using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.Universities;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public sealed class UniversitiesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UniversityOptionResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var universities = await dbContext.Universities
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return Ok(universities.Select(item => item.ToResponse()).ToArray());
    }
}
