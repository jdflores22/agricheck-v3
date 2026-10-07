using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// National food-security demo: BAI/BFAR/BPI imports with destination warehouses,
/// mix of in-pipeline (expected) and stored inventory (actual) for DA oversight.
/// </summary>
public static class FoodSecurityPipelineDemoSeeder
{
    public const string ReferencePrefix = "ENT-FS-";

    private sealed record FoodLane(
        string AgencyCode,
        string WarehouseCode,
        string CommodityName,
        string HsCode,
        decimal QuantityKg);

    private static readonly FoodLane[] Lanes =
    {
        new("BAI", "WH-MNL", "Frozen pork", "0203.11", 2_500m),
        new("BFAR", "WH-TAG", "Frozen fish fillet", "0302.11", 1_800m),
        new("BPI", "WH-QC", "Fresh potatoes", "0701.10", 3_200m),
    };

    public static async Task SeedAsync(AgriCheckDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await db.Entries.AnyAsync(e => e.ReferenceNo.StartsWith(ReferencePrefix), cancellationToken))
        {
            return;
        }

        var importer = await db.Users.FirstOrDefaultAsync(u => u.Email == "importer@agricheck.local", cancellationToken);
        if (importer is null)
        {
            return;
        }

        var warehouseStaff = await db.Users.FirstOrDefaultAsync(u => u.Email == "warehouse@agricheck.local", cancellationToken)
            ?? await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@agricheck.local", cancellationToken);
        if (warehouseStaff is null)
        {
            return;
        }

        var doctor = await db.Users.FirstOrDefaultAsync(u => u.Email == "doctor@agricheck.local", cancellationToken)
            ?? warehouseStaff;

        var now = DateTime.UtcNow;
        var sequence = 1;

        foreach (var lane in Lanes)
        {
            var agency = await db.Agencies.FirstOrDefaultAsync(a => a.Code == lane.AgencyCode && a.IsActive, cancellationToken);
            var facility = await db.WarehouseFacilities.FirstOrDefaultAsync(f => f.Code == lane.WarehouseCode && f.IsActive, cancellationToken);
            if (agency is null || facility is null)
            {
                continue;
            }

            for (var i = 0; i < 10; i++)
            {
                await AddPipelineOnlyEntryAsync(db, importer.Id, agency.Id, facility.Id, lane, sequence++, now, cancellationToken);
            }

            for (var i = 0; i < 15; i++)
            {
                await AddStoredEntryAsync(
                    db,
                    importer.Id,
                    agency.Id,
                    facility.Id,
                    lane,
                    sequence++,
                    now,
                    warehouseStaff.Id,
                    doctor.Id,
                    cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded national food-security import demo ({Prefix}*) — pipeline + warehouse stock for BAI/BFAR/BPI",
            ReferencePrefix);
    }

    private static async Task AddPipelineOnlyEntryAsync(
        AgriCheckDbContext db,
        long importerId,
        long agencyId,
        long facilityId,
        FoodLane lane,
        int sequence,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var entry = new Entry
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = $"{ReferencePrefix}{sequence:D4}",
            UserId = importerId,
            AgencyId = agencyId,
            EntryType = EntryType.Import,
            ImportTrack = EntryImportTrack.Regular,
            Status = EntryStatus.Submitted,
            SubmittedAt = now.AddDays(-sequence % 14),
            PaymentStatus = PaymentStatus.Unpaid,
            PaymentAmount = 1500m,
            FinalWarehouseFacilityId = facilityId,
            CompliancePhase = EntryCompliancePhase.AccreditationReady,
            FormDataJson =
                $"{{\"import_commodity_hs_code\":\"{lane.HsCode}\",\"quantity\":\"{lane.QuantityKg}\",\"unit\":\"kg\"}}",
        };
        db.Entries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        db.EntryDetails.Add(new EntryDetail
        {
            EntryId = entry.Id,
            CommodityName = lane.CommodityName,
            Quantity = lane.QuantityKg,
            Unit = "kg",
            OriginCountry = "Australia",
            DestinationCountry = "Philippines",
            PortOfEntry = "Manila",
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task AddStoredEntryAsync(
        AgriCheckDbContext db,
        long importerId,
        long agencyId,
        long facilityId,
        FoodLane lane,
        int sequence,
        DateTime now,
        long warehouseStaffId,
        long doctorUserId,
        CancellationToken cancellationToken)
    {
        var entry = new Entry
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = $"{ReferencePrefix}{sequence:D4}",
            UserId = importerId,
            AgencyId = agencyId,
            EntryType = EntryType.Import,
            ImportTrack = EntryImportTrack.Regular,
            Status = EntryStatus.InTransit,
            SubmittedAt = now.AddDays(-20 - sequence % 10),
            PaymentStatus = PaymentStatus.Paid,
            PaymentAmount = 1500m,
            FinalWarehouseFacilityId = facilityId,
            CompliancePhase = EntryCompliancePhase.AtWarehouse,
            FormDataJson =
                $"{{\"import_commodity_hs_code\":\"{lane.HsCode}\",\"quantity\":\"{lane.QuantityKg}\",\"unit\":\"kg\"}}",
        };
        db.Entries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        db.EntryDetails.Add(new EntryDetail
        {
            EntryId = entry.Id,
            CommodityName = lane.CommodityName,
            Quantity = lane.QuantityKg,
            Unit = "kg",
            OriginCountry = "Australia",
            DestinationCountry = "Philippines",
            PortOfEntry = "Manila",
        });

        var container = new Container
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            SequenceNumber = 1,
            ContainerNumber = $"FS{sequence:D6}",
            ContainerType = "40FT",
            Status = ContainerStatus.AtWarehouse,
            WarehouseFacilityId = facilityId,
            ArrivalTime = now.AddDays(-2 - sequence % 5),
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync(cancellationToken);

        db.ContainerDoctorInspections.Add(new ContainerDoctorInspection
        {
            Uuid = Guid.NewGuid(),
            ContainerId = container.Id,
            DoctorUserId = doctorUserId,
            Status = DoctorInspectionStatus.Approved,
            Findings = "Demo: cleared for CSW intake.",
            CompletedAt = now.AddDays(-3),
        });

        db.WarehouseInventories.Add(new WarehouseInventory
        {
            Uuid = Guid.NewGuid(),
            ContainerId = container.Id,
            WarehouseFacilityId = facilityId,
            LocationCode = $"FS-{lane.AgencyCode}-{sequence % 20:D2}",
            ReceivedAt = now.AddDays(-1 - sequence % 4),
            ReceivedByUserId = warehouseStaffId,
            Status = WarehouseInventoryStatus.Stored,
        });

        db.StorageTallySheets.Add(new StorageTallySheet
        {
            WarehouseFacilityId = facilityId,
            ContainerId = container.Id,
            EntryId = entry.Id,
            Direction = "In",
            CommodityDescription = lane.CommodityName,
            RecordedByUserId = warehouseStaffId,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
