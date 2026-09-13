using AgriCheck.Application.OpsPortal.Dtos;

namespace AgriCheck.Application.OpsPortal;

public interface IAgriTrackPushService
{
    Task RegisterDeviceAsync(long userId, string expoPushToken, string platform, CancellationToken cancellationToken = default);
    Task RemoveDeviceAsync(long userId, string expoPushToken, CancellationToken cancellationToken = default);
    Task RegisterCurrentUserDeviceAsync(RegisterMobilePushDeviceRequest request, CancellationToken cancellationToken = default);
    Task RemoveCurrentUserDeviceAsync(string expoPushToken, CancellationToken cancellationToken = default);
    Task NotifyDriverAssignmentAsync(long driverUserId, string containerNumber, Guid containerUuid, CancellationToken cancellationToken = default);
    Task NotifyWarehouseArrivalAsync(string containerNumber, Guid containerUuid, string warehouseName, CancellationToken cancellationToken = default);
}
