namespace AgriCheck.Application.Notifications;

public record NotificationDto(
    Guid Uuid,
    string Type,
    string Title,
    string Message,
    string? RelatedEntityType,
    string? RelatedEntityUuid,
    bool IsRead,
    DateTime CreatedAt);

public record NotificationPreferenceDto(bool EmailEnabled, bool InAppEnabled);

public record UpdateNotificationPreferenceRequest(bool EmailEnabled, bool InAppEnabled);

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(int limit, bool unreadOnly, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(CancellationToken cancellationToken = default);
    Task<NotificationPreferenceDto> GetPreferencesAsync(CancellationToken cancellationToken = default);
    Task<NotificationPreferenceDto> UpdatePreferencesAsync(UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken = default);
    Task NotifyAsync(long userId, string type, string title, string message, string? relatedEntityType = null, string? relatedEntityUuid = null, CancellationToken cancellationToken = default);
}
