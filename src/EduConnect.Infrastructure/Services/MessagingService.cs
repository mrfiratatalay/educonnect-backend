using EduConnect.Application.Contracts.Common;
using EduConnect.Application.Contracts.Messaging;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Infrastructure.Services;

public sealed class MessagingService(
    AppDbContext dbContext,
    IMessagingNotifier notifier) : IMessagingService
{
    public async Task<ConversationResponse> SendMessageAsync(Guid senderId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        Conversation? conversation = null;

        if (request.ConversationId.HasValue)
        {
            conversation = await dbContext.Conversations
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.Id == request.ConversationId.Value, cancellationToken);
                
            if (conversation is null) throw new KeyNotFoundException("Konuşma bulunamadı.");
            
            if (!conversation.Participants.Any(x => x.UserId == senderId))
                throw new UnauthorizedAccessException("Bu konuşmaya katılmadığınız için mesaj gönderemezsiniz.");
        }
        else if (request.ReceiverId.HasValue)
        {
            if (request.ReceiverId.Value == senderId) 
                throw new InvalidOperationException("Kendinize mesaj gönderemezsiniz.");

            // Removed Block check as per previous instruction to ignore if follow relation isn't mature, but let's just make sure receiver exists.
            var receiverExists = await dbContext.Users.AnyAsync(x => x.Id == request.ReceiverId.Value, cancellationToken);
            if (!receiverExists) throw new KeyNotFoundException("Alıcı bulunamadı.");
            
            var existingConvoId = await dbContext.ConversationParticipants
                .Where(x => x.UserId == senderId || x.UserId == request.ReceiverId.Value)
                .GroupBy(x => x.ConversationId)
                .Where(g => g.Count() == 2 && g.Any(p => p.UserId == senderId) && g.Any(p => p.UserId == request.ReceiverId.Value))
                .Select(g => g.Key)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (existingConvoId != Guid.Empty)
            {
                conversation = await dbContext.Conversations
                    .Include(x => x.Participants)
                    .FirstOrDefaultAsync(x => x.Id == existingConvoId, cancellationToken);
            }
            
            if (conversation is null)
            {
                conversation = new Conversation();
                conversation.Participants.Add(new ConversationParticipant { UserId = senderId });
                conversation.Participants.Add(new ConversationParticipant { UserId = request.ReceiverId.Value });
                dbContext.Conversations.Add(conversation);
            }
        }
        else
        {
            throw new ArgumentException("ConversationId veya ReceiverId zorunludur.");
        }
        
        var content = request.Content.Trim();
        var message = new DirectMessage
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = content
        };
        
        conversation.LastMessageAtUtc = DateTime.UtcNow;
        conversation.LastMessagePreview = content.Length > 100 ? content[..97] + "..." : content;
        
        Guid receiverId = Guid.Empty;
        foreach (var p in conversation.Participants.Where(x => x.UserId != senderId))
        {
            p.UnreadCount++;
            p.HasUnreadMessages = true;
            receiverId = p.UserId;
        }
        
        dbContext.DirectMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        // Lightweight fetch for the other participant
        var receiverData = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == receiverId)
            .Select(x => new { x.Id, x.FullName, x.StudentProfile!.AvatarUrl })
            .FirstOrDefaultAsync(cancellationToken);

        var senderParticipant = conversation.Participants.First(x => x.UserId == senderId);
        
        var response = new ConversationResponse(
            conversation.Id,
            conversation.LastMessageAtUtc,
            conversation.LastMessagePreview,
            senderParticipant.UnreadCount,
            receiverData != null ? new ParticipantResponse(receiverData.Id, receiverData.FullName, receiverData.AvatarUrl) : new ParticipantResponse(Guid.Empty, "Bilinmeyen", null)
        );

        // Notify via SignalR!
        if (receiverId != Guid.Empty)
        {
            await notifier.NotifyNewMessageAsync(receiverId, response, cancellationToken);
        }

        return response;
    }

    public async Task<CursorPagedResponse<ConversationResponse>> GetConversationsAsync(Guid userId, DateTime? cursor = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var query = dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(x => x.UserId == userId && (cursor == null || x.Conversation.LastMessageAtUtc < cursor))
            .Include(x => x.Conversation)
            .OrderByDescending(x => x.Conversation.LastMessageAtUtc);

        var totalCount = await dbContext.ConversationParticipants.CountAsync(x => x.UserId == userId, cancellationToken);

        var participants = await query
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = participants.Count > limit;
        if (hasMore) participants.RemoveAt(limit);

        var conversationIds = participants.Select(x => x.ConversationId).ToArray();

        var otherParticipants = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(x => conversationIds.Contains(x.ConversationId) && x.UserId != userId)
            .Select(x => new { x.ConversationId, x.UserId, x.User.FullName, x.User.StudentProfile!.AvatarUrl })
            .ToListAsync(cancellationToken);

        var responseItems = participants.Select(p => 
        {
            var otherP = otherParticipants.FirstOrDefault(x => x.ConversationId == p.ConversationId);
            var otherParticipantResponse = otherP != null 
                ? new ParticipantResponse(otherP.UserId, otherP.FullName, otherP.AvatarUrl)
                : new ParticipantResponse(Guid.Empty, "Bilinmeyen", null);

            return new ConversationResponse(
                p.ConversationId,
                p.Conversation.LastMessageAtUtc,
                p.Conversation.LastMessagePreview,
                p.UnreadCount,
                otherParticipantResponse
            );
        }).ToArray();

        return new CursorPagedResponse<ConversationResponse>
        {
            Items = responseItems,
            NextCursor = responseItems.LastOrDefault()?.LastMessageAtUtc,
            HasMore = hasMore,
            TotalCount = totalCount
        };
    }

    public async Task<CursorPagedResponse<DirectMessageResponse>> GetMessagesAsync(Guid userId, Guid conversationId, DateTime? cursor = null, int limit = 50, CancellationToken cancellationToken = default)
    {
        var participantExists = await dbContext.ConversationParticipants
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId, cancellationToken);

        if (!participantExists)
            throw new UnauthorizedAccessException("Erişim reddedildi.");

        limit = Math.Clamp(limit, 1, 100);

        var query = dbContext.DirectMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId && (cursor == null || x.CreatedAtUtc < cursor))
            .OrderByDescending(x => x.CreatedAtUtc);

        var totalCount = await dbContext.DirectMessages.CountAsync(x => x.ConversationId == conversationId, cancellationToken);

        var messages = await query
            .Take(limit + 1)
            .Select(m => new DirectMessageResponse(
                m.Id,
                m.SenderId,
                m.Content,
                m.CreatedAtUtc,
                m.IsRead
            ))
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > limit;
        if (hasMore) messages.RemoveAt(limit);

        messages.Reverse(); // Return in chronological order for UI display

        return new CursorPagedResponse<DirectMessageResponse>
        {
            Items = messages,
            NextCursor = messages.FirstOrDefault()?.SentAtUtc, // The oldest message in the retrieved batch
            HasMore = hasMore,
            TotalCount = totalCount
        };
    }

    public async Task MarkAsReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        var participant = await dbContext.ConversationParticipants
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == userId, cancellationToken);

        if (participant is null) return;

        if (participant.HasUnreadMessages || participant.UnreadCount > 0)
        {
            participant.HasUnreadMessages = false;
            participant.UnreadCount = 0;

            var unreadMessages = await dbContext.DirectMessages
                .Where(x => x.ConversationId == conversationId && x.SenderId != userId && !x.IsRead)
                .ToListAsync(cancellationToken);

            var senderIdToNotify = unreadMessages.FirstOrDefault()?.SenderId;

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAtUtc = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            if (senderIdToNotify.HasValue)
            {
                await notifier.NotifyMessageReadAsync(senderIdToNotify.Value, conversationId, cancellationToken);
            }
        }
    }
}
