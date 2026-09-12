using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class WarehouseProfilingSchemaSeeder
{
    private const string SettingKey = "warehouse_profiling_schema_applied";

    public static async Task EnsureAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        if (await ColumnExistsAsync(db, "warehouse_facilities", "BarangayId", cancellationToken))
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE `warehouse_facilities`
                ADD COLUMN `BarangayId` bigint NULL,
                ADD COLUMN `CityId` bigint NULL,
                ADD COLUMN `Latitude` decimal(10,7) NULL,
                ADD COLUMN `Longitude` decimal(10,7) NULL,
                ADD COLUMN `ProvinceId` bigint NULL,
                ADD COLUMN `RegionId` bigint NULL,
                ADD COLUMN `StreetAddress` varchar(500) NULL,
                ADD COLUMN `ZipCode` varchar(16) NULL;
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX `IX_warehouse_facilities_BarangayId` ON `warehouse_facilities` (`BarangayId`);",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX `IX_warehouse_facilities_CityId` ON `warehouse_facilities` (`CityId`);",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX `IX_warehouse_facilities_ProvinceId` ON `warehouse_facilities` (`ProvinceId`);",
            cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX `IX_warehouse_facilities_RegionId` ON `warehouse_facilities` (`RegionId`);",
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE `warehouse_facilities`
                ADD CONSTRAINT `FK_warehouse_facilities_address_barangays_BarangayId`
                    FOREIGN KEY (`BarangayId`) REFERENCES `address_barangays` (`Id`) ON DELETE SET NULL,
                ADD CONSTRAINT `FK_warehouse_facilities_address_cities_CityId`
                    FOREIGN KEY (`CityId`) REFERENCES `address_cities` (`Id`) ON DELETE SET NULL,
                ADD CONSTRAINT `FK_warehouse_facilities_address_provinces_ProvinceId`
                    FOREIGN KEY (`ProvinceId`) REFERENCES `address_provinces` (`Id`) ON DELETE SET NULL,
                ADD CONSTRAINT `FK_warehouse_facilities_address_regions_RegionId`
                    FOREIGN KEY (`RegionId`) REFERENCES `address_regions` (`Id`) ON DELETE SET NULL;
            """,
            cancellationToken);

        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == SettingKey, cancellationToken);
        if (setting is null)
        {
            db.SystemSettings.Add(new SystemSetting
            {
                SettingKey = SettingKey,
                SettingValue = DateTime.UtcNow.ToString("O"),
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<bool> ColumnExistsAsync(
        AgriCheckDbContext db,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

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
}
