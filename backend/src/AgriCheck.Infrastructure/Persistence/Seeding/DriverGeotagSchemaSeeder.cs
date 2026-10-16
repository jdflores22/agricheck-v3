using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// Ensures incident reports can store multiple geotag photos per container (non-unique photo type index).
/// </summary>
public static class DriverGeotagSchemaSeeder
{
    public static async Task EnsureAsync(AgriCheckDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE container_inspection_photos
                DROP INDEX IX_container_inspection_photos_ContainerId_PhotoType
                """,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Drop unique container inspection photo index skipped (may already be updated).");
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IX_container_inspection_photos_ContainerId_PhotoType
                ON container_inspection_photos (ContainerId, PhotoType)
                """,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Create container inspection photo index skipped (may already exist).");
        }
    }
}
