namespace AgriCheck.Application.Common;

public record AddressOptionDto(long Id, string Code, string Name);

public record AddressBarangayOptionDto(long Id, string Code, string Name, string ZipCode);

public record RegisteredWarehouseDto(
    long Id,
    string Code,
    string Name,
    string FormattedAddress,
    string? RegionName,
    string? ProvinceName,
    string? CityName,
    string? BarangayName,
    string? StreetAddress,
    string? ZipCode,
    decimal? Latitude,
    decimal? Longitude);

public static class WarehouseAddressFormatter
{
    public static string Format(
        string? streetAddress,
        string? barangayName,
        string? cityName,
        string? provinceName,
        string? regionName,
        string? zipCode,
        string? fallbackLocation = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(streetAddress)) parts.Add(streetAddress.Trim());
        if (!string.IsNullOrWhiteSpace(barangayName)) parts.Add(barangayName.Trim());
        if (!string.IsNullOrWhiteSpace(cityName)) parts.Add(cityName.Trim());
        if (!string.IsNullOrWhiteSpace(provinceName)) parts.Add(provinceName.Trim());
        if (!string.IsNullOrWhiteSpace(regionName)) parts.Add(regionName.Trim());
        if (!string.IsNullOrWhiteSpace(zipCode)) parts.Add(zipCode.Trim());

        if (parts.Count > 0)
        {
            return string.Join(", ", parts);
        }

        return fallbackLocation?.Trim() ?? string.Empty;
    }
}
