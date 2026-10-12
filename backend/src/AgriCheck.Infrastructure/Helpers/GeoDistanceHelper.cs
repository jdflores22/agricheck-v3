namespace AgriCheck.Infrastructure.Helpers;

public static class GeoDistanceHelper
{
    /// <summary>Pickup confirm and warehouse check-in radius (AgriTrack <c>DELIVERY_STOP_GEOFENCE_METERS</c>).</summary>
    public const double DeliveryStopGeofenceMeters = 100;

    public const double DefaultWarehouseGeofenceMeters = DeliveryStopGeofenceMeters;

    public static double DistanceMeters(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double earthRadius = 6371000;
        var dLat = DegreesToRadians((double)(lat2 - lat1));
        var dLon = DegreesToRadians((double)(lon2 - lon1));
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(DegreesToRadians((double)lat1)) * Math.Cos(DegreesToRadians((double)lat2))
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
