using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// Ensures incident reports can store multiple geotag photos per container (non-unique photo type index).
/// </summary>
public static class DriverGeotagSchemaSeeder
{
    private const string IndexName = "IX_container_inspection_photos_ContainerId_PhotoType";

    public static async Task EnsureAsync(AgriCheckDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (!await TableExistsAsync(db, logger, cancellationToken))
        {
            return;
        }

        var isUnique = await IsUniqueIndexAsync(db, logger, cancellationToken);
        if (isUnique == false)
        {
            return;
        }

        if (isUnique == true)
        {
            logger.LogWarning(
                "Container inspection photo index {IndexName} is still UNIQUE; rebuilding as non-unique for incident reports.",
                IndexName);
        }

        await RecreateIndexAsync(db, logger, cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var count = await db.Database.SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS Value
                    FROM information_schema.tables
                    WHERE table_schema = DATABASE()
                      AND table_name = 'container_inspection_photos'
                    """)
                .FirstOrDefaultAsync(cancellationToken);
            return count > 0;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not probe container_inspection_photos table; attempting index rebuild.");
            return true;
        }
    }

    /// <returns>true = unique, false = non-unique, null = unknown / missing</returns>
    private static async Task<bool?> IsUniqueIndexAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await db.Database.SqlQueryRaw<IndexUniquenessRow>(
                    $"""
                     SELECT NON_UNIQUE AS NonUnique, COUNT(*) AS RowCount
                     FROM information_schema.statistics
                     WHERE table_schema = DATABASE()
                       AND table_name = 'container_inspection_photos'
                       AND index_name = '{IndexName}'
                     GROUP BY NON_UNIQUE
                     """)
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                return null;
            }

            if (rows.Any(r => r.NonUnique == 0))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not read index metadata for {IndexName}; will attempt rebuild.", IndexName);
            return null;
        }
    }

    private static async Task RecreateIndexAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                $"""
                 ALTER TABLE container_inspection_photos
                 DROP INDEX {IndexName}
                 """,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Drop index {IndexName} skipped (may not exist).", IndexName);
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                $"""
                 CREATE INDEX {IndexName}
                 ON container_inspection_photos (ContainerId, PhotoType)
                 """,
                cancellationToken);
            logger.LogInformation("Recreated non-unique index {IndexName} on container_inspection_photos.", IndexName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to recreate non-unique index {IndexName}.", IndexName);
            throw;
        }
    }

    private sealed class IndexUniquenessRow
    {
        public int NonUnique { get; set; }
        public int RowCount { get; set; }
    }
}
