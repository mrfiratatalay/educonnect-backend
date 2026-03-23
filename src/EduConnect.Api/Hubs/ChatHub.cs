using System.Security.Claims;
using EduConnect.Application.Contracts.Chat;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Domain.Enums;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Hubs;

[Authorize]
public sealed class ChatHub(AppDbContext dbContext, IChatbotService chatbotService) : Hub
{
    public async Task<ChatSessionStartedResponse> StartSession()
    {
        var userId = GetCurrentUserId();

        var session = new ChatSession
        {
            UserId = userId
        };

        dbContext.ChatSessions.Add(session);
        await dbContext.SaveChangesAsync();

        return new ChatSessionStartedResponse
        {
            SessionId = session.Id,
            StartedAtUtc = session.StartedAtUtc
        };
    }

    public async Task EndSession(Guid sessionId)
    {
        var userId = GetCurrentUserId();

        var session = await dbContext.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId);

        if (session is null)
        {
            throw new HubException("Chat oturumu bulunamadı.");
        }

        session.EndedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public async Task SendMessage(Guid sessionId, string message)
    {
        var userId = GetCurrentUserId();

        var session = await dbContext.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId);

        if (session is null)
        {
            throw new HubException("Chat oturumu bulunamadı.");
        }

        if (session.EndedAtUtc is not null)
        {
            throw new HubException("Bu chat oturumu kapatılmış.");
        }

        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            SenderType = SenderType.User,
            Content = message.Trim(),
            TimestampUtc = DateTime.UtcNow
        };

        var reply = await chatbotService.GetReplyAsync(message);

        var botMessage = new ChatMessage
        {
            SessionId = session.Id,
            SenderType = SenderType.Bot,
            Content = reply.Content,
            IntentDetected = reply.IntentDetected,
            Confidence = reply.Confidence,
            TimestampUtc = DateTime.UtcNow
        };

        dbContext.ChatMessages.Add(userMessage);
        dbContext.ChatMessages.Add(botMessage);
        session.TotalMessages += 2;

        await dbContext.SaveChangesAsync();

        await Clients.Caller.SendAsync("ReceiveMessage", new
        {
            sessionId = session.Id,
            content = botMessage.Content,
            intentDetected = botMessage.IntentDetected,
            confidence = botMessage.Confidence,
            timestampUtc = botMessage.TimestampUtc
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("Kullanıcı bilgisi okunamadı.");
        }

        return userId;
    }
}
