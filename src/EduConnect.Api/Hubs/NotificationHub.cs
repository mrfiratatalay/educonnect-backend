using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EduConnect.Api.Hubs;

[Authorize]
public sealed class NotificationHub : Hub;
