namespace AgriCheck.Infrastructure.Helpers;

public static class DeliveryComplianceEventTypes
{
    public const string Misroute = "misroute";
    public const string WrongWarehouse = "wrong_warehouse";
    public const string HandoffQrReplay = "handoff_qr_replay";
    public const string OffRouteBlocked = "off_route_blocked";
    public const string DriverIncident = "driver_incident";
}
