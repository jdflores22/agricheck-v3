using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.OpsPortal.Dtos;

namespace AgriCheck.IntegrationTests.Support;

internal sealed class NoOpAgriTrackPushService : IAgriTrackPushService
{
    public Task RegisterDeviceAsync(long userId, string expoPushToken, string platform, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveDeviceAsync(long userId, string expoPushToken, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RegisterCurrentUserDeviceAsync(RegisterMobilePushDeviceRequest request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveCurrentUserDeviceAsync(string expoPushToken, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyDriverAssignmentAsync(long driverUserId, string containerNumber, Guid containerUuid, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyWarehouseArrivalAsync(string containerNumber, Guid containerUuid, string warehouseName, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
