using EduConnect.Api.Common;
using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.Events;
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
public sealed class EventsController(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    NotificationPublisher notificationPublisher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EventResponse>>> GetAll(
        [FromQuery] Guid? groupId,
        CancellationToken cancellationToken)
    {
        var query = QueryEvents();
        if (groupId.HasValue)
        {
            query = query.Where(x => x.GroupId == groupId.Value);
        }

        var events = await query
            .OrderBy(x => x.StartDateUtc)
            .ToListAsync(cancellationToken);

        return Ok(events.Select(x => x.ToResponse(currentUserService.UserId)).ToArray());
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventResponse>> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        if (request.EndDateUtc <= request.StartDateUtc)
        {
            return BadRequest(new { message = "Bitiş tarihi başlangıç tarihinden sonra olmalıdır." });
        }

        if (request.GroupId.HasValue && !await dbContext.Groups.AnyAsync(x => x.Id == request.GroupId.Value && x.IsActive, cancellationToken))
        {
            return BadRequest(new { message = "Seçilen grup bulunamadı." });
        }

        if (request.GroupId.HasValue)
        {
            var membershipRole = await dbContext.GroupMembers
                .Where(x => x.GroupId == request.GroupId.Value && x.UserId == userId.Value)
                .Select(x => (GroupMemberRole?)x.Role)
                .FirstOrDefaultAsync(cancellationToken);

            if (membershipRole is null)
            {
                return BadRequest(new { message = "Grup etkinliği oluşturmak için önce o gruba katılmalısınız." });
            }

            if (membershipRole is not GroupMemberRole.Owner and not GroupMemberRole.Moderator)
            {
                return BadRequest(new { message = "Grup etkinliği oluşturmak için moderatör veya kurucu olmalısınız." });
            }
        }

        var entity = new Event
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Location = request.Location.Trim(),
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc,
            GroupId = request.GroupId,
            MaxParticipants = request.MaxParticipants,
            Category = request.Category.Trim(),
            CreatorUserId = userId.Value
        };

        dbContext.Events.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        entity = await QueryEvents().FirstAsync(x => x.Id == entity.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity.ToResponse(userId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await QueryEvents().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? NotFound() : Ok(entity.ToResponse(currentUserService.UserId));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventResponse>> Update(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        if (request.EndDateUtc <= request.StartDateUtc)
        {
            return BadRequest(new { message = "Bitiş tarihi başlangıç tarihinden sonra olmalıdır." });
        }

        var entity = await dbContext.Events
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        if (!CanManageEvent(entity, userId.Value))
        {
            return Forbid();
        }

        if (request.GroupId.HasValue && !await dbContext.Groups.AnyAsync(x => x.Id == request.GroupId.Value && x.IsActive, cancellationToken))
        {
            return BadRequest(new { message = "Seçilen grup bulunamadı." });
        }

        if (request.GroupId.HasValue)
        {
            var membershipRole = await dbContext.GroupMembers
                .Where(x => x.GroupId == request.GroupId.Value && x.UserId == userId.Value)
                .Select(x => (GroupMemberRole?)x.Role)
                .FirstOrDefaultAsync(cancellationToken);

            if (membershipRole is null)
            {
                return BadRequest(new { message = "Grup etkinliği güncellemek için önce o gruba katılmalısınız." });
            }

            if (membershipRole is not GroupMemberRole.Owner and not GroupMemberRole.Moderator)
            {
                return BadRequest(new { message = "Grup etkinliği güncellemek için moderatör veya kurucu olmalısınız." });
            }
        }

        if (entity.Participants.Count(x => x.Status == EventParticipantStatus.Registered) > request.MaxParticipants)
        {
            return BadRequest(new { message = "Yeni kontenjan mevcut kayitli katilimci sayisindan kucuk olamaz." });
        }

        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.Location = request.Location.Trim();
        entity.StartDateUtc = request.StartDateUtc;
        entity.EndDateUtc = request.EndDateUtc;
        entity.GroupId = request.GroupId;
        entity.MaxParticipants = request.MaxParticipants;
        entity.Category = request.Category.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        entity = await QueryEvents().FirstAsync(x => x.Id == id, cancellationToken);
        return Ok(entity.ToResponse(userId));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var entity = await dbContext.Events
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        if (!CanManageEvent(entity, userId.Value))
        {
            return Forbid();
        }

        if (entity.Participants.Count > 0)
        {
            dbContext.EventParticipants.RemoveRange(entity.Participants);
        }

        dbContext.Events.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/register")]
    public async Task<IActionResult> Register(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var actorName = await GetCurrentUserDisplayNameAsync(userId.Value, cancellationToken);

        var entity = await dbContext.Events
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        var existingParticipant = entity.Participants.FirstOrDefault(x => x.UserId == userId.Value);

        if (existingParticipant?.Status == EventParticipantStatus.Registered)
        {
            return BadRequest(new { message = "Etkinliğe zaten kayıtlısınız." });
        }

        var activeParticipantCount = entity.Participants.Count(x => x.Status == EventParticipantStatus.Registered);
        if (activeParticipantCount >= entity.MaxParticipants)
        {
            return BadRequest(new { message = "Etkinlik kontenjani dolu." });
        }

        if (existingParticipant is null)
        {
            dbContext.EventParticipants.Add(new EventParticipant
            {
                EventId = entity.Id,
                UserId = userId.Value,
                Status = EventParticipantStatus.Registered
            });
        }
        else
        {
            existingParticipant.Status = EventParticipantStatus.Registered;
            existingParticipant.RegisteredAtUtc = DateTime.UtcNow;
        }

        Notification? createdNotification = null;

        if (entity.CreatorUserId != userId.Value)
        {
            createdNotification = new Notification
            {
                UserId = entity.CreatorUserId,
                Title = $"{actorName} etkinligine katildi",
                Message = $"\"{entity.Title}\" için yeni bir katılımcı var.",
                Type = NotificationType.Event,
                TargetPath = "/events"
            };
            dbContext.Notifications.Add(createdNotification);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (createdNotification is not null)
        {
            await notificationPublisher.PublishAsync(createdNotification, cancellationToken);
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var participant = await dbContext.EventParticipants
            .FirstOrDefaultAsync(
                x => x.EventId == id && x.UserId == userId.Value && x.Status == EventParticipantStatus.Registered,
                cancellationToken);

        if (participant is null)
        {
            return NotFound();
        }

        participant.Status = EventParticipantStatus.Cancelled;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private IQueryable<Event> QueryEvents()
    {
        return dbContext.Events
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => !x.GroupId.HasValue || (x.Group != null && x.Group.IsActive))
            .Include(x => x.CreatorUser)
            .Include(x => x.Group)
            .Include(x => x.Participants);
    }

    private async Task<string> GetCurrentUserDisplayNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Bir kullanici";
    }

    private bool CanManageEvent(Event entity, Guid currentUserId)
    {
        var isPrivileged =
            User.IsInRole(UserRole.Admin.ToString()) ||
            User.IsInRole(UserRole.Moderator.ToString());

        return isPrivileged || entity.CreatorUserId == currentUserId;
    }
}
