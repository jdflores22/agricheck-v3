using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Infrastructure.Helpers;

internal static class WarehouseHandoffHelper
{
    public static bool IsHandoffQrAvailable(Container container) =>
        container.WarehouseHandoffEligibleAt is not null &&
        (container.Status is ContainerStatus.InTransit
            or ContainerStatus.PickedUp
            or ContainerStatus.AtWarehouse) &&
        !DeliveryComplianceHoldHelper.HasActiveHold(container);

    public static bool CanIssueDeliveryHandoffQr(Container container) =>
        container.Status is ContainerStatus.InTransit
            or ContainerStatus.PickedUp
            or ContainerStatus.AtWarehouse;

    public static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    public static bool IsWithinHandoffQrGracePeriod(DateTime? warehouseHandoffEligibleAtUtc, DateTime utcNow, TimeSpan grace) =>
        warehouseHandoffEligibleAtUtc is not null &&
        (utcNow - NormalizeUtc(warehouseHandoffEligibleAtUtc.Value)) <= grace;
}
