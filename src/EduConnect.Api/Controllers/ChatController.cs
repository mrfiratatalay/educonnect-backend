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

        var openSessions = await dbContext.ChatSessions
            .Where(x => x.UserId == userId.Value && x.EndedAtUtc == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var s in openSessions)
            s.EndedAtUtc = now;

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
    [EnableRateLimiting("gemini")]
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
            TimestampUtc = DateTime.UtcNow,
            // --- Faz 5: Analitik alanları ---
            ModelUsed = reply.ModelUsed,
            KbScore = reply.KbScore,
            KbHit = reply.KbHit,
            IsFallback = reply.IsFallback,
            LatencyMs = reply.LatencyMs,
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
                ConfidenceBand = GetConfidenceBand(m.Confidence),
                NeedsReview = (m.Confidence ?? 0) < 0.6 || m.IsFallback == true,
                ModelUsed = m.ModelUsed,
                KbScore = m.KbScore,
                KbHit = m.KbHit,
                IsFallback = m.IsFallback,
                LatencyMs = m.LatencyMs,
                TimestampUtc = m.TimestampUtc,
                // --- Faz 5: Geri bildirim durumu ---
                HasFeedback = m.Feedback != null,
                FeedbackIsHelpful = m.Feedback != null ? m.Feedback.IsHelpful : null,
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

    [HttpDelete("sessions/{sessionId:guid}/permanent")]
    public async Task<ActionResult> DeleteSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        var session = await dbContext.ChatSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId.Value, cancellationToken);

        if (session is null)
            return NotFound("Chat oturumu bulunamadı.");

        dbContext.ChatSessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ===== Faz 5: Geri Bildirim Endpoint'i =====

    [HttpPost("messages/{messageId:guid}/feedback")]
    public async Task<ActionResult> SubmitFeedback(
        Guid messageId,
        [FromBody] SubmitChatFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        // Mesajın var olduğunu ve bu kullanıcının oturumuna ait olduğunu doğrula
        var message = await dbContext.ChatMessages
            .Include(m => m.Session)
            .Include(m => m.Feedback)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderType == SenderType.Bot, cancellationToken);

        if (message is null)
            return NotFound("Bot mesajı bulunamadı.");

        if (message.Session.UserId != userId.Value)
            return Forbid();

        // Zaten geri bildirim varsa güncelle
        if (message.Feedback is not null)
        {
            message.Feedback.IsHelpful = request.IsHelpful;
            message.Feedback.Comment = request.Comment?.Trim();
        }
        else
        {
            var feedback = new ChatMessageFeedback
            {
                ChatMessageId = messageId,
                UserId = userId.Value,
                IsHelpful = request.IsHelpful,
                Comment = request.Comment?.Trim(),
            };
            dbContext.ChatMessageFeedbacks.Add(feedback);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { messageId, request.IsHelpful });
    }

    // ===== Faz 5: Analitik Özet Endpoint'i =====

    [HttpGet("analytics/summary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EduAiAnalyticsSummaryResponse>> GetAnalyticsSummary(CancellationToken cancellationToken)
    {
        var totalSessions = await dbContext.ChatSessions.CountAsync(cancellationToken);

        var botMessages = dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.SenderType == SenderType.Bot);

        var totalBotMessages = await botMessages.CountAsync(cancellationToken);

        if (totalBotMessages == 0)
        {
            return Ok(new EduAiAnalyticsSummaryResponse
            {
                TotalSessions = totalSessions,
                TotalBotMessages = 0,
            });
        }

        // Geri bildirim metrikleri
        var feedbackStats = await dbContext.ChatMessageFeedbacks
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Helpful = g.Count(f => f.IsHelpful),
                NotHelpful = g.Count(f => !f.IsHelpful),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var feedbackCount = feedbackStats?.Count ?? 0;
        var helpfulCount = feedbackStats?.Helpful ?? 0;
        var notHelpfulCount = feedbackStats?.NotHelpful ?? 0;

        // Performans metrikleri
        var avgConfidence = await botMessages
            .Where(m => m.Confidence != null)
            .AverageAsync(m => m.Confidence!.Value, cancellationToken);

        var avgLatency = await botMessages
            .Where(m => m.LatencyMs != null)
            .AverageAsync(m => (double)m.LatencyMs!.Value, cancellationToken);

        // KB hit / fallback oranları
        var kbHitCount = await botMessages.CountAsync(m => m.KbHit == true, cancellationToken);
        var fallbackCount = await botMessages.CountAsync(m => m.IsFallback == true, cancellationToken);

        // Intent dağılımı
        var intentDistribution = await botMessages
            .Where(m => m.IntentDetected != null)
            .GroupBy(m => m.IntentDetected!)
            .Select(g => new IntentBreakdown
            {
                Intent = g.Key,
                Count = g.Count(),
                AvgConfidence = g.Average(m => m.Confidence ?? 0),
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        // Çözülmeyen sorgular: fallback + no KB hit VEYA düşük confidence VEYA olumsuz geri bildirim
        var unresolvedQueries = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.SenderType == SenderType.Bot &&
                        ((m.IsFallback == true && m.KbHit != true) ||
                         m.Confidence < 0.5 ||
                         (m.Feedback != null && !m.Feedback.IsHelpful)))
            .OrderByDescending(m => m.TimestampUtc)
            .Take(20)
            .Select(m => new
            {
                m.SessionId,
                m.IntentDetected,
                m.Confidence,
                m.TimestampUtc,
            })
            .ToListAsync(cancellationToken);

        // Çözülmemiş bot mesajlarının user mesajlarını bul
        var unresolvedSessionIds = unresolvedQueries.Select(u => u.SessionId).Distinct().ToList();
        var userMessages = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.SenderType == SenderType.User && unresolvedSessionIds.Contains(m.SessionId))
            .OrderByDescending(m => m.TimestampUtc)
            .ToListAsync(cancellationToken);

        var topUnresolved = unresolvedQueries
            .Select(u =>
            {
                // En yakın user mesajını bul (bu bot mesajından önce gönderilen)
                var userMsg = userMessages
                    .Where(um => um.SessionId == u.SessionId && um.TimestampUtc <= u.TimestampUtc)
                    .OrderByDescending(um => um.TimestampUtc)
                    .FirstOrDefault();

                return new UnresolvedQueryInfo
                {
                    UserMessage = userMsg?.Content ?? "(bilinmiyor)",
                    Intent = u.IntentDetected ?? "unknown",
                    Confidence = u.Confidence ?? 0,
                    TimestampUtc = u.TimestampUtc,
                };
            })
            .ToList();

        return Ok(new EduAiAnalyticsSummaryResponse
        {
            TotalSessions = totalSessions,
            TotalBotMessages = totalBotMessages,
            FeedbackCount = feedbackCount,
            HelpfulCount = helpfulCount,
            NotHelpfulCount = notHelpfulCount,
            HelpfulRate = feedbackCount > 0 ? Math.Round((double)helpfulCount / feedbackCount * 100, 1) : 0,
            AvgConfidence = Math.Round(avgConfidence, 4),
            AvgLatencyMs = Math.Round(avgLatency, 1),
            KbHitCount = kbHitCount,
            KbHitRate = totalBotMessages > 0 ? Math.Round((double)kbHitCount / totalBotMessages * 100, 1) : 0,
            FallbackCount = fallbackCount,
            FallbackRate = totalBotMessages > 0 ? Math.Round((double)fallbackCount / totalBotMessages * 100, 1) : 0,
            IntentDistribution = intentDistribution,
            TopUnresolvedQueries = topUnresolved,
        });
    }

    private static string? GetConfidenceBand(double? confidence)
    {
        if (confidence is null)
            return null;

        if (confidence >= 0.85)
            return "high";
        if (confidence >= 0.60)
            return "medium";
        return "low";
    }
}
