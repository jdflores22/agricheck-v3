using System.Globalization;
using System.Text.Json;
using AgriCheck.Domain.Entities;

namespace AgriCheck.Infrastructure.Helpers;

internal static class ContainerDeliveryPointHelper
{
    public const string PickupLabelKey = "delivery_pickup_label";
    public const string PickupLatKey = "delivery_pickup_latitude";
    public const string PickupLngKey = "delivery_pickup_longitude";

    public const string DestinationLabelKey = "delivery_destination_label";
    public const string DestinationLatKey = "delivery_destination_latitude";
    public const string DestinationLngKey = "delivery_destination_longitude";

    public static void SetPickup(Container container, string label, decimal latitude, decimal longitude)
    {
        var dict = ParseForm(container.FormDataJson);
        dict[PickupLabelKey] = label.Trim();
        dict[PickupLatKey] = latitude.ToString(CultureInfo.InvariantCulture);
        dict[PickupLngKey] = longitude.ToString(CultureInfo.InvariantCulture);
        container.FormDataJson = JsonSerializer.Serialize(dict);
    }

    public static void ClearStoredPickup(Container container)
    {
        var dict = ParseForm(container.FormDataJson);
        dict.Remove(PickupLabelKey);
        dict.Remove(PickupLatKey);
        dict.Remove(PickupLngKey);
        container.FormDataJson = dict.Count == 0 ? null : JsonSerializer.Serialize(dict);
    }

    public static void SetDestination(Container container, string label, decimal latitude, decimal longitude)
    {
        var dict = ParseForm(container.FormDataJson);
        dict[DestinationLabelKey] = label.Trim();
        dict[DestinationLatKey] = latitude.ToString(CultureInfo.InvariantCulture);
        dict[DestinationLngKey] = longitude.ToString(CultureInfo.InvariantCulture);
        container.FormDataJson = JsonSerializer.Serialize(dict);
    }

    public static void ClearStoredDestination(Container container)
    {
        var dict = ParseForm(container.FormDataJson);
        dict.Remove(DestinationLabelKey);
        dict.Remove(DestinationLatKey);
        dict.Remove(DestinationLngKey);
        container.FormDataJson = dict.Count == 0 ? null : JsonSerializer.Serialize(dict);
    }

    public static (string Label, decimal Latitude, decimal Longitude)? TryGetStoredDestination(Container container)
    {
        var dict = ParseForm(container.FormDataJson);
        if (!dict.TryGetValue(DestinationLatKey, out var latRaw) || !dict.TryGetValue(DestinationLngKey, out var lngRaw))
        {
            return null;
        }

        if (!decimal.TryParse(latRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !decimal.TryParse(lngRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
        {
            return null;
        }

        if (!GeoCoordinateHelper.IsPlausiblePhilippinesCoordinate(lat, lng))
        {
            return null;
        }

        var label = dict.GetValueOrDefault(DestinationLabelKey) ?? "Warehouse";
        return (label, lat, lng);
    }

    public static (string Label, decimal Latitude, decimal Longitude)? TryGetStoredPickup(Container container)
    {
        var dict = ParseForm(container.FormDataJson);
        if (!dict.TryGetValue(PickupLatKey, out var latRaw) || !dict.TryGetValue(PickupLngKey, out var lngRaw))
        {
            return null;
        }

        if (!decimal.TryParse(latRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !decimal.TryParse(lngRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
        {
            return null;
        }

        if (!GeoCoordinateHelper.IsPlausiblePhilippinesCoordinate(lat, lng))
        {
            return null;
        }

        var label = dict.GetValueOrDefault(PickupLabelKey) ?? "Pickup point";
        return (label, lat, lng);
    }

    public static (string Label, decimal Latitude, decimal Longitude) ResolvePlannedPickup(Entry entry)
    {
        var port = entry.Detail?.PortOfEntry?.Trim();
        if (!string.IsNullOrWhiteSpace(port))
        {
            foreach (var (keyword, label, lat, lng) in KnownPortPickupPoints)
            {
                if (port.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return (label, lat, lng);
                }
            }

            return ($"Port of entry — {port}", DefaultManilaPickup.Latitude, DefaultManilaPickup.Longitude);
        }

        return DefaultManilaPickup;
    }

    public static (string Label, decimal Latitude, decimal Longitude) ResolvePickup(Container container, Entry entry)
    {
        var stored = TryGetStoredPickup(container);
        if (stored is not null)
        {
            return stored.Value;
        }

        var firstTrail = container.Locations
            .Where(l => l.Latitude is not null && l.Longitude is not null)
            .OrderBy(l => l.RecordedAt)
            .FirstOrDefault();
        if (firstTrail is not null)
        {
            return ("Recorded pickup", firstTrail.Latitude!.Value, firstTrail.Longitude!.Value);
        }

        return ResolvePlannedPickup(entry);
    }

    private static readonly (string Label, decimal Latitude, decimal Longitude) DefaultManilaPickup =
        ("Port of Manila (MICP)", 14.5862m, 120.9681m);

    private static readonly (string Keyword, string Label, decimal Latitude, decimal Longitude)[] KnownPortPickupPoints =
    {
        ("SUBIC", "Subic Bay Freeport", 14.7954m, 120.2853m),
        ("BATANGAS", "Batangas Port", 13.7544m, 121.0458m),
        ("CEBU", "Cebu International Port", 10.3075m, 123.9054m),
        ("DAVAO", "Sasa Port, Davao", 7.1253m, 125.6670m),
        ("MANILA", DefaultManilaPickup.Label, DefaultManilaPickup.Latitude, DefaultManilaPickup.Longitude),
        ("MICP", DefaultManilaPickup.Label, DefaultManilaPickup.Latitude, DefaultManilaPickup.Longitude),
    };

    private static Dictionary<string, string> ParseForm(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
