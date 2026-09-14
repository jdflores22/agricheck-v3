using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class OperatorFleetSchemaSeeder
{
    public static async Task EnsureAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();

        if (!await SchemaIntrospectionHelper.TableExistsAsync(connection, "operator_vehicles", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE `operator_vehicles` (
                  `Id` bigint NOT NULL AUTO_INCREMENT,
                  `Uuid` char(36) COLLATE ascii_general_ci NOT NULL,
                  `OperatorUserId` bigint NOT NULL,
                  `PlateNumber` varchar(32) NOT NULL,
                  `VehicleType` varchar(100) NOT NULL,
                  `Description` varchar(255) NULL,
                  `DefaultDriverUserId` bigint NULL,
                  `IsActive` tinyint(1) NOT NULL,
                  `CreatedAt` datetime(6) NOT NULL,
                  `UpdatedAt` datetime(6) NOT NULL,
                  PRIMARY KEY (`Id`),
                  UNIQUE KEY `IX_operator_vehicles_Uuid` (`Uuid`),
                  UNIQUE KEY `IX_operator_vehicles_OperatorUserId_PlateNumber` (`OperatorUserId`, `PlateNumber`),
                  KEY `IX_operator_vehicles_DefaultDriverUserId` (`DefaultDriverUserId`),
                  CONSTRAINT `FK_operator_vehicles_users_DefaultDriverUserId`
                    FOREIGN KEY (`DefaultDriverUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL,
                  CONSTRAINT `FK_operator_vehicles_users_OperatorUserId`
                    FOREIGN KEY (`OperatorUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """,
                cancellationToken);
        }

        if (!await SchemaIntrospectionHelper.ColumnExistsAsync(connection, "containers", "AssignedOperatorVehicleId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `containers` ADD `AssignedOperatorVehicleId` bigint NULL;",
                cancellationToken);
        }

        if (!await SchemaIntrospectionHelper.IndexExistsAsync(connection, "containers", "IX_containers_AssignedOperatorVehicleId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE INDEX `IX_containers_AssignedOperatorVehicleId` ON `containers` (`AssignedOperatorVehicleId`);",
                cancellationToken);
        }

        if (!await SchemaIntrospectionHelper.ForeignKeyExistsAsync(connection, "containers", "FK_containers_operator_vehicles_AssignedOperatorVehicleId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE `containers`
                  ADD CONSTRAINT `FK_containers_operator_vehicles_AssignedOperatorVehicleId`
                  FOREIGN KEY (`AssignedOperatorVehicleId`) REFERENCES `operator_vehicles` (`Id`) ON DELETE SET NULL;
                """,
                cancellationToken);
        }
    }
}
