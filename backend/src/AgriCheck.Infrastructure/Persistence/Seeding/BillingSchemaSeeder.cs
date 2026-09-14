using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class BillingSchemaSeeder
{
    public static async Task EnsureAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();

        if (!await SchemaIntrospectionHelper.TableExistsAsync(connection, "agency_payment_settings", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE `agency_payment_settings` (
                  `Id` bigint NOT NULL AUTO_INCREMENT,
                  `AgencyId` bigint NOT NULL,
                  `PayMongoEnabled` tinyint(1) NOT NULL,
                  `PayMongoApiKey` varchar(255) NULL,
                  `PayMongoWebhookSecret` varchar(255) NULL,
                  `PayMongoPublicKey` varchar(255) NULL,
                  `CashPaymentEnabled` tinyint(1) NOT NULL DEFAULT 1,
                  `CashPaymentInstructions` varchar(2000) NULL,
                  `CreatedAt` datetime(6) NOT NULL,
                  `UpdatedAt` datetime(6) NOT NULL,
                  PRIMARY KEY (`Id`),
                  UNIQUE KEY `IX_agency_payment_settings_AgencyId` (`AgencyId`),
                  CONSTRAINT `FK_agency_payment_settings_agencies_AgencyId`
                    FOREIGN KEY (`AgencyId`) REFERENCES `agencies` (`Id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """,
                cancellationToken);
        }

        if (!await SchemaIntrospectionHelper.TableExistsAsync(connection, "billing_charges", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE `billing_charges` (
                  `Id` bigint NOT NULL AUTO_INCREMENT,
                  `Uuid` char(36) COLLATE ascii_general_ci NOT NULL,
                  `AgencyBillingId` bigint NOT NULL,
                  `Description` varchar(256) NOT NULL,
                  `Amount` decimal(12,2) NOT NULL,
                  `SortOrder` int NOT NULL,
                  `CreatedAt` datetime(6) NOT NULL,
                  `UpdatedAt` datetime(6) NOT NULL,
                  PRIMARY KEY (`Id`),
                  UNIQUE KEY `IX_billing_charges_Uuid` (`Uuid`),
                  KEY `IX_billing_charges_AgencyBillingId` (`AgencyBillingId`),
                  CONSTRAINT `FK_billing_charges_billings_AgencyBillingId`
                    FOREIGN KEY (`AgencyBillingId`) REFERENCES `billings` (`Id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """,
                cancellationToken);
        }

        if (!await SchemaIntrospectionHelper.ColumnExistsAsync(connection, "client_bills", "AgencyBillingId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE `client_bills` ADD `AgencyBillingId` bigint NULL;",
                cancellationToken);
        }

        if (!await SchemaIntrospectionHelper.IndexExistsAsync(connection, "client_bills", "IX_client_bills_AgencyBillingId", cancellationToken)
            && await SchemaIntrospectionHelper.ColumnExistsAsync(connection, "client_bills", "AgencyBillingId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX `IX_client_bills_AgencyBillingId` ON `client_bills` (`AgencyBillingId`);",
                cancellationToken);
        }

        if (!await SchemaIntrospectionHelper.ForeignKeyExistsAsync(connection, "client_bills", "FK_client_bills_billings_AgencyBillingId", cancellationToken)
            && await SchemaIntrospectionHelper.ColumnExistsAsync(connection, "client_bills", "AgencyBillingId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE `client_bills`
                  ADD CONSTRAINT `FK_client_bills_billings_AgencyBillingId`
                  FOREIGN KEY (`AgencyBillingId`) REFERENCES `billings` (`Id`) ON DELETE SET NULL;
                """,
                cancellationToken);
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            SELECT '20260913010000_AgencyPaymentSettings', '8.0.0'
            WHERE NOT EXISTS (
              SELECT 1 FROM `__EFMigrationsHistory`
              WHERE `MigrationId` = '20260913010000_AgencyPaymentSettings'
            );
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            SELECT '20260913020000_AgencyBillingCharges', '8.0.0'
            WHERE NOT EXISTS (
              SELECT 1 FROM `__EFMigrationsHistory`
              WHERE `MigrationId` = '20260913020000_AgencyBillingCharges'
            );
            """,
            cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            SELECT '20260913030000_ClientBillAgencyBillingLink', '8.0.0'
            WHERE NOT EXISTS (
              SELECT 1 FROM `__EFMigrationsHistory`
              WHERE `MigrationId` = '20260913030000_ClientBillAgencyBillingLink'
            );
            """,
            cancellationToken);
    }
}
