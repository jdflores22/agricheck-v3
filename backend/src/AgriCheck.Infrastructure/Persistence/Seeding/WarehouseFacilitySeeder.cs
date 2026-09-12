using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class WarehouseFacilitySeeder
{
    private sealed record WarehouseSeed(
        string Code,
        string Name,
        int Capacity,
        decimal Latitude,
        decimal Longitude,
        string? CityCode,
        string? BarangayName,
        string StreetAddress,
        string? ZipCode,
        string? LocationFallback);

    private static readonly WarehouseSeed[] Warehouses =
    {
        new("WH-MNL", "Manila Cold Storage", 500, 14.5832m, 120.9822m, "MNL", "Ermita", "1230 Roxas Boulevard", "1000", null),
        new("WH-QC", "Quezon City Agri Depot", 420, 14.6196m, 121.0569m, "QC", "Cubao", "45 EDSA corner Main Avenue", "1109", null),
        new("WH-MKT", "Makati Freeport Warehouse", 380, 14.5547m, 121.0244m, "MKT", "San Lorenzo", "6788 Ayala Avenue", "1223", null),
        new("WH-TAG", "Taguig Portside Storage", 450, 14.5310m, 121.0466m, "TAG", "Fort Bonifacio", "Block 5 Bonifacio Global City", "1634", null),
        new("WH-PSG", "Pasig Cold Chain Hub", 360, 14.5736m, 121.0709m, "PSG", "Kapitolyo", "88 Shaw Boulevard", "1603", null),
        new("WH-MUN", "Muntinlupa Bonded Warehouse", 320, 14.4192m, 121.0373m, "MUN", "Alabang", "Commerce Avenue corner Filinvest", "1780", null),
        new("WH-PRQ", "Parañaque Inspection Warehouse", 280, 14.5346m, 120.9920m, "PRQ", "Baclaran", "1500 Ninoy Aquino Avenue", "1702", null),
        new("WH-CEB", "Cebu Logistics Hub", 300, 10.3157m, 123.8854m, null, null, "North Reclamation Area", "6000", "Cebu City, Cebu"),
        new("WH-PAM", "Pampanga Regional Warehouse", 350, 15.1850m, 120.5370m, null, null, "Clark Freeport Zone", "2023", "Angeles City, Pampanga"),
        new("WH-BAT", "Batangas Port Warehouse", 290, 13.7565m, 121.0583m, null, null, "Batangas International Port", "4200", "Batangas City, Batangas"),
        new("WH-DVO", "Davao Agri Terminal", 310, 7.0731m, 125.6128m, null, null, "Sasa Wharf Road", "8000", "Davao City, Davao del Sur"),
        new("WH-ILO", "Iloilo Grains Storage", 260, 10.7202m, 122.5621m, null, null, "Brgy. Loboc, Lapuz District", "5000", "Iloilo City, Iloilo"),
    };

    public static async Task SeedAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var seed in Warehouses)
        {
            var existing = await db.WarehouseFacilities
                .Include(w => w.Region)
                .Include(w => w.Province)
                .Include(w => w.City)
                .Include(w => w.Barangay)
                .FirstOrDefaultAsync(w => w.Code == seed.Code, cancellationToken);

            if (existing is null)
            {
                existing = new WarehouseFacility { Code = seed.Code, IsActive = true };
                db.WarehouseFacilities.Add(existing);
            }

            existing.Name = seed.Name;
            existing.Capacity = seed.Capacity;
            existing.Latitude = seed.Latitude;
            existing.Longitude = seed.Longitude;
            existing.StreetAddress = seed.StreetAddress;
            existing.ZipCode = seed.ZipCode;
            existing.Location = seed.LocationFallback;

            if (!string.IsNullOrWhiteSpace(seed.CityCode) && !string.IsNullOrWhiteSpace(seed.BarangayName))
            {
                var resolved = await ResolveAddressAsync(db, seed.CityCode, seed.BarangayName, cancellationToken);
                existing.RegionId = resolved.RegionId;
                existing.ProvinceId = resolved.ProvinceId;
                existing.CityId = resolved.CityId;
                existing.BarangayId = resolved.BarangayId;
                if (string.IsNullOrWhiteSpace(existing.ZipCode))
                {
                    existing.ZipCode = resolved.ZipCode;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<(long? RegionId, long? ProvinceId, long? CityId, long? BarangayId, string? ZipCode)> ResolveAddressAsync(
        AgriCheckDbContext db,
        string cityCode,
        string barangayName,
        CancellationToken cancellationToken)
    {
        var city = await db.AddressCities
            .Include(c => c.Province)
            .Include(c => c.Barangays)
            .FirstOrDefaultAsync(c => c.Code == cityCode, cancellationToken);

        if (city is null)
        {
            return (null, null, null, null, null);
        }

        var barangay = city.Barangays.FirstOrDefault(b => string.Equals(b.Name, barangayName, StringComparison.OrdinalIgnoreCase));
        return (city.Province.RegionId, city.ProvinceId, city.Id, barangay?.Id, barangay?.ZipCode ?? city.Barangays.FirstOrDefault()?.ZipCode);
    }
}
