using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class InspectorAssignmentsSchemaSeeder
{
    public static async Task EnsureAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await TableExistsAsync(db, "inspector_assignments", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE `inspector_assignments` (
                  `Id` bigint NOT NULL AUTO_INCREMENT,
                  `Uuid` char(36) COLLATE ascii_general_ci NOT NULL,
                  `ContainerId` bigint NOT NULL,
                  `AgencyId` bigint NOT NULL,
                  `InspectorUserId` bigint NOT NULL,
                  `Status` varchar(32) NOT NULL,
                  `AssignedAt` datetime(6) NOT NULL,
                  `CompletedAt` datetime(6) NULL,
                  `CreatedAt` datetime(6) NOT NULL,
                  `UpdatedAt` datetime(6) NOT NULL,
                  PRIMARY KEY (`Id`),
                  UNIQUE KEY `IX_inspector_assignments_Uuid` (`Uuid`),
                  KEY `IX_inspector_assignments_AgencyId` (`AgencyId`),
                  KEY `IX_inspector_assignments_InspectorUserId` (`InspectorUserId`),
                  KEY `IX_inspector_assignments_ContainerId_InspectorUserId_Status` (`ContainerId`, `InspectorUserId`, `Status`),
                  CONSTRAINT `FK_inspector_assignments_agencies_AgencyId`
                    FOREIGN KEY (`AgencyId`) REFERENCES `agencies` (`Id`) ON DELETE CASCADE,
                  CONSTRAINT `FK_inspector_assignments_containers_ContainerId`
                    FOREIGN KEY (`ContainerId`) REFERENCES `containers` (`Id`) ON DELETE CASCADE,
                  CONSTRAINT `FK_inspector_assignments_users_InspectorUserId`
                    FOREIGN KEY (`InspectorUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """,
                cancellationToken);
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            SELECT '20260913040000_InspectorAssignments', '8.0.0'
            WHERE NOT EXISTS (
              SELECT 1 FROM `__EFMigrationsHistory`
              WHERE `MigrationId` = '20260913040000_InspectorAssignments'
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
}
