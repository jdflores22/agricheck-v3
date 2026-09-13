using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AgriCheck.Api.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
    public static string UserGroup(Guid userUuid) => $"user:{userUuid:D}";

    public override async Task OnConnectedAsync()
    {
        var sub = Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(sub, out var userUuid))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userUuid));
        }

        await base.OnConnectedAsync();
    }
}
