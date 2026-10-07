using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

public static class WarehouseOperatingLicenseHelper
{
    public static async Task<HashSet<long>> GetActiveLicensedWarehouseIdsAsync(
        AgriCheckDbContext db,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var ids = await db.WarehouseOperatingLicenses
            .AsNoTracking()
            .Where(l => l.Status == WarehouseOperatingLicenseStatus.Active && l.ExpiresAt > now)
            .Select(l => l.WarehouseFacilityId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public static async Task<bool> HasActiveLicenseAsync(
        AgriCheckDbContext db,
        long warehouseFacilityId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await db.WarehouseOperatingLicenses
            .AsNoTracking()
            .AnyAsync(
                l => l.WarehouseFacilityId == warehouseFacilityId
                    && l.Status == WarehouseOperatingLicenseStatus.Active
                    && l.ExpiresAt > now,
                cancellationToken);
    }

    public static async Task<WarehouseOperatingLicense?> GetCurrentLicenseAsync(
        AgriCheckDbContext db,
        long warehouseFacilityId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await db.WarehouseOperatingLicenses
            .AsNoTracking()
            .Where(l => l.WarehouseFacilityId == warehouseFacilityId
                        && l.Status == WarehouseOperatingLicenseStatus.Active
                        && l.ExpiresAt > now)
            .OrderByDescending(l => l.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<Dictionary<long, WarehouseOperatingLicense>> LoadActiveLicensesByWarehouseIdsAsync(
        AgriCheckDbContext db,
        IReadOnlyList<long> warehouseIds,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        if (warehouseIds.Count == 0)
        {
            return new Dictionary<long, WarehouseOperatingLicense>();
        }

        var list = await db.WarehouseOperatingLicenses.AsNoTracking()
            .Where(l => warehouseIds.Contains(l.WarehouseFacilityId)
                        && l.Status == WarehouseOperatingLicenseStatus.Active
                        && l.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        return list
            .GroupBy(l => l.WarehouseFacilityId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IssuedAt).First());
    }
}
