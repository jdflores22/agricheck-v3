using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class EntrySchemaSeeder
{
    public static async Task EnsureAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await ColumnExistsAsync(db, "entries", "MavDocumentStatus", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `entries` ADD `MavDocumentStatus` varchar(32) NOT NULL DEFAULT 'NotProvided';",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "entries", "MavNo", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `entries` ADD `MavNo` varchar(64) NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "entries", "MavRemarks", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `entries` ADD `MavRemarks` varchar(2000) NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "entries", "PrimaryMicId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `entries` ADD `PrimaryMicId` bigint NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "entries", "ImportTrack", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `entries` ADD `ImportTrack` varchar(16) NOT NULL DEFAULT 'Regular';",
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE `entries`
                SET `ImportTrack` = 'Mav'
                WHERE `EntryType` = 'Import'
                  AND (
                        `MavNo` IS NOT NULL
                     OR `MavDocumentStatus` <> 'NotProvided'
                     OR `PrimaryMicId` IS NOT NULL
                     OR `Id` IN (SELECT DISTINCT `EntryId` FROM `mic_utilizations`)
                  );
                """,
                cancellationToken);
        }

        if (!await IndexExistsAsync(db, "entries", "IX_entries_MavNo", cancellationToken)
            && await ColumnExistsAsync(db, "entries", "MavNo", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX `IX_entries_MavNo` ON `entries` (`MavNo`);",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "containers", "FormDataJson", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `containers` ADD `FormDataJson` json NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "containers", "SequenceNumber", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `containers` ADD `SequenceNumber` int NOT NULL DEFAULT 1;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "containers", "ContainerType", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `containers` ADD `ContainerType` varchar(50) NULL;",
                cancellationToken);
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            SELECT '20260911061827_EntryMavIntegration', '8.0.0'
            WHERE NOT EXISTS (
              SELECT 1 FROM `__EFMigrationsHistory`
              WHERE `MigrationId` = '20260911061827_EntryMavIntegration'
            );
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            SELECT '20260912150000_EntryImportTrack', '8.0.0'
            WHERE NOT EXISTS (
              SELECT 1 FROM `__EFMigrationsHistory`
              WHERE `MigrationId` = '20260912150000_EntryImportTrack'
            );
            """,
            cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(
        AgriCheckDbContext db,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName
            """;

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var columnParam = command.CreateParameter();
        columnParam.ParameterName = "@columnName";
        columnParam.Value = columnName;
        command.Parameters.Add(columnParam);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
    }

    private static async Task<bool> IndexExistsAsync(
        AgriCheckDbContext db,
        string tableName,
        string indexName,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
              AND INDEX_NAME = @indexName
            """;

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var indexParam = command.CreateParameter();
        indexParam.ParameterName = "@indexName";
        indexParam.Value = indexName;
        command.Parameters.Add(indexParam);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
    }
}
