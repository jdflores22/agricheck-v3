using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// Records EF migrations when legacy schema seeders already created the objects (avoids "table already exists" on Migrate).
/// </summary>
public static class MigrationHistoryBaselineHelper
{
    private const string ProductVersion = "7.0.20";

    public static async Task EnsureRecordedAsync(
        AgriCheckDbContext db,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            SELECT {0}, {1}
            WHERE NOT EXISTS (
              SELECT 1 FROM `__EFMigrationsHistory`
              WHERE `MigrationId` = {0}
            );
            """,
            new object[] { migrationId, ProductVersion },
            cancellationToken);
    }

    public static async Task BaselineSchemaDriftBeforeMigrateAsync(
        AgriCheckDbContext db,
        CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();

        if (await SchemaIntrospectionHelper.TableExistsAsync(connection, "operator_vehicles", cancellationToken))
        {
            await EnsureRecordedAsync(db, "20260914130000_OperatorFleet", cancellationToken);
        }

        if (await SchemaIntrospectionHelper.TableExistsAsync(connection, "warehouse_staff_assignments", cancellationToken))
        {
            await EnsureRecordedAsync(db, "20260928190000_WarehouseStaffLtoApplications", cancellationToken);
        }

        if (await SchemaIntrospectionHelper.TableExistsAsync(connection, "warehouse_importer_allocations", cancellationToken))
        {
            await EnsureRecordedAsync(db, "20260928195000_WarehouseImporterAllocations", cancellationToken);
        }
    }
}
