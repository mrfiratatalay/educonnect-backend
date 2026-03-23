using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.Groups;
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
public sealed class GroupsController(AppDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<GroupResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var groups = await QueryGroups()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(groups.Select(x => x.ToResponse(currentUserService.UserId)).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<GroupResponse>> Create([FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var group = new Group
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            CreatorUserId = userId.Value
        };

        dbContext.Groups.Add(group);
        dbContext.GroupMembers.Add(new GroupMember
        {
            Group = group,
            UserId = userId.Value,
            Role = GroupMemberRole.Owner
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        group = await QueryGroups().FirstAsync(x => x.Id == group.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group.ToResponse(userId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GroupResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var group = await QueryGroups()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return group is null ? NotFound() : Ok(group.ToResponse(currentUserService.UserId));
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group is null)
        {
            return NotFound();
        }

        var alreadyMember = await dbContext.GroupMembers.AnyAsync(
            x => x.GroupId == id && x.UserId == userId.Value,
            cancellationToken);

        if (alreadyMember)
        {
            return BadRequest(new { message = "Kullanıcı zaten bu grupta." });
        }

        dbContext.GroupMembers.Add(new GroupMember
        {
            GroupId = id,
            UserId = userId.Value
        });

        if (group.CreatorUserId != userId.Value)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = group.CreatorUserId,
                Title = "Yeni grup üyesi",
                Message = $"{User.Identity?.Name ?? "Bir kullanıcı"} grubunuza katıldı.",
                Type = NotificationType.Social
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var membership = await dbContext.GroupMembers
            .FirstOrDefaultAsync(x => x.GroupId == id && x.UserId == userId.Value, cancellationToken);

        if (membership is null)
        {
            return NotFound();
        }

        if (membership.Role == GroupMemberRole.Owner)
        {
            return BadRequest(new { message = "Grup sahibi kendi oluşturduğu gruptan ayrılamaz." });
        }

        dbContext.GroupMembers.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<Group> QueryGroups()
    {
        return dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.CreatorUser)
            .Include(x => x.Members);
    }
}
