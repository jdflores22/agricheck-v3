namespace AgriCheck.Infrastructure.Helpers;

/// <summary>
/// Reference stops for AgriTrack navigation / delivery test reset (see <c>reset-delivery-test</c> API).
/// </summary>
public static class DriverDeliveryTestReset
{
    public const string PickupLabel = "Reference pickup (test)";
    public const decimal PickupLatitude = 14.454427m;
    public const decimal PickupLongitude = 120.990610m;

    public const string WarehouseLabel = "Reference warehouse (test)";
    public const decimal WarehouseLatitude = 14.459128m;
    public const decimal WarehouseLongitude = 120.987523m;

    /// <summary>Seed trail GPS on reset (~300 m from reference pickup for leg-1 drive tests).</summary>
    public const decimal SeedLatitude = 14.452700m;
    public const decimal SeedLongitude = 120.992400m;
}
