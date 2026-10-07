using AgriCheck.Application.Compliance;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class SpsIssuingRuleService : ISpsIssuingRuleService
{
    private readonly AgriCheckDbContext _db;

    public SpsIssuingRuleService(AgriCheckDbContext db) => _db = db;

    public async Task<SpsIssuingAgencyMatchDto?> ResolveIssuerAsync(
        string? hsCode,
        long? fallbackAgencyId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeHs(hsCode);
        if (normalized.Length > 0)
        {
            var rules = await _db.SpsIssuingRules
                .AsNoTracking()
                .Include(r => r.IssuingAgency)
                .Where(r => normalized.StartsWith(r.HsCodePrefix))
                .ToListAsync(cancellationToken);

            var best = rules
                .OrderByDescending(r => r.HsCodePrefix.Length)
                .ThenByDescending(r => r.IsPrimaryIssuer)
                .FirstOrDefault();

            if (best?.IssuingAgency is not null)
            {
                return new SpsIssuingAgencyMatchDto(
                    best.IssuingAgencyId,
                    best.IssuingAgency.Code,
                    best.IssuingAgency.Name,
                    best.HsCodePrefix,
                    false);
            }

            var mavCategory = await _db.MavHsCategories
                .AsNoTracking()
                .Include(c => c.Agency)
                .Where(c => c.IsActive && c.AgencyId != null && normalized.StartsWith(NormalizeHs(c.HsCode)))
                .OrderByDescending(c => c.HsCode.Length)
                .FirstOrDefaultAsync(cancellationToken);

            if (mavCategory?.Agency is not null)
            {
                return new SpsIssuingAgencyMatchDto(
                    mavCategory.AgencyId!.Value,
                    mavCategory.Agency.Code,
                    mavCategory.Agency.Name,
                    NormalizeHs(mavCategory.HsCode),
                    false);
            }
        }

        if (fallbackAgencyId is null)
        {
            return null;
        }

        var agency = await _db.Agencies.AsNoTracking().FirstOrDefaultAsync(a => a.Id == fallbackAgencyId, cancellationToken);
        return agency is null
            ? null
            : new SpsIssuingAgencyMatchDto(agency.Id, agency.Code, agency.Name, string.Empty, true);
    }

    private static string NormalizeHs(string? hsCode) =>
        string.IsNullOrWhiteSpace(hsCode) ? string.Empty : hsCode.Trim().Replace(".", "", StringComparison.Ordinal);
}
