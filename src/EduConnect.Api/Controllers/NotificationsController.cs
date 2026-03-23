using EduConnect.Api.Mappings;
using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class NotificationsController(AppDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var notifications = await dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notifications.Select(x => x.ToResponse()).ToArray());
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value, cancellationToken);

        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var notifications = await dbContext.Notifications
            .Where(x => x.UserId == userId.Value && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
