using System.Data;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent dev/local backfill: DA warehouse LTO on active facilities and importer↔warehouse links from existing graph.
/// </summary>
public static class WarehouseLtoBackfillSeeder
{
    private static readonly string[] ClientRoleCodes = { "ROLE_IMPORTER", "ROLE_EXPORTER", "ROLE_BROKER" };

    public static async Task BackfillAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return;
        }

        if (!await TableExistsAsync(db, "warehouse_operating_licenses", cancellationToken))
        {
            logger.LogWarning(
                "Skipping warehouse LTO backfill — run EF migration 20260928120000_WarehouseLtoImporterFacilities first.");
            return;
        }

        var issuedByUserId = await db.Users
            .AsNoTracking()
            .Where(u => u.UserRoles.Any(ur => ur.Role.Code == "ROLE_ADMIN"))
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (issuedByUserId == 0)
        {
            issuedByUserId = await db.Users.AsNoTracking().Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);
        }

        if (issuedByUserId == 0)
        {
            logger.LogWarning("Skipping warehouse LTO backfill — no users in database.");
            return;
        }

        var now = DateTime.UtcNow;
        var activeWarehouses = await db.WarehouseFacilities
            .AsNoTracking()
            .Where(w => w.IsActive)
            .Select(w => new { w.Id, w.Code })
            .ToListAsync(cancellationToken);

        var licensedFacilityIds = await db.WarehouseOperatingLicenses
            .AsNoTracking()
            .Where(l => l.Status == WarehouseOperatingLicenseStatus.Active && l.ExpiresAt > now)
            .Select(l => l.WarehouseFacilityId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var licensedSet = licensedFacilityIds.ToHashSet();
        var ltoAdded = 0;

        foreach (var warehouse in activeWarehouses.Where(w => !licensedSet.Contains(w.Id)))
        {
            var licenseNumber = $"DA-LTO-{warehouse.Code}";
            var duplicateNumber = await db.WarehouseOperatingLicenses
                .AnyAsync(l => l.LicenseNumber == licenseNumber, cancellationToken);
            if (duplicateNumber)
            {
                licenseNumber = $"DA-LTO-{warehouse.Code}-{warehouse.Id}";
            }

            db.WarehouseOperatingLicenses.Add(new WarehouseOperatingLicense
            {
                WarehouseFacilityId = warehouse.Id,
                LicenseNumber = licenseNumber,
                Status = WarehouseOperatingLicenseStatus.Active,
                IssuedAt = now.AddYears(-1),
                ExpiresAt = now.AddYears(5),
                IssuedByUserId = issuedByUserId,
                Notes = "Auto-backfill for local/dev — replace with real DA mother-unit LTO in production.",
            });
            licensedSet.Add(warehouse.Id);
            ltoAdded++;
        }

        if (ltoAdded > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Warehouse LTO backfill: issued {Count} dev LTO record(s).", ltoAdded);
        }

        var clientUserIds = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ClientRoleCodes.Contains(ur.Role.Code))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var clientSet = clientUserIds.ToHashSet();

        var pairs = new HashSet<(long UserId, long WarehouseFacilityId)>();

        var fromEntries = await db.Entries
            .AsNoTracking()
            .Where(e => e.FinalWarehouseFacilityId != null)
            .Select(e => new { e.UserId, FacilityId = e.FinalWarehouseFacilityId!.Value })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var row in fromEntries)
        {
            pairs.Add((row.UserId, row.FacilityId));
        }

        var fromRegistrations = await db.FacilityRegistrations
            .AsNoTracking()
            .Select(r => new { r.SubmittedByUserId, r.WarehouseFacilityId })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var row in fromRegistrations)
        {
            pairs.Add((row.SubmittedByUserId, row.WarehouseFacilityId));
        }

        var existingAffiliations = await db.ImporterFacilityAffiliations
            .AsNoTracking()
            .Select(a => new { a.UserId, a.WarehouseFacilityId })
            .ToListAsync(cancellationToken);

        var existingSet = existingAffiliations
            .Select(a => (a.UserId, a.WarehouseFacilityId))
            .ToHashSet();

        var affiliationAdded = 0;
        foreach (var (userId, facilityId) in pairs)
        {
            if (!clientSet.Contains(userId) || !licensedSet.Contains(facilityId) || existingSet.Contains((userId, facilityId)))
            {
                continue;
            }

            db.ImporterFacilityAffiliations.Add(new ImporterFacilityAffiliation
            {
                UserId = userId,
                WarehouseFacilityId = facilityId,
            });
            existingSet.Add((userId, facilityId));
            affiliationAdded++;
        }

        foreach (var userId in clientSet)
        {
            if (existingSet.Any(a => a.UserId == userId))
            {
                continue;
            }

            var fallbackFacility = activeWarehouses.FirstOrDefault(w => licensedSet.Contains(w.Id));
            if (fallbackFacility is null)
            {
                continue;
            }

            db.ImporterFacilityAffiliations.Add(new ImporterFacilityAffiliation
            {
                UserId = userId,
                WarehouseFacilityId = fallbackFacility.Id,
            });
            existingSet.Add((userId, fallbackFacility.Id));
            affiliationAdded++;
        }

        if (affiliationAdded > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Warehouse LTO backfill: linked {Count} importer facility affiliation(s).", affiliationAdded);
        }
    }

    private static async Task<bool> TableExistsAsync(
        AgriCheckDbContext db,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*) FROM information_schema.tables
                WHERE table_schema = DATABASE() AND table_name = @table
                """;
            var param = command.CreateParameter();
            param.ParameterName = "@table";
            param.Value = tableName;
            command.Parameters.Add(param);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}
