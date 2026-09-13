using AgriCheck.Application.Notifications;
using AgriCheck.Api.Hubs;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Api.Services;

public sealed class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly IServiceScopeFactory _scopeFactory;

    public SignalRNotificationRealtimePublisher(IHubContext<NotificationsHub> hub, IServiceScopeFactory scopeFactory)
    {
        _hub = hub;
        _scopeFactory = scopeFactory;
    }

    public async Task PublishAsync(long userId, NotificationDto notification, int unreadCount, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgriCheckDbContext>();

        var userUuid = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Uuid)
            .FirstOrDefaultAsync(cancellationToken);

        if (userUuid == Guid.Empty) return;

        await _hub.Clients
            .Group(NotificationsHub.UserGroup(userUuid))
            .SendAsync("notificationReceived", new { notification, unreadCount }, cancellationToken);
    }
}
