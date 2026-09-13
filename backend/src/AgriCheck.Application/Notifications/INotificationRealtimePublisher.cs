namespace AgriCheck.Application.Notifications;

public interface INotificationRealtimePublisher
{
    Task PublishAsync(long userId, NotificationDto notification, int unreadCount, CancellationToken cancellationToken = default);
}
