using AgriCheck.Application.ClientPortal;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal static class WarehouseStaffAccessHelper
{
    public static async Task<IReadOnlyList<long>> GetAccessibleWarehouseIdsAsync(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        long userId,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole("ROLE_ADMIN"))
        {
            return await db.WarehouseFacilities.AsNoTracking()
                .Where(f => f.IsActive)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);
        }

        return await db.WarehouseStaffAssignments.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => a.WarehouseFacilityId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public static async Task EnsureCanAccessWarehouseAsync(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        long userId,
        long warehouseFacilityId,
        CancellationToken cancellationToken)
    {
        var ids = await GetAccessibleWarehouseIdsAsync(db, currentUser, userId, cancellationToken);
        if (!ids.Contains(warehouseFacilityId))
        {
            throw new ClientPortalException(
                "WAREHOUSE_ACCESS_DENIED",
                "You are not assigned to this warehouse facility. Contact your administrator.");
        }
    }
}
