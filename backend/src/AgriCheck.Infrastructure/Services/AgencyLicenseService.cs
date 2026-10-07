using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class AgencyLicenseService : IAgencyLicenseService
{
    private static readonly AgencyLicenseKind[] SpsicPrerequisiteKinds =
    {
        AgencyLicenseKind.Lto,
        AgencyLicenseKind.Lti,
    };

    private readonly AgriCheckDbContext _db;

    public AgencyLicenseService(AgriCheckDbContext db) => _db = db;

    public async Task<bool> HasActiveSpsicPrerequisiteLicenseAsync(long userId, long agencyId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.AgencyLicenses
            .AsNoTracking()
            .Include(l => l.LicenseType)
            .AnyAsync(
                l => l.UserId == userId
                    && l.AgencyId == agencyId
                    && l.Status == AgencyLicenseStatus.Active
                    && l.ExpiresAt > now
                    && SpsicPrerequisiteKinds.Contains(l.LicenseType.Kind),
                cancellationToken);
    }

    public async Task<AgencyLicenseSummaryDto?> GetPrimaryActiveLicenseAsync(long userId, long agencyId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var license = await _db.AgencyLicenses
            .AsNoTracking()
            .Include(l => l.LicenseType)
            .Where(l => l.UserId == userId
                && l.AgencyId == agencyId
                && l.Status == AgencyLicenseStatus.Active
                && l.ExpiresAt > now
                && SpsicPrerequisiteKinds.Contains(l.LicenseType.Kind))
            .OrderByDescending(l => l.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (license is null)
        {
            return null;
        }

        return new AgencyLicenseSummaryDto(
            license.Uuid,
            license.LicenseNumber,
            license.LicenseType.Code,
            license.LicenseType.Name,
            license.ExpiresAt);
    }

    public async Task<bool> MeetsCommodityLicenseRulesAsync(
        long userId,
        long agencyId,
        long? commodityId,
        string? hsCode,
        CancellationToken cancellationToken = default)
    {
        var rules = await _db.CommodityLicenseRules
            .AsNoTracking()
            .Where(r => r.AgencyId == agencyId && r.IsRequired)
            .ToListAsync(cancellationToken);

        if (rules.Count == 0)
        {
            return true;
        }

        var normalizedHs = NormalizeHs(hsCode);
        var applicable = rules.Where(r =>
                (r.CommodityId is null || r.CommodityId == commodityId)
                && (string.IsNullOrWhiteSpace(r.HsCodePrefix)
                    || (!string.IsNullOrEmpty(normalizedHs)
                        && normalizedHs.StartsWith(NormalizeHs(r.HsCodePrefix), StringComparison.Ordinal))))
            .ToList();

        if (applicable.Count == 0)
        {
            return true;
        }

        var now = DateTime.UtcNow;
        var activeTypeIds = await _db.AgencyLicenses
            .AsNoTracking()
            .Where(l => l.UserId == userId
                && l.AgencyId == agencyId
                && l.Status == AgencyLicenseStatus.Active
                && l.ExpiresAt > now)
            .Select(l => l.AgencyLicenseTypeId)
            .ToListAsync(cancellationToken);

        return applicable.All(rule => activeTypeIds.Contains(rule.AgencyLicenseTypeId));
    }

    private static string NormalizeHs(string? hsCode) =>
        string.IsNullOrWhiteSpace(hsCode) ? string.Empty : hsCode.Trim().Replace(".", "", StringComparison.Ordinal);
}
