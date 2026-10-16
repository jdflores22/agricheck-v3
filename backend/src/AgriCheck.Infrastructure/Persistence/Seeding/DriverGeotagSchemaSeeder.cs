using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

/// <summary>
/// Ensures incident reports can store multiple geotag photos per container (non-unique photo type index).
/// </summary>
public static class DriverGeotagSchemaSeeder
{
    public static Task EnsureAsync(AgriCheckDbContext db, ILogger logger, CancellationToken cancellationToken = default) =>
        ContainerInspectionPhotoIndexHelper.EnsureNonUniquePhotoTypeIndexAsync(db, logger, cancellationToken);
}
