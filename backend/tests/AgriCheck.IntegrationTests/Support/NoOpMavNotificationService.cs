using AgriCheck.Application.MavPortal;

namespace AgriCheck.IntegrationTests.Support;

internal sealed class NoOpMavNotificationService : IMavNotificationService
{
    public Task NotifyApplicationSubmittedAsync(long importerId, Guid applicationUuid, string referenceNumber, string commodityName, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyApplicationApprovedAsync(long importerId, Guid applicationUuid, string referenceNumber, string licenseNumber, decimal allocatedVolume, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyApplicationRejectedAsync(long importerId, Guid applicationUuid, string referenceNumber, string reason, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyMicIssuedAsync(long importerId, Guid micUuid, string certificateNumber, decimal volume, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendComplianceRemindersAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
