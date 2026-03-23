using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.Discounts;
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
public sealed class DiscountsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DiscountResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var discounts = await dbContext.Discounts
            .AsNoTracking()
            .Where(x => x.IsActive && x.ValidUntilUtc > DateTime.UtcNow)
            .OrderBy(x => x.ValidUntilUtc)
            .ToListAsync(cancellationToken);

        return Ok(discounts.Select(x => x.ToResponse()).ToArray());
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    public async Task<ActionResult<DiscountResponse>> Create(
        [FromBody] CreateDiscountRequest request,
        CancellationToken cancellationToken)
    {
        var discount = new Discount
        {
            BusinessName = request.BusinessName.Trim(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DiscountRate = request.DiscountRate,
            DiscountCode = request.DiscountCode.Trim(),
            LogoUrl = request.LogoUrl?.Trim(),
            ValidUntilUtc = request.ValidUntilUtc,
            IsActive = true
        };

        dbContext.Discounts.Add(discount);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = discount.Id }, discount.ToResponse());
    }
}
