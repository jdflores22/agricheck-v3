using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class AgencyNewsSchemaSeeder
{
    public static async Task EnsureAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();

        if (!await SchemaIntrospectionHelper.TableExistsAsync(connection, "agency_news_posts", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE `agency_news_posts` (
                  `Id` bigint NOT NULL AUTO_INCREMENT,
                  `Uuid` char(36) COLLATE ascii_general_ci NOT NULL,
                  `AgencyId` bigint NOT NULL,
                  `Title` varchar(500) NOT NULL,
                  `Summary` varchar(1000) NULL,
                  `Body` text NOT NULL,
                  `ImageUrl` varchar(1000) NULL,
                  `Category` varchar(50) NOT NULL,
                  `IsRegulatory` tinyint(1) NOT NULL,
                  `IsPublished` tinyint(1) NOT NULL,
                  `PublishedAt` datetime(6) NULL,
                  `DisplayOrder` int NOT NULL,
                  `CreatedAt` datetime(6) NOT NULL,
                  `UpdatedAt` datetime(6) NOT NULL,
                  PRIMARY KEY (`Id`),
                  UNIQUE KEY `IX_agency_news_posts_Uuid` (`Uuid`),
                  KEY `IX_agency_news_posts_AgencyId` (`AgencyId`),
                  KEY `IX_agency_news_posts_IsPublished_PublishedAt` (`IsPublished`, `PublishedAt`),
                  KEY `IX_agency_news_posts_DisplayOrder` (`DisplayOrder`),
                  CONSTRAINT `FK_agency_news_posts_agencies_AgencyId`
                    FOREIGN KEY (`AgencyId`) REFERENCES `agencies` (`Id`) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """,
                cancellationToken);
        }

        if (await SchemaIntrospectionHelper.TableExistsAsync(connection, "agency_news_posts", cancellationToken))
        {
            await MigrationHistoryBaselineHelper.EnsureRecordedAsync(db, "20261011230000_AgencyNewsPosts", cancellationToken);
        }
    }
}
