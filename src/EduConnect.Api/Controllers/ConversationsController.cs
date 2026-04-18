using EduConnect.Api.Common;
using EduConnect.Api.Hubs;
using EduConnect.Application.Contracts.DirectMessages;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ConversationsController(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IHubContext<DirectMessageHub> directMessageHub,
    NotificationPublisher notificationPublisher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryResponse>>> GetConversations(
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var rows = await dbContext.DirectConversations
            .AsNoTracking()
            .Where(c => c.UserLowerId == userId || c.UserHigherId == userId)
            .OrderByDescending(c => c.LastMessageAtUtc)
            .Select(c => new
            {
                c.Id,
                c.UserLowerId,
                c.UserHigherId,
                c.LastMessageAtUtc,
                LowerName = c.UserLower.FullName,
                LowerAvatar = c.UserLower.StudentProfile != null ? c.UserLower.StudentProfile.AvatarUrl : null,
                HigherName = c.UserHigher.FullName,
                HigherAvatar = c.UserHigher.StudentProfile != null ? c.UserHigher.StudentProfile.AvatarUrl : null,
                LastContent = c.Messages
                    .OrderByDescending(m => m.SentAtUtc)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var me = userId.Value;
        var result = rows.Select(r =>
        {
            var isLower = r.UserLowerId == me;
            var otherName = isLower ? r.HigherName : r.LowerName;
            var otherAvatar = isLower ? r.HigherAvatar : r.LowerAvatar;
            var otherId = isLower ? r.UserHigherId : r.UserLowerId;
            var preview = r.LastContent is { Length: > 80 } ? r.LastContent[..80] + "…" : r.LastContent;

            return new ConversationSummaryResponse
            {
                Id = r.Id,
                OtherUserId = otherId,
                OtherUserName = otherName,
                OtherUserAvatarUrl = otherAvatar,
                LastMessagePreview = preview,
                LastMessageAtUtc = r.LastMessageAtUtc,
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{conversationId:guid}/messages")]
    public async Task<ActionResult<PagedMessagesResponse>> GetMessages(
        Guid conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var conv = await dbContext.DirectConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && (c.UserLowerId == userId || c.UserHigherId == userId),
                cancellationToken);

        if (conv is null)
        {
            return NotFound();
        }

        var query = dbContext.DirectMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.SentAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new DirectMessageItemResponse
            {
                Id = m.Id,
                SenderUserId = m.SenderUserId,
                Content = m.Content,
                SentAtUtc = m.SentAtUtc,
            })
            .ToListAsync(cancellationToken);

        items.Reverse();

        return Ok(new PagedMessagesResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        });
    }

    [HttpPost]
    public async Task<ActionResult<StartConversationResponse>> StartConversation(
        [FromBody] StartConversationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var otherId = request.OtherUserId;
        if (otherId == userId)
        {
            return BadRequest(new { message = "Kendinizle sohbet baslatilamaz." });
        }

        var otherExists = await dbContext.Users.AnyAsync(x => x.Id == otherId && x.IsActive, cancellationToken);
        if (!otherExists)
        {
            return NotFound(new { message = "Kullanici bulunamadi." });
        }

        OrderPair(userId.Value, otherId, out var lower, out var higher);

        var existing = await dbContext.DirectConversations
            .FirstOrDefaultAsync(
                c => c.UserLowerId == lower && c.UserHigherId == higher,
                cancellationToken);

        if (existing is not null)
        {
            return Ok(new StartConversationResponse
            {
                ConversationId = existing.Id,
                Created = false,
            });
        }

        var now = DateTime.UtcNow;
        var conv = new DirectConversation
        {
            UserLowerId = lower,
            UserHigherId = higher,
            LastMessageAtUtc = now,
        };

        dbContext.DirectConversations.Add(conv);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new StartConversationResponse
        {
            ConversationId = conv.Id,
            Created = true,
        });
    }

    [HttpPost("{conversationId:guid}/messages")]
    [EnableRateLimiting("directmessages")]
    public async Task<ActionResult<DirectMessageItemResponse>> SendMessage(
        Guid conversationId,
        [FromBody] SendDirectMessageRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var content = request.Content.Trim();
        if (content.Length == 0)
        {
            return BadRequest(new { message = "Mesaj bos olamaz." });
        }

        var conv = await dbContext.DirectConversations
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && (c.UserLowerId == userId || c.UserHigherId == userId),
                cancellationToken);

        if (conv is null)
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        var msg = new DirectMessage
        {
            ConversationId = conv.Id,
            SenderUserId = userId.Value,
            Content = content,
            SentAtUtc = now,
        };

        dbContext.DirectMessages.Add(msg);
        conv.LastMessageAtUtc = now;

        var otherUserId = conv.UserLowerId == userId.Value ? conv.UserHigherId : conv.UserLowerId;
        var actorName = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId.Value)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Bir kullanici";

        var createdNotification = new Notification
        {
            UserId = otherUserId,
            Title = $"{actorName} sana mesaj gonderdi",
            Message = content.Length > 120 ? $"{content[..117]}..." : content,
            Type = Domain.Enums.NotificationType.Social,
            TargetPath = $"/messages?conversation={conv.Id}"
        };
        dbContext.Notifications.Add(createdNotification);

        await dbContext.SaveChangesAsync(cancellationToken);
        await notificationPublisher.PublishAsync(createdNotification, cancellationToken);

        var response = new DirectMessageItemResponse
        {
            Id = msg.Id,
            SenderUserId = msg.SenderUserId,
            Content = msg.Content,
            SentAtUtc = msg.SentAtUtc,
        };

        await directMessageHub.Clients.User(otherUserId.ToString()).SendAsync(
            "ReceiveMessage",
            new
            {
                conversationId = conv.Id,
                message = response,
            },
            cancellationToken);

        return Ok(response);
    }

    private static void OrderPair(Guid a, Guid b, out Guid lower, out Guid higher)
    {
        if (a.CompareTo(b) <= 0)
        {
            lower = a;
            higher = b;
        }
        else
        {
            lower = b;
            higher = a;
        }
    }
}
