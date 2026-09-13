using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal static class AgencyBillingNotificationHelper
{
    private static readonly string[] BillingAgentRoles = { "ROLE_BILLING_AGENT", "ROLE_ACCOUNTANT" };

    public static async Task NotifyEntryApprovedForBillingAsync(
        AgriCheckDbContext db,
        INotificationService notifications,
        Entry entry,
        CancellationToken cancellationToken = default)
    {
        var billingAgentIds = await GetAgencyBillingAgentUserIdsAsync(db, entry.AgencyId, cancellationToken);
        foreach (var userId in billingAgentIds)
        {
            await notifications.NotifyAsync(
                userId,
                "billing_queue",
                "Entry approved — create billing",
                $"{entry.ReferenceNo} was approved and is ready for agency billing.",
                "Entry",
                entry.Uuid.ToString(),
                cancellationToken);
        }
    }

    private static async Task<IReadOnlyList<long>> GetAgencyBillingAgentUserIdsAsync(
        AgriCheckDbContext db,
        long agencyId,
        CancellationToken cancellationToken)
    {
        return await db.AgencyMemberships
            .Where(m => m.AgencyId == agencyId)
            .SelectMany(m => m.User.UserRoles)
            .Where(ur => BillingAgentRoles.Contains(ur.Role.Code))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
