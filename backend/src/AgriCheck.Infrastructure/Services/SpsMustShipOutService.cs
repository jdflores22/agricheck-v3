using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class SpsMustShipOutService : ISpsMustShipOutService
{
    private readonly AgriCheckDbContext _db;

    public SpsMustShipOutService(AgriCheckDbContext db) => _db = db;

    public DateTime ComputeMustShipOutBy(DateTime issuedAt, string? hsCode, string? commodityName) =>
        issuedAt.ToUniversalTime().AddDays(ResolveValidityDays(hsCode, commodityName));

    public async Task<int> GetValidityDaysAsync(string? hsCode, string? commodityName, CancellationToken cancellationToken = default)
    {
        var rules = await _db.SpsMustShipOutRules.AsNoTracking().ToListAsync(cancellationToken);
        if (rules.Count == 0)
        {
            return ResolveValidityDays(hsCode, commodityName);
        }

        var hs = NormalizeHs(hsCode);
        var name = (commodityName ?? string.Empty).ToLowerInvariant();
        var match = rules
            .Where(r => hs.StartsWith(NormalizeHs(r.HsCodePrefix), StringComparison.Ordinal))
            .OrderByDescending(r => r.HsCodePrefix.Length)
            .FirstOrDefault(r =>
                string.IsNullOrWhiteSpace(r.CommodityKeyword)
                || name.Contains(r.CommodityKeyword.Trim().ToLowerInvariant(), StringComparison.Ordinal));

        return match?.ValidityDays ?? ResolveValidityDays(hsCode, commodityName);
    }

    public async Task ExpireOverdueClearancesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var overdue = await _db.SpsImportClearances
            .Where(c => c.Status == SpsImportClearanceStatus.Issued
                && c.MustShipOutBy != null
                && c.MustShipOutBy < now)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0)
        {
            return;
        }

        foreach (var clearance in overdue)
        {
            clearance.Status = SpsImportClearanceStatus.Expired;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeHs(string? hsCode) =>
        string.IsNullOrWhiteSpace(hsCode) ? string.Empty : hsCode.Trim().Replace(".", "", StringComparison.Ordinal);

    internal static int ResolveValidityDays(string? hsCode, string? commodityName)
    {
        var hs = NormalizeHs(hsCode);
        var name = (commodityName ?? string.Empty).ToLowerInvariant();

        if (hs.StartsWith("03", StringComparison.Ordinal))
        {
            if (name.Contains("fresh", StringComparison.Ordinal) || name.Contains("chilled", StringComparison.Ordinal))
            {
                return 30;
            }

            return 45;
        }

        if (hs.StartsWith("07", StringComparison.Ordinal) || hs.StartsWith("08", StringComparison.Ordinal))
        {
            if (name.Contains("fresh", StringComparison.Ordinal) || name.Contains("chilled", StringComparison.Ordinal))
            {
                return 20;
            }
        }

        if (hs.StartsWith("31", StringComparison.Ordinal) || hs.StartsWith("38", StringComparison.Ordinal))
        {
            return 60;
        }

        if (hs.StartsWith("01", StringComparison.Ordinal))
        {
            return 60;
        }

        if (hs.StartsWith("02", StringComparison.Ordinal) || hs.StartsWith("04", StringComparison.Ordinal) || hs.StartsWith("05", StringComparison.Ordinal))
        {
            return 90;
        }

        if (hs.StartsWith("23", StringComparison.Ordinal))
        {
            return 60;
        }

        if (hs.StartsWith("06", StringComparison.Ordinal) || hs.StartsWith("12", StringComparison.Ordinal))
        {
            return 60;
        }

        return 60;
    }

}
