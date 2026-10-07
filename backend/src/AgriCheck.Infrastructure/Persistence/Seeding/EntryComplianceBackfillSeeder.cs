using System.Text.Json;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class EntryComplianceBackfillSeeder
{
    public static async Task BackfillFinalWarehouseFacilityIdsAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var entries = await db.Entries
            .Where(e => e.FinalWarehouseFacilityId == null && e.FormDataJson != null)
            .Select(e => new { e.Id, e.FormDataJson })
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return;
        }

        var updated = 0;
        foreach (var row in entries)
        {
            var facilityId = TryParseWarehouseFacilityId(row.FormDataJson);
            if (facilityId is null)
            {
                continue;
            }

            var exists = await db.WarehouseFacilities.AnyAsync(
                f => f.Id == facilityId && f.IsActive,
                cancellationToken);
            if (!exists)
            {
                continue;
            }

            await db.Entries
                .Where(e => e.Id == row.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.FinalWarehouseFacilityId, facilityId),
                    cancellationToken);
            updated++;
        }

        if (updated > 0)
        {
            logger.LogInformation("Backfilled FinalWarehouseFacilityId on {Count} entries.", updated);
        }
    }

    internal static long? TryParseWarehouseFacilityId(string? formDataJson)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(formDataJson);
            var root = doc.RootElement;
            foreach (var key in new[]
                     {
                         "finalWarehouseFacilityId",
                         "warehouseFacilityId",
                         "final_warehouse_facility_id",
                         "destinationWarehouseFacilityId",
                     })
            {
                if (root.TryGetProperty(key, out var prop) && prop.TryGetInt64(out var id) && id > 0)
                {
                    return id;
                }
            }

            if (root.TryGetProperty("warehouseFacilityCode", out var codeProp)
                && codeProp.GetString() is { Length: > 0 } code)
            {
                return null; // resolved in async path only — skip here
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
