using AgriCheck.Application.ClientPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;

namespace AgriCheck.Infrastructure.Services;

internal static class MavContextHelper
{
    private static readonly string[] ImporterRoles =
    {
        "ROLE_MAV_IMPORTER",
        "ROLE_IMPORTER",
        "ROLE_EXPORTER",
        "ROLE_BROKER",
        "ROLE_MAV_ADMIN",
        "ROLE_ADMIN"
    };

    public static async Task<User> RequireImporterAsync(AgriCheckDbContext db, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);
        if (!ImporterRoles.Any(currentUser.IsInRole))
        {
            throw new ClientPortalException("FORBIDDEN", "MAV importer access required.");
        }

        return user;
    }

    public static async Task<User> RequireMavStaffAsync(AgriCheckDbContext db, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);
        if (!currentUser.IsInRole("ROLE_MAV_ADMIN") && !currentUser.IsInRole("ROLE_MAV_EVALUATOR")
            && !currentUser.IsInRole("ROLE_MAV_SECRETARY") && !currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "MAV staff access required.");
        }

        return user;
    }

    public static async Task<User> RequireMavAdminAsync(AgriCheckDbContext db, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);
        if (!currentUser.IsInRole("ROLE_MAV_ADMIN") && !currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "MAV admin access required.");
        }

        return user;
    }
}

internal static class MavAuditHelper
{
    public static void Write(AgriCheckDbContext db, long? userId, string entityType, string entityId, string action, string? description = null, object? payload = null)
    {
        db.MavAuditLogs.Add(new MavAuditLog
        {
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Description = description,
            PayloadJson = payload is null ? null : System.Text.Json.JsonSerializer.Serialize(payload)
        });
    }
}
