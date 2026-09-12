using AgriCheck.Application.ClientPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

internal static class DaContextHelper
{
    public static async Task<User> RequireDaLeadershipAsync(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(db, currentUser, cancellationToken);

        if (currentUser.IsInRole("ROLE_ADMIN") ||
            currentUser.IsInRole("ROLE_DA_SECRETARY") ||
            currentUser.IsInRole("ROLE_DA_UNDERSECRETARY"))
        {
            return user;
        }

        throw new ClientPortalException(
            "DA_ACCESS_DENIED",
            "DA leadership access is required.");
    }
}
