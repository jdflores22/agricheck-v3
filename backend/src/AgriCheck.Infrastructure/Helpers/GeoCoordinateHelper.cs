namespace AgriCheck.Infrastructure.Helpers;

internal static class GeoCoordinateHelper
{
    private const decimal MinLat = 4.2m;
    private const decimal MaxLat = 21.3m;
    private const decimal MinLng = 116.0m;
    private const decimal MaxLng = 127.5m;

    public static bool IsPlausiblePhilippinesCoordinate(decimal latitude, decimal longitude)
    {
        if (latitude == 0 && longitude == 0)
        {
            return false;
        }

        return latitude >= MinLat && latitude <= MaxLat && longitude >= MinLng && longitude <= MaxLng;
    }
}
