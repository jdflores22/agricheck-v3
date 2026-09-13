using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class DriverRegistrationSchemaSeeder
{
    public static async Task EnsureAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await TableExistsAsync(db, "operator_invite_codes", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE `operator_invite_codes` (
                  `Id` bigint NOT NULL AUTO_INCREMENT,
                  `Code` varchar(32) NOT NULL,
                  `OperatorUserId` bigint NOT NULL,
                  `Label` varchar(128) NULL,
                  `MaxUses` int NOT NULL,
                  `UsedCount` int NOT NULL,
                  `ExpiresAt` datetime(6) NULL,
                  `IsActive` tinyint(1) NOT NULL,
                  `CreatedAt` datetime(6) NOT NULL,
                  `UpdatedAt` datetime(6) NOT NULL,
                  PRIMARY KEY (`Id`),
                  UNIQUE KEY `IX_operator_invite_codes_Code` (`Code`),
                  KEY `IX_operator_invite_codes_OperatorUserId` (`OperatorUserId`),
                  CONSTRAINT `FK_operator_invite_codes_users_OperatorUserId`
                    FOREIGN KEY (`OperatorUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """,
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "driver_profiles", "BirthDate", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `driver_profiles` ADD `BirthDate` datetime(6) NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "driver_profiles", "RegionId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `driver_profiles` ADD `RegionId` bigint NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "driver_profiles", "ProvinceId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `driver_profiles` ADD `ProvinceId` bigint NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "driver_profiles", "CityId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `driver_profiles` ADD `CityId` bigint NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "driver_profiles", "BarangayId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `driver_profiles` ADD `BarangayId` bigint NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "driver_profiles", "ZipCode", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `driver_profiles` ADD `ZipCode` varchar(16) NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "driver_profiles", "StreetAddress", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `driver_profiles` ADD `StreetAddress` varchar(255) NULL;",
                cancellationToken);
        }

        if (!await ColumnExistsAsync(db, "driver_profiles", "OperatorUserId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `driver_profiles` ADD `OperatorUserId` bigint NULL;",
                cancellationToken);
        }

        if (!await IndexExistsAsync(db, "driver_profiles", "IX_driver_profiles_OperatorUserId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE INDEX `IX_driver_profiles_OperatorUserId` ON `driver_profiles` (`OperatorUserId`);",
                cancellationToken);
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            SELECT '20260913120000_DriverRegistrationAndInvites', '8.0.0'
            WHERE NOT EXISTS (
              SELECT 1 FROM `__EFMigrationsHistory`
              WHERE `MigrationId` = '20260913120000_DriverRegistrationAndInvites'
            );
            """,
            cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(
        AgriCheckDbContext db,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName
            """;

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "@tableName";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
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
