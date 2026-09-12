using AgriCheck.Application.ClientPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

internal static class UserContextHelper
{
    public static async Task<User> RequireUserAsync(AgriCheckDbContext db, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        if (currentUser.UserUuid is null)
        {
            throw new ClientPortalException("UNAUTHORIZED", "Authentication required.");
        }

        return await db.Users.FirstOrDefaultAsync(u => u.Uuid == currentUser.UserUuid, cancellationToken)
            ?? throw new ClientPortalException("USER_NOT_FOUND", "User not found.");
    }

    public static async Task RequireAccreditedAsync(AgriCheckDbContext db, long userId, CancellationToken cancellationToken)
    {
        var latest = await db.AccreditationSubmissions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest?.Status != AccreditationSubmissionStatus.Approved)
        {
            throw new ClientPortalException(
                "ACCREDITATION_REQUIRED",
                "Accreditation is required before you can submit entries to DA agencies.");
        }
    }
}

public class ClientPortalException : Exception
{
    public string Code { get; }

    public ClientPortalException(string code, string message) : base(message) => Code = code;
}
