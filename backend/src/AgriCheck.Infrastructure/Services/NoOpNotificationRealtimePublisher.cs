using AgriCheck.Application.Notifications;

namespace AgriCheck.Infrastructure.Services;

public sealed class NoOpNotificationRealtimePublisher : INotificationRealtimePublisher
{
    public Task PublishAsync(long userId, NotificationDto notification, int unreadCount, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
