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
public sealed class EventsController(AppDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EventResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var events = await QueryEvents()
            .OrderBy(x => x.StartDateUtc)
            .ToListAsync(cancellationToken);

        return Ok(events.Select(x => x.ToResponse(currentUserService.UserId)).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<EventResponse>> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        if (request.EndDateUtc <= request.StartDateUtc)
        {
            return BadRequest(new { message = "Bitis tarihi baslangic tarihinden sonra olmalidir." });
        }

        if (request.GroupId.HasValue && !await dbContext.Groups.AnyAsync(x => x.Id == request.GroupId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Secilen grup bulunamadi." });
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
            return BadRequest(new { message = "Etkinlige zaten kayitlisiniz." });
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

        if (entity.CreatorUserId != userId.Value)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = entity.CreatorUserId,
                Title = $"{actorName} etkinligine katildi",
                Message = $"\"{entity.Title}\" icin yeni bir katilimci var.",
                Type = NotificationType.Event,
                TargetPath = "/events"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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
}
