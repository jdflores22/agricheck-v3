using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// MySQL requires a supporting index on FK columns before the composite (ContainerId, PhotoType) index can be dropped.
/// </summary>
internal static class ContainerInspectionPhotoIndexHelper
{
    internal const string ContainerIdIndexName = "IX_container_inspection_photos_ContainerId";
    internal const string ContainerPhotoTypeIndexName = "IX_container_inspection_photos_ContainerId_PhotoType";

    public static async Task EnsureNonUniquePhotoTypeIndexAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!await TableExistsAsync(db, logger, cancellationToken))
        {
            return;
        }

        await EnsureContainerIdIndexAsync(db, logger, cancellationToken);

        var isUnique = await IsCompositeIndexUniqueAsync(db, logger, cancellationToken);
        if (isUnique == false)
        {
            return;
        }

        if (isUnique == true)
        {
            logger.LogWarning(
                "Rebuilding {IndexName} as non-unique (MySQL FK requires {ContainerIdIndex} first).",
                ContainerPhotoTypeIndexName,
                ContainerIdIndexName);
        }

        await DropCompositeIndexAsync(db, logger, cancellationToken);
        await CreateCompositeIndexAsync(db, logger, cancellationToken);
    }

    public static async Task ApplyMigrationStepsAsync(
        AgriCheckDbContext db,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerIdIndexAsync(db, NullLogger.Instance, cancellationToken);
        await DropCompositeIndexAsync(db, NullLogger.Instance, cancellationToken);
        await CreateCompositeIndexAsync(db, NullLogger.Instance, cancellationToken);
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
            logger.LogDebug(ex, "Could not probe container_inspection_photos.");
            return true;
        }
    }

    private static async Task EnsureContainerIdIndexAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var exists = await db.Database.SqlQueryRaw<int>(
                    $"""
                     SELECT COUNT(*) AS Value
                     FROM information_schema.statistics
                     WHERE table_schema = DATABASE()
                       AND table_name = 'container_inspection_photos'
                       AND index_name = '{ContainerIdIndexName}'
                     """)
                .FirstOrDefaultAsync(cancellationToken);
            if (exists > 0)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not probe {IndexName}; attempting create.", ContainerIdIndexName);
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                $"""
                 CREATE INDEX {ContainerIdIndexName}
                 ON container_inspection_photos (ContainerId)
                 """,
                cancellationToken);
            logger.LogInformation("Created index {IndexName} for container_inspection_photos FK support.", ContainerIdIndexName);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Create index {IndexName} skipped (may already exist).", ContainerIdIndexName);
        }
    }

    private static async Task<bool?> IsCompositeIndexUniqueAsync(
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
                       AND index_name = '{ContainerPhotoTypeIndexName}'
                     GROUP BY NON_UNIQUE
                     """)
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                return null;
            }

            return rows.Any(r => r.NonUnique == 0);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not read metadata for {IndexName}.", ContainerPhotoTypeIndexName);
            return null;
        }
    }

    private static async Task DropCompositeIndexAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                $"""
                 ALTER TABLE container_inspection_photos
                 DROP INDEX {ContainerPhotoTypeIndexName}
                 """,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Drop index {IndexName} skipped.", ContainerPhotoTypeIndexName);
        }
    }

    private static async Task CreateCompositeIndexAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                $"""
                 CREATE INDEX {ContainerPhotoTypeIndexName}
                 ON container_inspection_photos (ContainerId, PhotoType)
                 """,
                cancellationToken);
            logger.LogInformation("Ensured non-unique index {IndexName}.", ContainerPhotoTypeIndexName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create index {IndexName}.", ContainerPhotoTypeIndexName);
            throw;
        }
    }

    private sealed class IndexUniquenessRow
    {
        public int NonUnique { get; set; }
        public int RowCount { get; set; }
    }

    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
