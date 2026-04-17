using EduConnect.Application.Contracts.Common;
using EduConnect.Application.Contracts.Messaging;
using EduConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class MessagesController(
    IMessagingService messagingService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConversationResponse>> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "Mesaj içeriği boş olamaz." });

        if (request.Content.Length > 2000)
            return BadRequest(new { message = "Mesaj içeriği çok uzun." });

        var response = await messagingService.SendMessageAsync(userId.Value, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<CursorPagedResponse<ConversationResponse>>> GetConversations(
        [FromQuery] DateTime? cursor = null,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        var response = await messagingService.GetConversationsAsync(userId.Value, cursor, limit, cancellationToken);
        return Ok(response);
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<ActionResult<CursorPagedResponse<DirectMessageResponse>>> GetMessages(
        Guid conversationId,
        [FromQuery] DateTime? cursor = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        var response = await messagingService.GetMessagesAsync(userId.Value, conversationId, cursor, limit, cancellationToken);
        return Ok(response);
    }

    [HttpPost("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();

        await messagingService.MarkAsReadAsync(userId.Value, conversationId, cancellationToken);
        return NoContent();
    }
}
