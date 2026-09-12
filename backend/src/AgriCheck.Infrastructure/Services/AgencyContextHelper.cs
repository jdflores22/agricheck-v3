using AgriCheck.Application.ClientPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

internal static class AgencyContextHelper
{
    public static async Task<(User User, Agency Agency)> RequireAgencyStaffAsync(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);

        if (currentUser.IsInRole("ROLE_ADMIN"))
        {
            var defaultAgency = await db.Agencies.FirstAsync(a => a.IsActive, cancellationToken);
            return (user, defaultAgency);
        }

        var membership = await db.AgencyMemberships
            .Include(m => m.Agency)
            .FirstOrDefaultAsync(m => m.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("AGENCY_ACCESS_DENIED", "Agency membership required.");

        if (!membership.Agency.IsActive)
        {
            throw new ClientPortalException("AGENCY_INACTIVE", "Agency is inactive.");
        }

        return (user, membership.Agency);
    }

    public static async Task<Entry> RequireAgencyEntryAsync(
        AgriCheckDbContext db,
        Agency agency,
        Guid entryUuid,
        CancellationToken cancellationToken)
    {
        var entry = await db.Entries.FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found for your agency.");
        return entry;
    }
}
