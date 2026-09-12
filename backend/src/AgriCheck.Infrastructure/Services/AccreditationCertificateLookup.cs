using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

internal static class AccreditationCertificateLookup
{
    public static async Task<Certificate?> FindForSubmissionAsync(
        AgriCheckDbContext db,
        Guid submissionUuid,
        long? userId = null,
        CancellationToken cancellationToken = default)
    {
        var submissionToken = submissionUuid.ToString();
        var query = db.Certificates
            .Where(c =>
                c.Status == CertificateStatus.Active &&
                c.SummaryJson != null &&
                c.SummaryJson.Contains(submissionToken));

        if (userId.HasValue)
        {
            query = query.Where(c => c.UserId == userId.Value);
        }

        return await query
            .OrderByDescending(c => c.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
