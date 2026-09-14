using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// Ensures production Hostinger databases match the full v3 schema (83 application tables).
/// </summary>
public static class ProductionSchemaSeeder
{
    public static async Task EnsureAsync(
        AgriCheckDbContext db,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        await DriverRegistrationSchemaSeeder.EnsureAsync(db, cancellationToken);
        logger?.LogInformation("Driver registration schema ensured.");

        await EntrySchemaSeeder.EnsureAsync(db, cancellationToken);
        logger?.LogInformation("Entry schema ensured.");

        await BillingSchemaSeeder.EnsureAsync(db, cancellationToken);
        logger?.LogInformation("Billing schema ensured.");

        await InspectorAssignmentsSchemaSeeder.EnsureAsync(db, cancellationToken);
        logger?.LogInformation("Inspector assignments schema ensured.");

        await WarehouseProfilingSchemaSeeder.EnsureAsync(db, cancellationToken);
        logger?.LogInformation("Warehouse profiling schema ensured.");
    }

    public static async Task<int> CountApplicationTablesAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        return await SchemaIntrospectionHelper.CountApplicationTablesAsync(connection, cancellationToken);
    }
}
