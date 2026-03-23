using EduConnect.Application.Contracts.Chat;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Domain.Enums;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("gemini")]
[Route("api/[controller]")]
public sealed class ChatController(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IChatbotService chatbotService) : ControllerBase
{
    [HttpPost("sessions")]
    public async Task<ActionResult<ChatSessionStartedResponse>> StartSession(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        var session = new ChatSession { UserId = userId.Value };
        dbContext.ChatSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ChatSessionStartedResponse
        {
            SessionId = session.Id,
            StartedAtUtc = session.StartedAtUtc
        });
    }

    [HttpPost("sessions/{sessionId:guid}/messages")]
    public async Task<ActionResult<ChatbotReply>> SendMessage(
        Guid sessionId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Mesaj boş olamaz.");

        if (request.Message.Length > 2000)
            return BadRequest("Mesaj çok uzun. Maksimum 2000 karakter.");

        var session = await dbContext.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId.Value, cancellationToken);

        if (session is null)
            return NotFound("Chat oturumu bulunamadı.");

        if (session.EndedAtUtc is not null)
            return BadRequest("Bu chat oturumu kapatılmış.");

        var previousMessages = await dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.TimestampUtc)
            .Select(m => new { m.SenderType, m.Content })
            .ToListAsync(cancellationToken);

        var history = previousMessages
            .Select(m => (
                Role: m.SenderType == SenderType.User ? "user" : "assistant",
                m.Content))
            .ToArray();

        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            SenderType = SenderType.User,
            Content = request.Message.Trim(),
            TimestampUtc = DateTime.UtcNow
        };

        var reply = await chatbotService.GetReplyAsync(request.Message, history, cancellationToken);

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

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(reply);
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyCollection<ChatSessionResponse>>> GetSessions(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        var sessions = await dbContext.ChatSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(20)
            .Select(x => new ChatSessionResponse
            {
                SessionId = x.Id,
                StartedAtUtc = x.StartedAtUtc,
                EndedAtUtc = x.EndedAtUtc,
                TotalMessages = x.TotalMessages,
                IsActive = x.EndedAtUtc == null
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    [HttpGet("sessions/{sessionId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyCollection<ChatMessageResponse>>> GetMessages(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        var sessionExists = await dbContext.ChatSessions
            .AnyAsync(x => x.Id == sessionId && x.UserId == userId.Value, cancellationToken);

        if (!sessionExists)
            return NotFound("Chat oturumu bulunamadı.");

        var messages = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.TimestampUtc)
            .Select(m => new ChatMessageResponse
            {
                Id = m.Id,
                SenderType = m.SenderType,
                Content = m.Content,
                IntentDetected = m.IntentDetected,
                Confidence = m.Confidence,
                TimestampUtc = m.TimestampUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(messages);
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<ActionResult> EndSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        var session = await dbContext.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId.Value, cancellationToken);

        if (session is null)
            return NotFound("Chat oturumu bulunamadı.");

        session.EndedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
