using AgriCheck.Application.ClientPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

internal static class AccreditationContextHelper
{
    public static async Task<User> RequireAccreditationOfficerAsync(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);

        if (currentUser.IsInRole("ROLE_ADMIN") || currentUser.IsInRole("ROLE_ACCREDITATION_OFFICER"))
        {
            return user;
        }

        throw new ClientPortalException(
            "ACCREDITATION_ACCESS_DENIED",
            "DA accreditation officer access is required.");
    }
}
