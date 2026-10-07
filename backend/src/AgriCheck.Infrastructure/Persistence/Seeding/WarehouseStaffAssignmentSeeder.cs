using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class WarehouseStaffAssignmentSeeder
{
    public const string DemoWarehouseStaffEmail = "warehouse@agricheck.local";
    public const string DemoWarehouseFacilityCode = "WH-MNL";

    public static async Task SeedAsync(AgriCheckDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return;
        }

        try
        {
            _ = await db.WarehouseStaffAssignments.AnyAsync(cancellationToken);
        }
        catch
        {
            logger.LogWarning("Skipping warehouse staff assignment seed — run migration 20260928190000_WarehouseStaffLtoApplications first.");
            return;
        }

        await EnsureDemoManilaColdStorageStaffAsync(db, logger, cancellationToken);
    }

    /// <summary>
    /// Demo warehouse login operates <see cref="DemoWarehouseFacilityCode"/> (Manila Cold Storage) only.
    /// </summary>
    public static async Task EnsureDemoManilaColdStorageStaffAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var staff = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == DemoWarehouseStaffEmail, cancellationToken);

        if (staff is null)
        {
            return;
        }

        var manila = await db.WarehouseFacilities
            .FirstOrDefaultAsync(f => f.Code == DemoWarehouseFacilityCode && f.IsActive, cancellationToken);

        if (manila is null)
        {
            logger.LogWarning("Demo warehouse facility {Code} not found; skipping staff assignment.", DemoWarehouseFacilityCode);
            return;
        }

        if (staff.Profile is not null)
        {
            staff.Profile.FirstName = "Manila Cold Storage";
            staff.Profile.LastName = "Operator";
        }

        var stray = await db.WarehouseStaffAssignments
            .Where(a => a.UserId == staff.Id && a.WarehouseFacilityId != manila.Id)
            .ToListAsync(cancellationToken);

        if (stray.Count > 0)
        {
            db.WarehouseStaffAssignments.RemoveRange(stray);
            logger.LogInformation("Removed {Count} extra demo warehouse staff assignments (non-MNL).", stray.Count);
        }

        var hasMnl = await db.WarehouseStaffAssignments
            .AnyAsync(a => a.UserId == staff.Id && a.WarehouseFacilityId == manila.Id, cancellationToken);

        if (!hasMnl)
        {
            db.WarehouseStaffAssignments.Add(new WarehouseStaffAssignment
            {
                UserId = staff.Id,
                WarehouseFacilityId = manila.Id,
                Role = WarehouseStaffAssignmentRole.ComplianceAdmin,
            });
            logger.LogInformation("Assigned {Email} to {Facility} ({Code}).", DemoWarehouseStaffEmail, manila.Name, manila.Code);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
