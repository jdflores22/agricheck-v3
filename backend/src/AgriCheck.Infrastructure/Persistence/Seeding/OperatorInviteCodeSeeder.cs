using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class OperatorInviteCodeSeeder
{
    public static async Task EnsureSeedDataAsync(
        AgriCheckDbContext db,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        await DriverRegistrationSchemaSeeder.EnsureAsync(db, cancellationToken);

        var operatorRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == "ROLE_OPERATOR", cancellationToken);
        if (operatorRole is null)
        {
            return;
        }

        var operatorUserIds = await db.UserRoles
            .Where(ur => ur.RoleId == operatorRole.Id)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var operatorUserId in operatorUserIds)
        {
            if (await db.OperatorInviteCodes.AnyAsync(i => i.OperatorUserId == operatorUserId, cancellationToken))
            {
                continue;
            }

            var isDemoOperator = await db.Users.AnyAsync(
                u => u.Id == operatorUserId && u.Email == "operator@agricheck.local",
                cancellationToken);

            db.OperatorInviteCodes.Add(new OperatorInviteCode
            {
                Code = isDemoOperator ? "AGRITRACK-DEMO" : $"AT-{operatorUserId:D4}",
                OperatorUserId = operatorUserId,
                Label = isDemoOperator ? "AgriTrack demo fleet" : "Driver registration invite",
                MaxUses = 0,
                IsActive = true,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Operator invite codes ensured for {Count} operators.", operatorUserIds.Count);
    }
}
