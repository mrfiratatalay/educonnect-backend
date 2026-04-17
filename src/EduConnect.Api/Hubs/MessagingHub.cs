using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EduConnect.Api.Hubs;

[Authorize]
public sealed class MessagingHub : Hub
{
    // Connections automatically mapped to UserIdentifier via JWT ClaimTypes.NameIdentifier
}
