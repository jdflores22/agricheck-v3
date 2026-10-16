using AgriCheck.Domain.Entities;

namespace AgriCheck.Infrastructure.Helpers;

internal static class DeliveryComplianceHoldHelper
{
    public static bool HasActiveHold(Container container) =>
        !string.IsNullOrWhiteSpace(container.DeliveryComplianceHoldReason) &&
        container.DeliveryComplianceClearedAt is null;

    public static bool ShouldApplyHoldForEvent(string eventType) =>
        eventType is DeliveryComplianceEventTypes.WrongWarehouse
            or DeliveryComplianceEventTypes.Misroute
            or DeliveryComplianceEventTypes.OffRouteBlocked;

    public static void ApplyHold(Container container, string eventType)
    {
        if (!ShouldApplyHoldForEvent(eventType))
        {
            return;
        }

        container.DeliveryComplianceHoldReason = eventType;
        container.DeliveryComplianceHoldAt = DateTime.UtcNow;
        container.DeliveryComplianceClearedAt = null;
        container.DeliveryComplianceClearedByUserId = null;
        container.DeliveryComplianceClearanceNotes = null;
        container.WarehouseHandoffEligibleAt = null;
    }

    public static void ClearHold(Container container, long clearedByUserId, string? notes)
    {
        container.DeliveryComplianceClearedAt = DateTime.UtcNow;
        container.DeliveryComplianceClearedByUserId = clearedByUserId;
        container.DeliveryComplianceClearanceNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public static string HoldUserMessage(string? reason) => reason switch
    {
        DeliveryComplianceEventTypes.WrongWarehouse =>
            "Wrong warehouse detected. Trip cannot be ended until DA authority clears this delivery.",
        DeliveryComplianceEventTypes.Misroute =>
            "Route violation (misroute). Trip cannot be ended until DA authority reviews and clears.",
        DeliveryComplianceEventTypes.OffRouteBlocked =>
            "Sustained off-route movement. Trip cannot be ended until DA authority clears this delivery.",
        _ => "Delivery is on compliance hold. Trip cannot be ended until DA authority clears.",
    };
}
