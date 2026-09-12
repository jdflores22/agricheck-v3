namespace AgriCheck.Application.MavPortal;

public interface IMavNotificationService
{
    Task NotifyApplicationSubmittedAsync(long importerId, Guid applicationUuid, string referenceNumber, string commodityName, CancellationToken cancellationToken = default);
    Task NotifyApplicationApprovedAsync(long importerId, Guid applicationUuid, string referenceNumber, string licenseNumber, decimal allocatedVolume, CancellationToken cancellationToken = default);
    Task NotifyApplicationRejectedAsync(long importerId, Guid applicationUuid, string referenceNumber, string reason, CancellationToken cancellationToken = default);
    Task NotifyMicIssuedAsync(long importerId, Guid micUuid, string certificateNumber, decimal volume, CancellationToken cancellationToken = default);
    Task SendComplianceRemindersAsync(CancellationToken cancellationToken = default);
}
