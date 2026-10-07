using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal static class WarehouseAllocationMetricsHelper
{
    private static readonly EntryStatus[] ExcludedEntryStatuses =
    {
        EntryStatus.Cancelled,
        EntryStatus.Rejected,
    };

    public static async Task<decimal> SumImporterCommittedKgAsync(
        AgriCheckDbContext db,
        long importerUserId,
        long warehouseFacilityId,
        long? excludeEntryId,
        CancellationToken cancellationToken)
    {
        var query = db.Entries.AsNoTracking()
            .Include(e => e.Detail)
            .Where(e => e.UserId == importerUserId
                && e.FinalWarehouseFacilityId == warehouseFacilityId
                && !ExcludedEntryStatuses.Contains(e.Status));

        if (excludeEntryId is not null)
        {
            query = query.Where(e => e.Id != excludeEntryId.Value);
        }

        var entries = await query.ToListAsync(cancellationToken);
        return entries.Sum(e => ShipmentQuantityHelper.ToKg(e.Detail?.Quantity ?? 0, e.Detail?.Unit));
    }
}
