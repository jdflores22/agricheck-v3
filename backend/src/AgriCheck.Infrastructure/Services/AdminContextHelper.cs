using AgriCheck.Application.ClientPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;

namespace AgriCheck.Infrastructure.Services;

internal static class AuditLogHelper
{
    public static void Write(AgriCheckDbContext db, long? actorUserId, string action, string entityType, string? entityId, object? payload = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = payload is null ? null : System.Text.Json.JsonSerializer.Serialize(payload)
        });
    }
}

internal static class AdminContextHelper
{
    public static async Task<User> RequireAdminAsync(AgriCheckDbContext db, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);
        if (!currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "Admin access required.");
        }

        return user;
    }
}
