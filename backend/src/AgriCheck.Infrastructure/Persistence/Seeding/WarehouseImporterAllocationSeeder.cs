using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class WarehouseImporterAllocationSeeder
{
    private const string DemoImporterEmail = "importer@agricheck.local";
    private const string DemoWarehouseCode = "WH-MNL";
    private const string FishScopeJson = "[\"FishAndAquaticProducts\"]";

    public static async Task SeedAsync(AgriCheckDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await db.WarehouseImporterAllocations.AnyAsync(cancellationToken);
        }
        catch
        {
            return;
        }

        var importer = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == DemoImporterEmail, cancellationToken);
        var warehouse = await db.WarehouseFacilities.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Code == DemoWarehouseCode, cancellationToken);

        if (importer is null || warehouse is null)
        {
            return;
        }

        var registration = await db.FacilityRegistrations
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouse.Id, cancellationToken);

        if (registration is not null)
        {
            if (string.IsNullOrWhiteSpace(registration.ProductsJson) || registration.ProductsJson == "[]")
            {
                registration.ProductsJson = FishScopeJson;
            }

            registration.MaxCapacityKg ??= 500_000m;
            await db.SaveChangesAsync(cancellationToken);
        }

        var hasAffiliation = await db.ImporterFacilityAffiliations
            .AnyAsync(a => a.UserId == importer.Id && a.WarehouseFacilityId == warehouse.Id, cancellationToken);

        if (!hasAffiliation)
        {
            db.ImporterFacilityAffiliations.Add(new ImporterFacilityAffiliation
            {
                UserId = importer.Id,
                WarehouseFacilityId = warehouse.Id,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var hasAllocation = await db.WarehouseImporterAllocations
            .AnyAsync(
                a => a.ImporterUserId == importer.Id
                    && a.WarehouseFacilityId == warehouse.Id
                    && a.Status == WarehouseImporterAllocationStatus.Active,
                cancellationToken);

        if (!hasAllocation)
        {
            db.WarehouseImporterAllocations.Add(new WarehouseImporterAllocation
            {
                ImporterUserId = importer.Id,
                WarehouseFacilityId = warehouse.Id,
                ContractReference = "DEMO-MNL-150MT",
                AllocatedCapacityKg = 150_000m,
                AllowedCommodityClassesJson = FishScopeJson,
                ValidFrom = DateTime.UtcNow.AddYears(-1),
                Status = WarehouseImporterAllocationStatus.Active,
            });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded demo warehouse importer allocation (150 MT fish) for {Email} at {Code}", DemoImporterEmail, DemoWarehouseCode);
        }
    }
}
