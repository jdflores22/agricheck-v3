namespace AgriCheck.Infrastructure.Helpers;

public static class DeliveryComplianceEventTypes
{
    public const string Misroute = "misroute";
    public const string WrongWarehouse = "wrong_warehouse";
    public const string HandoffQrReplay = "handoff_qr_replay";
    public const string OffRouteBlocked = "off_route_blocked";
    public const string NavigationNotStarted = "navigation_not_started";
    public const string RouteDistanceRegression = "route_distance_regression";
    public const string DriverStationary15 = "driver_stationary_15";
    public const string DriverStationary30 = "driver_stationary_30";
    public const string DriverIncident = "driver_incident";
}
