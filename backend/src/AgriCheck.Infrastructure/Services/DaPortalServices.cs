using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Application.DaPortal;
using AgriCheck.Application.DaPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class DaDashboardService : IDaDashboardService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DaDashboardService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DaDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        return new DaDashboardDto(
            await _db.Agencies.CountAsync(a => a.IsActive, cancellationToken),
            await _db.Agencies.CountAsync(cancellationToken),
            await _db.Entries.CountAsync(cancellationToken),
            await _db.Entries.CountAsync(
                e => e.Status == EntryStatus.Submitted || e.Status == EntryStatus.UnderReview,
                cancellationToken),
            await _db.Entries.CountAsync(e => e.Status == EntryStatus.Approved, cancellationToken),
            await _db.AccreditationSubmissions.CountAsync(
                s => s.Status == AccreditationSubmissionStatus.Submitted ||
                     s.Status == AccreditationSubmissionStatus.UnderReview,
                cancellationToken),
            await _db.AgencyBillings.CountAsync(
                b => b.Status != AgencyBillingStatus.Paid && b.Status != AgencyBillingStatus.Cancelled,
                cancellationToken),
            await _db.Inspections.CountAsync(i => i.Status == InspectionStatus.Completed, cancellationToken));
    }
}

public class DaAgencyOverviewService : IDaAgencyOverviewService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DaAgencyOverviewService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DaAgencySummaryDto>> ListAgenciesAsync(CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var agencies = await _db.Agencies
            .AsNoTracking()
            .OrderBy(a => a.Code == "DA" ? 0 : 1)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);

        var leadershipUsers = await LoadLeadershipUsersAsync(cancellationToken);
        var entryStats = await LoadEntryStatsAsync(agencies.Select(a => a.Id), cancellationToken);
        var openBillingStats = await LoadOpenBillingStatsAsync(agencies.Select(a => a.Id), cancellationToken);

        return agencies
            .Select(agency => BuildAgencySummary(agency, entryStats, openBillingStats, leadershipUsers))
            .ToList();
    }

    public async Task<DaAgencyOversightDto> GetOversightAsync(long id, CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var agency = await _db.Agencies
            .AsNoTracking()
            .Include(a => a.Parent)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Agency not found.");

        var isParentAgency = string.Equals(agency.Code, "DA", StringComparison.OrdinalIgnoreCase);
        var scopedAgencies = isParentAgency
            ? await _db.Agencies.AsNoTracking().Where(a => a.ParentId == agency.Id).OrderBy(a => a.Name).ToListAsync(cancellationToken)
            : new List<Agency> { agency };
        var scopedAgencyIds = scopedAgencies.Select(a => a.Id).ToList();

        if (!isParentAgency)
        {
            scopedAgencyIds = new List<long> { agency.Id };
        }

        var leadershipUsers = await LoadLeadershipUsersAsync(cancellationToken);
        var entryStats = await LoadEntryStatsAsync(scopedAgencyIds, cancellationToken);
        var openBillingStats = await LoadOpenBillingStatsAsync(scopedAgencyIds, cancellationToken);

        var report = await BuildScopedReportAsync(scopedAgencyIds, cancellationToken);

        var openBillings = await _db.AgencyBillings.CountAsync(
            b => scopedAgencyIds.Contains(b.AgencyId) &&
                 b.Status != AgencyBillingStatus.Paid &&
                 b.Status != AgencyBillingStatus.Cancelled,
            cancellationToken);

        var agencySummary = BuildAgencySummary(agency, entryStats, openBillingStats, leadershipUsers);
        if (isParentAgency)
        {
            agencySummary = agencySummary with
            {
                TotalEntries = report.TotalEntries,
                PendingEntries = report.SubmittedEntries + report.UnderReviewEntries,
                OpenBillings = openBillings,
            };
        }

        DaAgencySummaryDto? parentSummary = null;
        if (agency.Parent is not null)
        {
            var parentEntryStats = await LoadEntryStatsAsync(new[] { agency.Parent.Id }, cancellationToken);
            var parentBillingStats = await LoadOpenBillingStatsAsync(new[] { agency.Parent.Id }, cancellationToken);
            parentSummary = BuildAgencySummary(agency.Parent, parentEntryStats, parentBillingStats, leadershipUsers);
        }

        IReadOnlyList<DaAgencySummaryDto> childSummaries = isParentAgency
            ? scopedAgencies
                .Select(child =>
                {
                    entryStats.TryGetValue(child.Id, out var childEntries);
                    openBillingStats.TryGetValue(child.Id, out var childBillings);
                    return BuildAgencySummary(child, entryStats, openBillingStats, leadershipUsers);
                })
                .ToList()
            : Array.Empty<DaAgencySummaryDto>();

        var activeUsers = await _db.AgencyMemberships
            .CountAsync(m => scopedAgencyIds.Contains(m.AgencyId), cancellationToken);

        var recentEntries = await _db.Entries
            .AsNoTracking()
            .Include(e => e.User).ThenInclude(u => u.Profile)
            .Where(e => scopedAgencyIds.Contains(e.AgencyId))
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .Select(e => new DaAgencyRecentEntryDto(
                e.Uuid,
                e.ReferenceNo,
                e.Status.ToString(),
                e.CreatedAt,
                e.User.Profile != null ? (e.User.Profile.FirstName + " " + e.User.Profile.LastName).Trim() : e.User.Email))
            .ToListAsync(cancellationToken);

        return new DaAgencyOversightDto(
            agencySummary,
            report,
            isParentAgency,
            parentSummary,
            childSummaries,
            activeUsers,
            openBillings,
            recentEntries);
    }

    private async Task<List<User>> LoadLeadershipUsersAsync(CancellationToken cancellationToken) =>
        await _db.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.AgencyMemberships)
            .Where(u =>
                u.Status == UserStatus.Active &&
                u.UserRoles.Any(ur =>
                    ur.Role.Code == "ROLE_SECRETARY" ||
                    ur.Role.Code == "ROLE_UNDERSECRETARY" ||
                    ur.Role.Code == "ROLE_DA_SECRETARY" ||
                    ur.Role.Code == "ROLE_DA_UNDERSECRETARY"))
            .ToListAsync(cancellationToken);

    private async Task<Dictionary<long, (int TotalEntries, int PendingEntries)>> LoadEntryStatsAsync(
        IEnumerable<long> agencyIds,
        CancellationToken cancellationToken)
    {
        var ids = agencyIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, (int TotalEntries, int PendingEntries)>();
        }

        return await _db.Entries
            .Where(e => ids.Contains(e.AgencyId))
            .GroupBy(e => e.AgencyId)
            .Select(g => new
            {
                AgencyId = g.Key,
                TotalEntries = g.Count(),
                PendingEntries = g.Count(e =>
                    e.Status == EntryStatus.Submitted || e.Status == EntryStatus.UnderReview),
            })
            .ToDictionaryAsync(x => x.AgencyId, x => (x.TotalEntries, x.PendingEntries), cancellationToken);
    }

    private async Task<Dictionary<long, int>> LoadOpenBillingStatsAsync(
        IEnumerable<long> agencyIds,
        CancellationToken cancellationToken)
    {
        var ids = agencyIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, int>();
        }

        return await _db.AgencyBillings
            .Where(b => ids.Contains(b.AgencyId) &&
                        b.Status != AgencyBillingStatus.Paid &&
                        b.Status != AgencyBillingStatus.Cancelled)
            .GroupBy(b => b.AgencyId)
            .Select(g => new { AgencyId = g.Key, OpenBillings = g.Count() })
            .ToDictionaryAsync(x => x.AgencyId, x => x.OpenBillings, cancellationToken);
    }

    private async Task<DaOversightReportDto> BuildScopedReportAsync(
        IReadOnlyList<long> agencyIds,
        CancellationToken cancellationToken)
    {
        if (agencyIds.Count == 0)
        {
            return new DaOversightReportDto(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var entries = _db.Entries.Where(e => agencyIds.Contains(e.AgencyId));
        return new DaOversightReportDto(
            await entries.CountAsync(cancellationToken),
            await entries.CountAsync(e => e.Status == EntryStatus.Submitted, cancellationToken),
            await entries.CountAsync(e => e.Status == EntryStatus.UnderReview, cancellationToken),
            await entries.CountAsync(e => e.Status == EntryStatus.Approved, cancellationToken),
            await entries.CountAsync(e => e.Status == EntryStatus.Rejected, cancellationToken),
            await _db.Inspections.CountAsync(
                i => agencyIds.Contains(i.AgencyId) && i.Status == InspectionStatus.Completed,
                cancellationToken),
            await _db.AccreditationSubmissions.CountAsync(
                s => s.Status == AccreditationSubmissionStatus.Submitted ||
                     s.Status == AccreditationSubmissionStatus.UnderReview,
                cancellationToken),
            await _db.AgencyBillings.CountAsync(
                b => agencyIds.Contains(b.AgencyId) && b.Status == AgencyBillingStatus.Issued,
                cancellationToken),
            await _db.AgencyBillings.CountAsync(
                b => agencyIds.Contains(b.AgencyId) && b.Status == AgencyBillingStatus.Paid,
                cancellationToken));
    }

    private static DaAgencySummaryDto BuildAgencySummary(
        Agency agency,
        IReadOnlyDictionary<long, (int TotalEntries, int PendingEntries)> entryStats,
        IReadOnlyDictionary<long, int> openBillingStats,
        IReadOnlyList<User> leadershipUsers)
    {
        entryStats.TryGetValue(agency.Id, out var entries);
        openBillingStats.TryGetValue(agency.Id, out var openBillings);
        var (secretary, undersecretaries) = ResolveAgencyLeadership(agency, leadershipUsers);

        return new DaAgencySummaryDto(
            agency.Id,
            agency.Code,
            agency.Name,
            agency.IsActive,
            agency.ParentId,
            agency.LogoUrl,
            entries.TotalEntries,
            entries.PendingEntries,
            0,
            openBillings,
            secretary,
            undersecretaries);
    }

    private static (DaAgencyLeadershipUserDto? Secretary, IReadOnlyList<DaAgencyLeadershipUserDto> Undersecretaries)
        ResolveAgencyLeadership(Agency agency, IReadOnlyList<User> leadershipUsers)
    {
        if (string.Equals(agency.Code, "DA", StringComparison.OrdinalIgnoreCase))
        {
            var secretary = leadershipUsers
                .FirstOrDefault(u => u.UserRoles.Any(ur => ur.Role.Code == "ROLE_DA_SECRETARY"));
            var undersecretaries = leadershipUsers
                .Where(u => u.UserRoles.Any(ur => ur.Role.Code == "ROLE_DA_UNDERSECRETARY"))
                .Select(MapLeadershipUser)
                .OrderBy(u => u.FullName)
                .ToList();

            return (secretary is null ? null : MapLeadershipUser(secretary), undersecretaries);
        }

        var agencySecretary = leadershipUsers
            .FirstOrDefault(u =>
                u.AgencyMemberships.Any(m => m.AgencyId == agency.Id) &&
                u.UserRoles.Any(ur => ur.Role.Code == "ROLE_SECRETARY"));
        var agencyUndersecretaries = leadershipUsers
            .Where(u =>
                u.AgencyMemberships.Any(m => m.AgencyId == agency.Id) &&
                u.UserRoles.Any(ur => ur.Role.Code == "ROLE_UNDERSECRETARY"))
            .Select(MapLeadershipUser)
            .OrderBy(u => u.FullName)
            .ToList();

        return (agencySecretary is null ? null : MapLeadershipUser(agencySecretary), agencyUndersecretaries);
    }

    private static DaAgencyLeadershipUserDto MapLeadershipUser(User user) =>
        new(
            user.Uuid,
            user.Profile is not null ? $"{user.Profile.FirstName} {user.Profile.LastName}".Trim() : user.Email,
            user.Email);
}

public class DaOversightReportService : IDaOversightReportService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DaOversightReportService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DaOversightReportDto> GetReportAsync(CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        return new DaOversightReportDto(
            await _db.Entries.CountAsync(cancellationToken),
            await _db.Entries.CountAsync(e => e.Status == EntryStatus.Submitted, cancellationToken),
            await _db.Entries.CountAsync(e => e.Status == EntryStatus.UnderReview, cancellationToken),
            await _db.Entries.CountAsync(e => e.Status == EntryStatus.Approved, cancellationToken),
            await _db.Entries.CountAsync(e => e.Status == EntryStatus.Rejected, cancellationToken),
            await _db.Inspections.CountAsync(i => i.Status == InspectionStatus.Completed, cancellationToken),
            await _db.AccreditationSubmissions.CountAsync(
                s => s.Status == AccreditationSubmissionStatus.Submitted ||
                     s.Status == AccreditationSubmissionStatus.UnderReview,
                cancellationToken),
            await _db.AgencyBillings.CountAsync(b => b.Status == AgencyBillingStatus.Issued, cancellationToken),
            await _db.AgencyBillings.CountAsync(b => b.Status == AgencyBillingStatus.Paid, cancellationToken));
    }

    public async Task<DaMavNationalReportDto> GetMavNationalReportAsync(int? mavYear, CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);
        var year = mavYear ?? DateTime.UtcNow.Year;
        var licenses = await _db.MavLicenses
            .AsNoTracking()
            .Include(l => l.Account)
            .Include(l => l.Application).ThenInclude(a => a.ApplicationPeriod).ThenInclude(p => p.Agency)
            .Where(l => l.MavYear == year)
            .ToListAsync(cancellationToken);
        var apps = _db.MavApplications.Where(a => a.ApplicationPeriod.MavYear == year);
        var appCounts = await apps
            .GroupBy(a => a.HsCode)
            .Select(g => new { HsCode = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var categories = await _db.MavHsCategories.AsNoTracking().Include(c => c.Agency).Where(c => c.IsActive).ToListAsync(cancellationToken);

        string ResolveAgency(string hsCode, long? periodAgencyId, string? periodAgencyCode)
        {
            if (!string.IsNullOrWhiteSpace(periodAgencyCode)) return periodAgencyCode;
            var match = categories.FirstOrDefault(c => hsCode.StartsWith(c.HsCode, StringComparison.OrdinalIgnoreCase));
            return match?.Agency?.Code ?? "Shared";
        }

        var mavAgencyIds = await _db.MavApplicationPeriods
            .Where(p => p.MavYear == year && p.AgencyId != null)
            .Select(p => p.AgencyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        mavAgencyIds.AddRange(categories.Where(c => c.AgencyId.HasValue).Select(c => c.AgencyId!.Value));
        var mavAgencySet = mavAgencyIds.ToHashSet();

        var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);
        var importEntries = await _db.Entries
            .AsNoTracking()
            .Include(e => e.Detail)
            .Include(e => e.Agency)
            .Include(e => e.MicUtilizations)
            .Where(e =>
                e.EntryType == EntryType.Import
                && e.Status != EntryStatus.Draft
                && e.Status != EntryStatus.Rejected
                && e.Status != EntryStatus.Cancelled
                && ((e.SubmittedAt ?? e.CreatedAt) >= yearStart && (e.SubmittedAt ?? e.CreatedAt) < yearEnd)
                && (e.ImportTrack == EntryImportTrack.Mav || mavAgencySet.Contains(e.AgencyId)))
            .ToListAsync(cancellationToken);

        var regularEntries = importEntries
            .Where(e => e.ImportTrack == EntryImportTrack.Regular)
            .Select(e => new
            {
                AgencyCode = e.Agency?.Code ?? "Shared",
                HsCode = ResolveEntryHsCode(e, categories),
                CommodityName = FirstNonEmpty(e.Detail?.CommodityName, "Unclassified") ?? "Unclassified",
                VolumeMt = DaStockVolumeHelper.ToKilograms(e) / 1000m,
            })
            .ToList();

        var regularByAgency = regularEntries
            .GroupBy(e => Norm(e.AgencyCode))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.VolumeMt));
        var regularByCommodity = regularEntries
            .GroupBy(e => (Norm(e.AgencyCode), Norm(e.HsCode), Norm(e.CommodityName)))
            .ToDictionary(
                g => g.Key,
                g => (
                    AgencyCode: g.First().AgencyCode,
                    HsCode: g.First().HsCode,
                    CommodityName: g.First().CommodityName,
                    Volume: g.Sum(x => x.VolumeMt)));

        var byAgency = licenses
            .GroupBy(l => ResolveAgency(l.HsCode, l.Application?.ApplicationPeriod.AgencyId, l.Application?.ApplicationPeriod.Agency?.Code))
            .Select(g =>
            {
                var utilizedVolume = g.Sum(l => l.Account?.UtilizedVolume ?? 0m);
                var regularVolume = regularByAgency.GetValueOrDefault(Norm(g.Key));
                return new DaMavAgencyRowDto(
                    g.Key,
                    g.Count(l => l.Status == MavLicenseStatus.Active),
                    g.Sum(l => l.AwardedVolume),
                    utilizedVolume,
                    g.Sum(l => l.AwardedVolume - (l.Account?.UtilizedVolume ?? 0m)),
                    regularVolume,
                    utilizedVolume + regularVolume);
            })
            .ToList();

        foreach (var (agencyKey, regularVolume) in regularByAgency.Where(pair => byAgency.All(row => Norm(row.AgencyCode) != pair.Key)))
        {
            var agencyCode = regularEntries.First(e => Norm(e.AgencyCode) == agencyKey).AgencyCode;
            byAgency.Add(new DaMavAgencyRowDto(agencyCode, 0, 0, 0, 0, regularVolume, regularVolume));
        }

        var byCommodity = licenses
            .GroupBy(l => new { l.HsCode, l.CommodityName, Agency = ResolveAgency(l.HsCode, l.Application?.ApplicationPeriod.AgencyId, l.Application?.ApplicationPeriod.Agency?.Code) })
            .Select(g =>
            {
                var utilizedVolume = g.Sum(l => l.Account?.UtilizedVolume ?? 0m);
                var regularVolume = regularByCommodity.GetValueOrDefault((Norm(g.Key.Agency), Norm(g.Key.HsCode), Norm(g.Key.CommodityName))).Volume;
                return new DaMavCommodityRowDto(
                    g.Key.HsCode,
                    g.Key.CommodityName,
                    g.Key.Agency,
                    appCounts.FirstOrDefault(a => string.Equals(a.HsCode, g.Key.HsCode, StringComparison.OrdinalIgnoreCase))?.Count ?? 0,
                    g.Count(l => l.Status == MavLicenseStatus.Active),
                    g.Sum(l => l.AwardedVolume),
                    utilizedVolume,
                    g.Sum(l => l.AwardedVolume - (l.Account?.UtilizedVolume ?? 0m)),
                    regularVolume,
                    utilizedVolume + regularVolume);
            })
            .ToList();

        foreach (var (key, value) in regularByCommodity.Where(pair =>
                     byCommodity.All(row =>
                         Norm(row.AgencyCode) != pair.Key.Item1
                         || Norm(row.HsCode) != pair.Key.Item2
                         || Norm(row.CommodityName) != pair.Key.Item3)))
        {
            byCommodity.Add(new DaMavCommodityRowDto(
                value.HsCode,
                value.CommodityName,
                value.AgencyCode,
                appCounts.FirstOrDefault(a => string.Equals(a.HsCode, value.HsCode, StringComparison.OrdinalIgnoreCase))?.Count ?? 0,
                0,
                0,
                0,
                0,
                value.Volume,
                value.Volume));
        }

        var awarded = licenses.Sum(l => l.AwardedVolume);
        var utilized = licenses.Sum(l => l.Account?.UtilizedVolume ?? 0m);
        var regularVolumeTotal = regularEntries.Sum(e => e.VolumeMt);

        return new DaMavNationalReportDto(
            year,
            await _db.MavApplicationPeriods.CountAsync(p => p.Status == MavApplicationPeriodStatus.Open, cancellationToken),
            await apps.CountAsync(cancellationToken),
            await apps.CountAsync(a => a.Status == MavApplicationStatus.Approved, cancellationToken),
            licenses.Count(l => l.Status == MavLicenseStatus.Active),
            await _db.MavImportCertificates.CountAsync(m => m.License.MavYear == year, cancellationToken),
            awarded,
            utilized,
            awarded - utilized,
            regularEntries.Count,
            regularVolumeTotal,
            utilized + regularVolumeTotal,
            byAgency.OrderBy(r => r.AgencyCode).ToList(),
            byCommodity.OrderBy(r => r.AgencyCode).ThenBy(r => r.HsCode).ToList());
    }

    private static string ResolveEntryHsCode(Entry entry, IReadOnlyCollection<MavHsCategory> categories)
    {
        var fromForm = ReadEntryHsCode(entry.FormDataJson);
        if (!string.IsNullOrWhiteSpace(fromForm))
        {
            return fromForm;
        }

        var commodityName = entry.Detail?.CommodityName;
        if (!string.IsNullOrWhiteSpace(commodityName))
        {
            var match = categories.FirstOrDefault(c =>
                c.Description.Contains(commodityName, StringComparison.OrdinalIgnoreCase)
                || commodityName.Contains(c.Description, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match.HsCode;
            }
        }

        return string.Empty;
    }

    private static string? ReadEntryHsCode(string? formDataJson)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return null;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(formDataJson);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != System.Text.Json.JsonValueKind.String)
                {
                    continue;
                }

                if (property.Name.Equals("hs_code", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Equals("hsCode", StringComparison.OrdinalIgnoreCase)
                    || property.Name.EndsWith("_hs_code", StringComparison.OrdinalIgnoreCase))
                {
                    var value = property.Value.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        return null;
    }

    public async Task<DaCommodityStockReportDto> GetCommodityStockReportAsync(
        DaCommodityStockQuery query,
        CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var categories = await _db.MavHsCategories.AsNoTracking().Where(c => c.IsActive).ToListAsync(cancellationToken);
        var stockLines = await LoadStoredStockLinesAsync(categories, cancellationToken);
        var hsFilter = query.HsCode?.Trim();
        var commodityFilter = query.CommodityName?.Trim();
        var agencyFilter = query.AgencyCode?.Trim();

        var filtered = stockLines
            .Where(l => MatchesCommodityFilter(l.HsCode, l.CommodityName, hsFilter, commodityFilter))
            .Where(l => string.IsNullOrWhiteSpace(agencyFilter)
                || string.Equals(l.AgencyCode, agencyFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var commodities = filtered
            .GroupBy(l => (Norm(l.HsCode), Norm(l.CommodityName)))
            .Select(g =>
            {
                var sample = g.First();
                var entryIds = g.Select(l => l.EntryId).Distinct().Count();
                return new DaCommodityStockRowDto(
                    string.IsNullOrWhiteSpace(sample.HsCode) ? "—" : sample.HsCode,
                    sample.CommodityName,
                    g.Sum(l => l.VolumeKg),
                    g.Where(l => l.ImportTrack == EntryImportTrack.Mav).Sum(l => l.VolumeKg),
                    g.Where(l => l.ImportTrack == EntryImportTrack.Regular).Sum(l => l.VolumeKg),
                    g.Count(),
                    g.Select(l => l.FacilityId).Distinct().Count(),
                    g.Select(l => l.AgencyCode).Distinct().Count(),
                    entryIds);
            })
            .OrderByDescending(r => r.StockKg)
            .ThenBy(r => r.CommodityName)
            .ToList();

        var byAgency = filtered
            .GroupBy(l => (l.AgencyCode, Norm(l.HsCode), Norm(l.CommodityName)))
            .Select(g =>
            {
                var sample = g.First();
                return new DaCommodityStockAgencyRowDto(
                    sample.AgencyCode,
                    string.IsNullOrWhiteSpace(sample.HsCode) ? "—" : sample.HsCode,
                    sample.CommodityName,
                    g.Sum(l => l.VolumeKg),
                    g.Where(l => l.ImportTrack == EntryImportTrack.Mav).Sum(l => l.VolumeKg),
                    g.Where(l => l.ImportTrack == EntryImportTrack.Regular).Sum(l => l.VolumeKg),
                    g.Count(),
                    g.Select(l => l.FacilityId).Distinct().Count());
            })
            .OrderByDescending(r => r.StockKg)
            .ThenBy(r => r.AgencyCode)
            .ThenBy(r => r.CommodityName)
            .ToList();

        return new DaCommodityStockReportDto(
            filtered.Sum(l => l.VolumeKg),
            filtered.Where(l => l.ImportTrack == EntryImportTrack.Mav).Sum(l => l.VolumeKg),
            filtered.Where(l => l.ImportTrack == EntryImportTrack.Regular).Sum(l => l.VolumeKg),
            commodities.Count,
            filtered.Count,
            filtered.Select(l => l.FacilityId).Distinct().Count(),
            commodities,
            byAgency);
    }

    public async Task<DaImportPipelineReportDto> GetImportPipelineReportAsync(
        DaImportPipelineQuery query,
        CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var categories = await _db.MavHsCategories.AsNoTracking().Where(c => c.IsActive).ToListAsync(cancellationToken);
        var actualLines = await LoadStoredStockLinesAsync(categories, cancellationToken);
        var expectedLines = await LoadExpectedPipelineLinesAsync(categories, cancellationToken);

        var hsFilter = query.HsCode?.Trim();
        var commodityFilter = query.CommodityName?.Trim();
        var agencyFilter = query.AgencyCode?.Trim();

        var filteredExpected = expectedLines
            .Where(l => MatchesCommodityFilter(l.HsCode, l.CommodityName, hsFilter, commodityFilter))
            .Where(l => string.IsNullOrWhiteSpace(agencyFilter)
                || string.Equals(l.AgencyCode, agencyFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var filteredActual = actualLines
            .Where(l => MatchesCommodityFilter(l.HsCode, l.CommodityName, hsFilter, commodityFilter))
            .Where(l => string.IsNullOrWhiteSpace(agencyFilter)
                || string.Equals(l.AgencyCode, agencyFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var byStage = new[] { "Processing", "InTransit", "AwaitingStorage" }
            .Select(stage =>
            {
                var stageLines = filteredExpected.Where(l => l.Stage == stage).ToList();
                return new DaImportPipelineStageRowDto(
                    stage,
                    PipelineStageLabel(stage),
                    stageLines.Sum(l => l.VolumeKg),
                    stageLines.Count(l => l.ContainerCount > 0),
                    stageLines.Select(l => l.EntryId).Distinct().Count());
            })
            .ToList();

        var commodityKeys = filteredExpected
            .Select(l => (Norm(l.HsCode), Norm(l.CommodityName)))
            .Concat(filteredActual.Select(l => (Norm(l.HsCode), Norm(l.CommodityName))))
            .Distinct()
            .ToList();

        var byCommodity = commodityKeys
            .Select(key =>
            {
                var expectedForKey = filteredExpected.Where(l => Norm(l.HsCode) == key.Item1 && Norm(l.CommodityName) == key.Item2).ToList();
                var actualForKey = filteredActual.Where(l => Norm(l.HsCode) == key.Item1 && Norm(l.CommodityName) == key.Item2).ToList();
                var hsCode = expectedForKey.FirstOrDefault()?.HsCode ?? actualForKey.First().HsCode;
                var commodityName = expectedForKey.FirstOrDefault()?.CommodityName ?? actualForKey.First().CommodityName;
                var importerCount = expectedForKey.Select(l => l.ImporterUuid)
                    .Concat(actualForKey.Select(l => l.ImporterUuid))
                    .Distinct()
                    .Count();
                var warehouseCount = actualForKey.Select(l => l.FacilityId).Distinct().Count();
                return new DaImportPipelineCommodityRowDto(
                    string.IsNullOrWhiteSpace(hsCode) ? "—" : hsCode,
                    commodityName,
                    expectedForKey.Sum(l => l.VolumeKg),
                    actualForKey.Sum(l => l.VolumeKg),
                    expectedForKey.Where(l => l.Stage == "Processing").Sum(l => l.VolumeKg),
                    expectedForKey.Where(l => l.Stage == "InTransit").Sum(l => l.VolumeKg),
                    expectedForKey.Where(l => l.Stage == "AwaitingStorage").Sum(l => l.VolumeKg),
                    expectedForKey.Count(l => l.ContainerCount > 0),
                    actualForKey.Count,
                    importerCount,
                    warehouseCount);
            })
            .OrderByDescending(r => r.ExpectedKg + r.ActualKg)
            .ThenBy(r => r.CommodityName)
            .ToList();

        var byImporter = filteredExpected
            .Select(l => (l.ImporterUuid, l.ImporterName, l.CompanyName, Kg: l.VolumeKg, EntryId: l.EntryId, Hs: Norm(l.HsCode), Commodity: Norm(l.CommodityName), IsExpected: true))
            .Concat(filteredActual.Select(l => (l.ImporterUuid, l.ImporterName, l.CompanyName, Kg: l.VolumeKg, EntryId: l.EntryId, Hs: Norm(l.HsCode), Commodity: Norm(l.CommodityName), IsExpected: false)))
            .GroupBy(l => l.ImporterUuid)
            .Select(g =>
            {
                var sample = g.First();
                return new DaImportPipelineImporterRowDto(
                    sample.ImporterUuid,
                    sample.ImporterName,
                    sample.CompanyName,
                    g.Where(l => l.IsExpected).Sum(l => l.Kg),
                    g.Where(l => !l.IsExpected).Sum(l => l.Kg),
                    g.Where(l => l.IsExpected).Select(l => l.EntryId).Distinct().Count(),
                    g.Select(l => (l.Hs, l.Commodity)).Distinct().Count());
            })
            .OrderByDescending(r => r.ExpectedKg + r.ActualKg)
            .ThenBy(r => r.CompanyName ?? r.ImporterName)
            .ToList();

        var byCommodityImporter = commodityKeys
            .SelectMany(key =>
            {
                var expectedForKey = filteredExpected.Where(l => Norm(l.HsCode) == key.Item1 && Norm(l.CommodityName) == key.Item2);
                var actualForKey = filteredActual.Where(l => Norm(l.HsCode) == key.Item1 && Norm(l.CommodityName) == key.Item2);
                var hsCode = expectedForKey.FirstOrDefault()?.HsCode ?? actualForKey.First().HsCode;
                var commodityName = expectedForKey.FirstOrDefault()?.CommodityName ?? actualForKey.First().CommodityName;
                var importerKeys = expectedForKey.Select(l => l.ImporterUuid)
                    .Concat(actualForKey.Select(l => l.ImporterUuid))
                    .Distinct();

                return importerKeys.Select(importerUuid =>
                {
                    var expectedImporter = expectedForKey.Where(l => l.ImporterUuid == importerUuid).ToList();
                    var actualImporter = actualForKey.Where(l => l.ImporterUuid == importerUuid).ToList();
                    var importerName = expectedImporter.FirstOrDefault()?.ImporterName
                        ?? actualImporter.First().ImporterName;
                    var companyName = expectedImporter.FirstOrDefault()?.CompanyName
                        ?? actualImporter.First().CompanyName;
                    return new DaImportPipelineCommodityImporterRowDto(
                        string.IsNullOrWhiteSpace(hsCode) ? "—" : hsCode,
                        commodityName,
                        importerUuid,
                        importerName,
                        companyName,
                        expectedImporter.Sum(l => l.VolumeKg),
                        actualImporter.Sum(l => l.VolumeKg),
                        expectedImporter.Count(l => l.ContainerCount > 0) + actualImporter.Count);
                });
            })
            .OrderByDescending(r => r.ExpectedKg + r.ActualKg)
            .ThenBy(r => r.CommodityName)
            .ThenBy(r => r.CompanyName ?? r.ImporterName)
            .ToList();

        var byCommodityWarehouse = commodityKeys
            .SelectMany(key =>
            {
                var actualForKey = filteredActual.Where(l => Norm(l.HsCode) == key.Item1 && Norm(l.CommodityName) == key.Item2).ToList();
                if (actualForKey.Count == 0)
                {
                    return Array.Empty<DaImportPipelineCommodityWarehouseRowDto>();
                }

                var hsCode = actualForKey.First().HsCode;
                var commodityName = actualForKey.First().CommodityName;
                return actualForKey
                    .GroupBy(l => l.FacilityId)
                    .Select(g =>
                    {
                        var sample = g.First();
                        return new DaImportPipelineCommodityWarehouseRowDto(
                            string.IsNullOrWhiteSpace(hsCode) ? "—" : hsCode,
                            commodityName,
                            sample.FacilityId,
                            sample.FacilityCode,
                            sample.FacilityName,
                            sample.RegionName,
                            g.Sum(l => l.VolumeKg),
                            g.Count());
                    });
            })
            .OrderByDescending(r => r.ActualKg)
            .ThenBy(r => r.CommodityName)
            .ThenBy(r => r.WarehouseName)
            .ToList();

        var storedByEntry = filteredActual
            .GroupBy(l => l.EntryId)
            .ToDictionary(g => g.Key, g => g.Count());

        var entries = filteredExpected
            .GroupBy(l => l.EntryId)
            .Select(g =>
            {
                var sample = g.First();
                var totalContainers = sample.TotalContainers;
                var storedContainers = storedByEntry.GetValueOrDefault(sample.EntryId);
                var pendingContainers = g.Count(l => l.ContainerCount > 0);
                var dominantStage = g
                    .GroupBy(l => l.Stage)
                    .OrderByDescending(x => x.Sum(v => v.VolumeKg))
                    .Select(x => x.Key)
                    .First();
                return new DaImportPipelineEntryRowDto(
                    sample.EntryUuid,
                    sample.EntryReference,
                    sample.AgencyCode,
                    sample.EntryStatus,
                    dominantStage,
                    PipelineStageLabel(dominantStage),
                    sample.CommodityName,
                    string.IsNullOrWhiteSpace(sample.HsCode) ? "—" : sample.HsCode,
                    g.Sum(l => l.VolumeKg),
                    totalContainers,
                    storedContainers,
                    pendingContainers,
                    sample.SubmittedAt,
                    sample.ImporterUuid,
                    sample.ImporterName,
                    sample.CompanyName);
            })
            .OrderByDescending(e => e.ExpectedKg)
            .ThenByDescending(e => e.SubmittedAt ?? DateTime.MinValue)
            .Take(200)
            .ToList();

        return new DaImportPipelineReportDto(
            filteredExpected.Sum(l => l.VolumeKg),
            filteredActual.Sum(l => l.VolumeKg),
            filteredExpected.Count(l => l.ContainerCount > 0),
            filteredActual.Count,
            entries.Count,
            byStage,
            byCommodity,
            byImporter,
            byCommodityImporter,
            byCommodityWarehouse,
            entries);
    }

    public async Task<DaGeoStockReportDto> GetGeoStockReportAsync(DaGeoStockQuery query, CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var categories = await _db.MavHsCategories.AsNoTracking().Where(c => c.IsActive).ToListAsync(cancellationToken);
        var stockLines = await LoadStoredStockLinesAsync(categories, cancellationToken);
        var facilities = await _db.WarehouseFacilities
            .AsNoTracking()
            .Include(f => f.Region)
            .Include(f => f.Province)
            .Include(f => f.City)
            .Include(f => f.Barangay)
            .Where(f => f.IsActive)
            .ToListAsync(cancellationToken);

        var storedByFacility = stockLines
            .GroupBy(l => l.FacilityId)
            .ToDictionary(g => g.Key, g => g.Count());

        var hsFilter = query.HsCode?.Trim();
        var commodityFilter = query.CommodityName?.Trim();

        var lines = stockLines
            .Where(l => MatchesCommodityFilter(l.HsCode, l.CommodityName, hsFilter, commodityFilter))
            .Select(l => new GeoStockLine(
                l.FacilityId,
                l.RegionId,
                l.RegionName,
                l.ProvinceId,
                l.ProvinceName,
                l.CityId,
                l.CityName,
                l.BarangayId,
                l.BarangayName,
                l.HsCode,
                l.CommodityName,
                l.VolumeKg))
            .ToList();

        var scopedFacilities = facilities.Where(f => MatchesGeo(f, query)).ToList();
        var scopedLines = lines.Where(l => MatchesGeo(l, query)).ToList();
        var (level, scopeLabel, path) = await BuildGeoPathAsync(query, cancellationToken);
        var commodities = AggregateCommodities(scopedLines);
        var locations = await BuildChildLocationsAsync(level, query, scopedFacilities, scopedLines, storedByFacility, cancellationToken);
        var warehouses = level is "city" or "barangay"
            ? scopedFacilities
                .Select(f => MapWarehouseStock(f, scopedLines, storedByFacility.GetValueOrDefault(f.Id)))
                .OrderByDescending(w => w.VolumeKg)
                .ThenBy(w => w.Name)
                .ToList()
            : new List<DaGeoStockWarehouseRowDto>();

        var storedContainers = scopedFacilities.Sum(f => storedByFacility.GetValueOrDefault(f.Id));
        var capacity = scopedFacilities.Sum(f => Math.Max(0, f.Capacity));
        return new DaGeoStockReportDto(
            level,
            scopeLabel,
            scopedLines.Sum(l => l.VolumeKg),
            scopedFacilities.Count,
            storedContainers,
            capacity,
            UtilizationPercent(storedContainers, capacity),
            path,
            commodities,
            locations,
            warehouses);
    }

    private async Task<(string Level, string ScopeLabel, IReadOnlyList<DaGeoStockBreadcrumbDto> Path)> BuildGeoPathAsync(
        DaGeoStockQuery query,
        CancellationToken cancellationToken)
    {
        var path = new List<DaGeoStockBreadcrumbDto> { new("nation", null, "Philippines") };
        if (query.RegionId is long regionId)
        {
            var region = await _db.AddressRegions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == regionId, cancellationToken);
            path.Add(new("region", regionId, region?.Name ?? "Region"));
        }

        if (query.ProvinceId is long provinceId)
        {
            var province = await _db.AddressProvinces.AsNoTracking().FirstOrDefaultAsync(p => p.Id == provinceId, cancellationToken);
            path.Add(new("province", provinceId, province?.Name ?? "Province"));
        }

        if (query.CityId is long cityId)
        {
            var city = await _db.AddressCities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);
            path.Add(new("city", cityId, city?.Name ?? "City/Municipality"));
        }

        if (query.BarangayId is long barangayId)
        {
            var barangay = await _db.AddressBarangays.AsNoTracking().FirstOrDefaultAsync(b => b.Id == barangayId, cancellationToken);
            path.Add(new("barangay", barangayId, barangay?.Name ?? "Barangay"));
        }

        var current = path[^1];
        return (current.Level, current.Name, path);
    }

    private async Task<IReadOnlyList<DaGeoStockLocationRowDto>> BuildChildLocationsAsync(
        string level,
        DaGeoStockQuery query,
        IReadOnlyList<WarehouseFacility> scopedFacilities,
        IReadOnlyList<GeoStockLine> scopedLines,
        IReadOnlyDictionary<long, int> storedByFacility,
        CancellationToken cancellationToken)
    {
        if (level == "nation")
        {
            var regions = await _db.AddressRegions.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync(cancellationToken);
            var rows = regions.Select(region => MapLocation(
                "region",
                region.Id,
                region.Name,
                scopedFacilities.Where(f => f.RegionId == region.Id).ToList(),
                scopedLines.Where(l => l.RegionId == region.Id).ToList(),
                storedByFacility)).ToList();
            var unclassified = scopedFacilities.Where(f => f.RegionId == null).ToList();
            if (unclassified.Count > 0)
            {
                rows.Add(MapLocation("region", null, "Unclassified Region", unclassified, scopedLines.Where(l => l.RegionId == null).ToList(), storedByFacility));
            }

            return rows.OrderByDescending(r => r.VolumeKg).ThenBy(r => r.Name).ToList();
        }

        if (level == "region" && query.RegionId is long regionId)
        {
            var provinces = await _db.AddressProvinces.AsNoTracking()
                .Where(p => p.IsActive && p.RegionId == regionId)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
            return provinces
                .Select(province => MapLocation(
                    "province",
                    province.Id,
                    province.Name,
                    scopedFacilities.Where(f => f.ProvinceId == province.Id).ToList(),
                    scopedLines.Where(l => l.ProvinceId == province.Id).ToList(),
                    storedByFacility))
                .Where(row => row.WarehouseCount > 0 || row.VolumeKg > 0)
                .OrderByDescending(r => r.VolumeKg)
                .ThenBy(r => r.Name)
                .ToList();
        }

        if (level == "province" && query.ProvinceId is long provinceId)
        {
            var cities = await _db.AddressCities.AsNoTracking()
                .Where(c => c.IsActive && c.ProvinceId == provinceId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
            return cities
                .Select(city => MapLocation(
                    "city",
                    city.Id,
                    city.Name,
                    scopedFacilities.Where(f => f.CityId == city.Id).ToList(),
                    scopedLines.Where(l => l.CityId == city.Id).ToList(),
                    storedByFacility))
                .Where(row => row.WarehouseCount > 0 || row.VolumeKg > 0)
                .OrderByDescending(r => r.VolumeKg)
                .ThenBy(r => r.Name)
                .ToList();
        }

        if (level == "city" && query.CityId is long cityId)
        {
            var barangays = await _db.AddressBarangays.AsNoTracking()
                .Where(b => b.IsActive && b.CityId == cityId)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken);
            return barangays
                .Select(barangay => MapLocation(
                    "barangay",
                    barangay.Id,
                    barangay.Name,
                    scopedFacilities.Where(f => f.BarangayId == barangay.Id).ToList(),
                    scopedLines.Where(l => l.BarangayId == barangay.Id).ToList(),
                    storedByFacility))
                .Where(row => row.WarehouseCount > 0 || row.VolumeKg > 0)
                .OrderByDescending(r => r.VolumeKg)
                .ThenBy(r => r.Name)
                .ToList();
        }

        return Array.Empty<DaGeoStockLocationRowDto>();
    }

    private static DaGeoStockLocationRowDto MapLocation(
        string level,
        long? id,
        string name,
        IReadOnlyList<WarehouseFacility> facilities,
        IReadOnlyList<GeoStockLine> lines,
        IReadOnlyDictionary<long, int> storedByFacility)
    {
        var stored = facilities.Sum(f => storedByFacility.GetValueOrDefault(f.Id));
        var capacity = facilities.Sum(f => Math.Max(0, f.Capacity));
        return new DaGeoStockLocationRowDto(
            level,
            id,
            name,
            lines.Sum(l => l.VolumeKg),
            facilities.Count,
            stored,
            capacity,
            UtilizationPercent(stored, capacity),
            AggregateCommodities(lines).Take(3).ToList());
    }

    private static DaGeoStockWarehouseRowDto MapWarehouseStock(
        WarehouseFacility facility,
        IReadOnlyList<GeoStockLine> lines,
        int storedContainers)
    {
        var capacity = Math.Max(0, facility.Capacity);
        var warehouseLines = lines.Where(l => l.FacilityId == facility.Id).ToList();
        var top = warehouseLines
            .GroupBy(l => new { l.HsCode, l.CommodityName })
            .Select(g => new { g.Key.HsCode, g.Key.CommodityName, Volume = g.Sum(l => l.VolumeKg) })
            .OrderByDescending(g => g.Volume)
            .FirstOrDefault();
        return new DaGeoStockWarehouseRowDto(
            facility.Id,
            facility.Code,
            facility.Name,
            warehouseLines.Sum(l => l.VolumeKg),
            storedContainers,
            capacity,
            UtilizationPercent(storedContainers, capacity),
            facility.Barangay?.Name,
            top?.CommodityName,
            string.IsNullOrWhiteSpace(top?.HsCode) ? null : top.HsCode);
    }

    private static IReadOnlyList<DaGeoStockCommodityRowDto> AggregateCommodities(IReadOnlyList<GeoStockLine> lines) =>
        lines
            .GroupBy(l => new { Hs = string.IsNullOrWhiteSpace(l.HsCode) ? "—" : l.HsCode, l.CommodityName })
            .Select(g => new DaGeoStockCommodityRowDto(
                g.Key.Hs,
                g.Key.CommodityName,
                g.Sum(l => l.VolumeKg),
                g.Count(),
                g.Select(l => l.FacilityId).Distinct().Count()))
            .OrderByDescending(r => r.VolumeKg)
            .ThenBy(r => r.CommodityName)
            .ToList();

    private static bool MatchesGeo(WarehouseFacility facility, DaGeoStockQuery query) =>
        (query.RegionId is null || facility.RegionId == query.RegionId)
        && (query.ProvinceId is null || facility.ProvinceId == query.ProvinceId)
        && (query.CityId is null || facility.CityId == query.CityId)
        && (query.BarangayId is null || facility.BarangayId == query.BarangayId);

    private static bool MatchesGeo(GeoStockLine line, DaGeoStockQuery query) =>
        (query.RegionId is null || line.RegionId == query.RegionId)
        && (query.ProvinceId is null || line.ProvinceId == query.ProvinceId)
        && (query.CityId is null || line.CityId == query.CityId)
        && (query.BarangayId is null || line.BarangayId == query.BarangayId);

    private static bool MatchesCommodityFilter(string hsCode, string commodityName, string? hsFilter, string? commodityFilter)
    {
        if (!string.IsNullOrWhiteSpace(hsFilter)
            && !hsCode.StartsWith(hsFilter, StringComparison.OrdinalIgnoreCase)
            && !hsFilter.StartsWith(hsCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(commodityFilter)
            || commodityName.Contains(commodityFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal ResolveEntryVolumeKg(Entry entry) => DaStockVolumeHelper.ToKilograms(entry);

    private static decimal UtilizationPercent(int stored, int capacity) =>
        capacity > 0 ? Math.Round((decimal)stored / capacity * 100m, 1) : 0m;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string Norm(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static (Guid ImporterUuid, string ImporterName, string? CompanyName) ResolveImporter(User user)
    {
        var importerName = user.Profile != null
            ? $"{user.Profile.FirstName} {user.Profile.LastName}".Trim()
            : user.Email;
        if (string.IsNullOrWhiteSpace(importerName))
        {
            importerName = user.Email;
        }

        return (user.Uuid, importerName, user.Profile?.CompanyName?.Trim());
    }

    private static string ClassifyPipelineStage(Entry entry, Container? container)
    {
        if (container is null)
        {
            return entry.Status switch
            {
                EntryStatus.InTransit or EntryStatus.AwaitingTransport or EntryStatus.ReadyForTransport or EntryStatus.PartiallyConfirmed
                    => "InTransit",
                _ => "Processing",
            };
        }

        return container.Status switch
        {
            ContainerStatus.Pending or ContainerStatus.ReadyForTransport => "Processing",
            ContainerStatus.AwaitingConfirmation or ContainerStatus.Assigned or ContainerStatus.InTransit or ContainerStatus.UnderInspection => "InTransit",
            ContainerStatus.Inspected or ContainerStatus.AtWarehouse => "AwaitingStorage",
            _ => "Processing",
        };
    }

    private static string PipelineStageLabel(string stage) => stage switch
    {
        "Processing" => "Agency processing",
        "InTransit" => "In transit / at port",
        "AwaitingStorage" => "Awaiting warehouse intake",
        _ => stage,
    };

    private async Task<List<PipelineLine>> LoadExpectedPipelineLinesAsync(
        IReadOnlyCollection<MavHsCategory> categories,
        CancellationToken cancellationToken)
    {
        var storedContainerIds = (await _db.WarehouseInventories
            .AsNoTracking()
            .Where(i => i.Status == WarehouseInventoryStatus.Stored)
            .Select(i => i.ContainerId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var activeStatuses = new[]
        {
            EntryStatus.Submitted,
            EntryStatus.UnderReview,
            EntryStatus.ForCompliance,
            EntryStatus.Approved,
            EntryStatus.DaIssueBilling,
            EntryStatus.ForInspection,
            EntryStatus.ReadyForTransport,
            EntryStatus.AwaitingTransport,
            EntryStatus.PartiallyConfirmed,
            EntryStatus.InTransit,
        };

        var entries = await _db.Entries
            .AsNoTracking()
            .Include(e => e.Agency)
            .Include(e => e.User).ThenInclude(u => u.Profile)
            .Include(e => e.Detail)
            .Include(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .Include(e => e.Containers)
            .Where(e => e.EntryType == EntryType.Import && activeStatuses.Contains(e.Status))
            .ToListAsync(cancellationToken);

        var lines = new List<PipelineLine>();
        foreach (var entry in entries)
        {
            var mic = entry.MicUtilizations.OrderByDescending(u => u.UtilizedAt).Select(u => u.Mic).FirstOrDefault();
            var hsCode = FirstNonEmpty(mic?.HsCode?.Trim(), ResolveEntryHsCode(entry, categories)) ?? string.Empty;
            var commodityName = FirstNonEmpty(mic?.CommodityName, entry.Detail?.CommodityName) ?? "Unclassified";
            var entryVolumeKg = DaStockVolumeHelper.ToKilograms(entry);
            var agencyCode = entry.Agency?.Code ?? "Shared";
            var totalContainers = entry.Containers.Count;
            var (importerUuid, importerName, companyName) = ResolveImporter(entry.User);

            var pendingContainers = entry.Containers
                .Where(c => c.Status != ContainerStatus.Released && !storedContainerIds.Contains(c.Id))
                .ToList();

            if (pendingContainers.Count == 0)
            {
                if (totalContainers == 0)
                {
                    lines.Add(new PipelineLine(
                        entry.Id,
                        entry.Uuid,
                        entry.ReferenceNo,
                        agencyCode,
                        entry.Status.ToString(),
                        entry.SubmittedAt,
                        importerUuid,
                        importerName,
                        companyName,
                        hsCode,
                        commodityName,
                        entryVolumeKg,
                        ClassifyPipelineStage(entry, null),
                        0,
                        totalContainers));
                }

                continue;
            }

            var perContainerKg = entryVolumeKg / pendingContainers.Count;
            foreach (var container in pendingContainers)
            {
                lines.Add(new PipelineLine(
                    entry.Id,
                    entry.Uuid,
                    entry.ReferenceNo,
                    agencyCode,
                    entry.Status.ToString(),
                    entry.SubmittedAt,
                    importerUuid,
                    importerName,
                    companyName,
                    hsCode,
                    commodityName,
                    perContainerKg,
                    ClassifyPipelineStage(entry, container),
                    1,
                    totalContainers));
            }
        }

        return lines;
    }

    private async Task<List<StockInventoryLine>> LoadStoredStockLinesAsync(
        IReadOnlyCollection<MavHsCategory> categories,
        CancellationToken cancellationToken)
    {
        var inventories = await _db.WarehouseInventories
            .AsNoTracking()
            .Include(i => i.WarehouseFacility).ThenInclude(f => f.Region)
            .Include(i => i.WarehouseFacility).ThenInclude(f => f.Province)
            .Include(i => i.WarehouseFacility).ThenInclude(f => f.City)
            .Include(i => i.WarehouseFacility).ThenInclude(f => f.Barangay)
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.Agency)
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.User).ThenInclude(u => u.Profile)
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .Where(i => i.Status == WarehouseInventoryStatus.Stored)
            .ToListAsync(cancellationToken);

        var storedByEntry = inventories
            .GroupBy(i => i.Container.EntryId)
            .ToDictionary(g => g.Key, g => g.Count());

        var lines = new List<StockInventoryLine>();
        foreach (var item in inventories)
        {
            var facility = item.WarehouseFacility;
            var entry = item.Container.Entry;
            var mic = entry.MicUtilizations.OrderByDescending(u => u.UtilizedAt).Select(u => u.Mic).FirstOrDefault();
            var hsCode = FirstNonEmpty(mic?.HsCode?.Trim(), ResolveEntryHsCode(entry, categories)) ?? string.Empty;
            var commodityName = FirstNonEmpty(mic?.CommodityName, entry.Detail?.CommodityName) ?? "Unclassified";
            var split = storedByEntry.GetValueOrDefault(entry.Id, 1);
            var volumeKg = DaStockVolumeHelper.ToKilograms(entry) / Math.Max(split, 1);
            var (importerUuid, importerName, companyName) = ResolveImporter(entry.User);
            lines.Add(new StockInventoryLine(
                facility.Id,
                facility.Code,
                facility.Name,
                entry.Id,
                importerUuid,
                importerName,
                companyName,
                entry.Agency?.Code ?? "Shared",
                entry.ImportTrack,
                facility.RegionId,
                facility.Region?.Name ?? "Unclassified Region",
                facility.ProvinceId,
                facility.Province?.Name ?? "Unclassified Province",
                facility.CityId,
                facility.City?.Name ?? "Unclassified City/Municipality",
                facility.BarangayId,
                facility.Barangay?.Name ?? "Unclassified Barangay",
                hsCode,
                commodityName,
                volumeKg));
        }

        return lines;
    }

    private sealed record PipelineLine(
        long EntryId,
        Guid EntryUuid,
        string EntryReference,
        string AgencyCode,
        string EntryStatus,
        DateTime? SubmittedAt,
        Guid ImporterUuid,
        string ImporterName,
        string? CompanyName,
        string HsCode,
        string CommodityName,
        decimal VolumeKg,
        string Stage,
        int ContainerCount,
        int TotalContainers);

    private sealed record StockInventoryLine(
        long FacilityId,
        string FacilityCode,
        string FacilityName,
        long EntryId,
        Guid ImporterUuid,
        string ImporterName,
        string? CompanyName,
        string AgencyCode,
        EntryImportTrack ImportTrack,
        long? RegionId,
        string RegionName,
        long? ProvinceId,
        string ProvinceName,
        long? CityId,
        string CityName,
        long? BarangayId,
        string BarangayName,
        string HsCode,
        string CommodityName,
        decimal VolumeKg);

    private sealed record GeoStockLine(
        long FacilityId,
        long? RegionId,
        string RegionName,
        long? ProvinceId,
        string ProvinceName,
        long? CityId,
        string CityName,
        long? BarangayId,
        string BarangayName,
        string HsCode,
        string CommodityName,
        decimal VolumeKg);
}

public class DaWarehouseManagementService : IDaWarehouseManagementService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DaWarehouseManagementService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DaWarehouseListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var facilities = await _db.WarehouseFacilities
            .AsNoTracking()
            .Include(x => x.Region)
            .Include(x => x.Province)
            .Include(x => x.City)
            .Include(x => x.Barangay)
            .OrderBy(x => x.Region != null ? x.Region.Name : "ZZZ")
            .ThenBy(x => x.Province != null ? x.Province.Name : "ZZZ")
            .ThenBy(x => x.City != null ? x.City.Name : "ZZZ")
            .ThenBy(x => x.Barangay != null ? x.Barangay.Name : "ZZZ")
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return facilities.Select(MapWarehouse).ToList();
    }

    public async Task<DaWarehouseDetailDto> GetDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var facility = await _db.WarehouseFacilities
            .AsNoTracking()
            .Include(x => x.Region)
            .Include(x => x.Province)
            .Include(x => x.City)
            .Include(x => x.Barangay)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Warehouse not found.");

        var storedCount = await _db.WarehouseInventories.CountAsync(
            i => i.WarehouseFacilityId == id && i.Status == WarehouseInventoryStatus.Stored,
            cancellationToken);
        var releasedCount = await _db.WarehouseInventories.CountAsync(
            i => i.WarehouseFacilityId == id && i.Status == WarehouseInventoryStatus.Released,
            cancellationToken);
        var pendingBookings = await _db.WarehouseBookings.CountAsync(
            b => b.WarehouseFacilityId == id
                && b.Status != WarehouseBookingStatus.Cancelled
                && b.Status != WarehouseBookingStatus.Completed,
            cancellationToken);

        var inventoryEntities = await _db.WarehouseInventories
            .AsNoTracking()
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .Include(i => i.ReceivedBy).ThenInclude(u => u.Profile)
            .Where(i => i.WarehouseFacilityId == id)
            .OrderByDescending(i => i.Status == WarehouseInventoryStatus.Stored)
            .ThenByDescending(i => i.ReceivedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var storedForVolume = await _db.WarehouseInventories
            .AsNoTracking()
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .Where(i => i.WarehouseFacilityId == id && i.Status == WarehouseInventoryStatus.Stored)
            .ToListAsync(cancellationToken);
        var storedByEntry = storedForVolume
            .GroupBy(i => i.Container.EntryId)
            .ToDictionary(g => g.Key, g => g.Count());

        var commodityLines = storedForVolume.Select(item =>
        {
            var entry = item.Container.Entry;
            var mic = entry.MicUtilizations.OrderByDescending(u => u.UtilizedAt).Select(u => u.Mic).FirstOrDefault();
            var split = storedByEntry.GetValueOrDefault(entry.Id, 1);
            return new DaGeoStockCommodityRowDto(
                string.IsNullOrWhiteSpace(mic?.HsCode) ? "—" : mic!.HsCode.Trim(),
                string.IsNullOrWhiteSpace(mic?.CommodityName) ? (entry.Detail?.CommodityName ?? "Unclassified") : mic.CommodityName.Trim(),
                DaStockVolumeHelper.ToKilograms(entry) / Math.Max(split, 1),
                1,
                1);
        }).ToList();
        var commodities = commodityLines
            .GroupBy(l => new { l.HsCode, l.CommodityName })
            .Select(g => new DaGeoStockCommodityRowDto(
                g.Key.HsCode,
                g.Key.CommodityName,
                g.Sum(l => l.VolumeKg),
                g.Count(),
                1))
            .OrderByDescending(r => r.VolumeKg)
            .ThenBy(r => r.CommodityName)
            .ToList();

        var inventory = inventoryEntities.Select(i =>
        {
            var entry = i.Container.Entry;
            var mic = entry.MicUtilizations.OrderByDescending(u => u.UtilizedAt).Select(u => u.Mic).FirstOrDefault();
            var split = storedByEntry.GetValueOrDefault(entry.Id, 1);
            var volumeKg = i.Status == WarehouseInventoryStatus.Stored
                ? DaStockVolumeHelper.ToKilograms(entry) / Math.Max(split, 1)
                : (decimal?)null;
            return new DaWarehouseInventoryItemDto(
                i.Uuid,
                i.Container.ContainerNumber,
                i.Container.ContainerType,
                entry.ReferenceNo,
                i.LocationCode,
                i.Status.ToString(),
                i.ReceivedAt,
                i.ReceivedBy.Profile != null
                    ? (i.ReceivedBy.Profile.FirstName + " " + i.ReceivedBy.Profile.LastName).Trim()
                    : i.ReceivedBy.Email,
                string.IsNullOrWhiteSpace(mic?.HsCode) ? null : mic.HsCode.Trim(),
                string.IsNullOrWhiteSpace(mic?.CommodityName) ? entry.Detail?.CommodityName : mic.CommodityName.Trim(),
                volumeKg);
        }).ToList();

        var capacity = Math.Max(0, facility.Capacity);
        var utilizationPercent = capacity > 0
            ? Math.Round((decimal)storedCount / capacity * 100m, 1)
            : 0m;

        return new DaWarehouseDetailDto(
            MapWarehouse(facility),
            storedCount,
            releasedCount,
            pendingBookings,
            capacity,
            utilizationPercent,
            commodities.Sum(c => c.VolumeKg),
            commodities,
            inventory);
    }

    public async Task<DaWarehouseListItemDto> CreateAsync(SaveDaWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.WarehouseFacilities.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw new ClientPortalException("CODE_EXISTS", "Warehouse code already exists.");
        }

        var (region, province, city, barangay) = await ResolveAddressHierarchyAsync(
            request.RegionId,
            request.ProvinceId,
            request.CityId,
            request.BarangayId,
            cancellationToken);

        var facility = new WarehouseFacility
        {
            Code = code,
            Name = request.Name.Trim(),
            Capacity = Math.Max(0, request.Capacity),
            IsActive = request.IsActive,
            RegionId = region.Id,
            ProvinceId = province.Id,
            CityId = city.Id,
            BarangayId = barangay.Id,
            StreetAddress = NormalizeOptional(request.StreetAddress),
            ZipCode = NormalizeOptional(request.ZipCode) ?? barangay.ZipCode,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Location = WarehouseAddressFormatter.Format(
                request.StreetAddress,
                barangay.Name,
                city.Name,
                province.Name,
                region.Name,
                request.ZipCode ?? barangay.ZipCode,
                null),
        };

        _db.WarehouseFacilities.Add(facility);
        await _db.SaveChangesAsync(cancellationToken);

        return MapWarehouse(await LoadWarehouseAsync(facility.Id, cancellationToken));
    }

    public async Task<DaWarehouseListItemDto> UpdateAsync(long id, SaveDaWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var facility = await _db.WarehouseFacilities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Warehouse not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.WarehouseFacilities.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
        {
            throw new ClientPortalException("CODE_EXISTS", "Warehouse code already exists.");
        }

        var (region, province, city, barangay) = await ResolveAddressHierarchyAsync(
            request.RegionId,
            request.ProvinceId,
            request.CityId,
            request.BarangayId,
            cancellationToken);

        facility.Code = code;
        facility.Name = request.Name.Trim();
        facility.Capacity = Math.Max(0, request.Capacity);
        facility.IsActive = request.IsActive;
        facility.RegionId = region.Id;
        facility.ProvinceId = province.Id;
        facility.CityId = city.Id;
        facility.BarangayId = barangay.Id;
        facility.StreetAddress = NormalizeOptional(request.StreetAddress);
        facility.ZipCode = NormalizeOptional(request.ZipCode) ?? barangay.ZipCode;
        facility.Latitude = request.Latitude;
        facility.Longitude = request.Longitude;
        facility.Location = WarehouseAddressFormatter.Format(
            request.StreetAddress,
            barangay.Name,
            city.Name,
            province.Name,
            region.Name,
            request.ZipCode ?? barangay.ZipCode,
            null);

        await _db.SaveChangesAsync(cancellationToken);
        return MapWarehouse(await LoadWarehouseAsync(facility.Id, cancellationToken));
    }

    private async Task<WarehouseFacility> LoadWarehouseAsync(long id, CancellationToken cancellationToken) =>
        await _db.WarehouseFacilities
            .AsNoTracking()
            .Include(x => x.Region)
            .Include(x => x.Province)
            .Include(x => x.City)
            .Include(x => x.Barangay)
            .FirstAsync(x => x.Id == id, cancellationToken);

    private async Task<(Domain.Entities.AddressRegion Region, Domain.Entities.AddressProvince Province, Domain.Entities.AddressCity City, Domain.Entities.AddressBarangay Barangay)>
        ResolveAddressHierarchyAsync(
            long regionId,
            long provinceId,
            long cityId,
            long barangayId,
            CancellationToken cancellationToken)
    {
        var barangay = await _db.AddressBarangays
            .Include(x => x.City).ThenInclude(x => x!.Province).ThenInclude(x => x!.Region)
            .FirstOrDefaultAsync(x => x.Id == barangayId && x.IsActive, cancellationToken)
            ?? throw new ClientPortalException("INVALID_ADDRESS", "Barangay not found.");

        if (barangay.CityId != cityId || barangay.City?.ProvinceId != provinceId || barangay.City?.Province?.RegionId != regionId)
        {
            throw new ClientPortalException("INVALID_ADDRESS", "Address hierarchy does not match selected location.");
        }

        return (barangay.City!.Province!.Region!, barangay.City.Province, barangay.City, barangay);
    }

    private static DaWarehouseListItemDto MapWarehouse(WarehouseFacility facility)
    {
        var formattedAddress = WarehouseAddressFormatter.Format(
            facility.StreetAddress,
            facility.Barangay?.Name,
            facility.City?.Name,
            facility.Province?.Name,
            facility.Region?.Name,
            facility.ZipCode ?? facility.Barangay?.ZipCode,
            facility.Location);

        return new DaWarehouseListItemDto(
            facility.Id,
            facility.Code,
            facility.Name,
            facility.IsActive,
            facility.Capacity,
            facility.RegionId,
            facility.Region?.Name,
            facility.ProvinceId,
            facility.Province?.Name,
            facility.CityId,
            facility.City?.Name,
            facility.BarangayId,
            facility.Barangay?.Name,
            facility.StreetAddress,
            facility.ZipCode ?? facility.Barangay?.ZipCode,
            formattedAddress,
            facility.Latitude,
            facility.Longitude);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public class DaImporterProfileService : IDaImporterProfileService
{
    private static readonly string[] ClientRoleCodes = { "ROLE_IMPORTER", "ROLE_EXPORTER", "ROLE_BROKER" };

    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DaImporterProfileService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<DaImporterListItemDto>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.UserRoles.Any(ur => ClientRoleCodes.Contains(ur.Role.Code)));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.Email.Contains(term)
                || (u.Profile != null && (
                    u.Profile.FirstName.Contains(term)
                    || u.Profile.LastName.Contains(term)
                    || (u.Profile.CompanyName != null && u.Profile.CompanyName.Contains(term)))));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.Profile != null ? u.Profile.CompanyName ?? u.Email : u.Email)
            .ThenBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var entryCounts = await _db.Entries
            .AsNoTracking()
            .Where(e => userIds.Contains(e.UserId))
            .GroupBy(e => e.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Total = g.Count(),
                Import = g.Count(e => e.EntryType == EntryType.Import),
            })
            .ToListAsync(cancellationToken);

        var accreditations = await _db.AccreditationSubmissions
            .AsNoTracking()
            .Where(s => userIds.Contains(s.UserId))
            .ToListAsync(cancellationToken);

        var items = users.Select(user =>
        {
            var counts = entryCounts.FirstOrDefault(c => c.UserId == user.Id);
            var accreditation = accreditations.FirstOrDefault(s => s.UserId == user.Id);
            var fullName = FormatFullName(user);
            return new DaImporterListItemDto(
                user.Uuid,
                fullName,
                user.Profile?.CompanyName?.Trim(),
                user.Email,
                user.Status.ToString(),
                accreditation?.Status.ToString(),
                accreditation is null ? null : AccreditationSubmissionStatusMapper.GetDisplayStatus(accreditation),
                accreditation?.Status == AccreditationSubmissionStatus.Approved,
                counts?.Total ?? 0,
                counts?.Import ?? 0,
                user.LastLoginAt);
        }).ToList();

        return new PagedResult<DaImporterListItemDto>(items, page, pageSize, total);
    }

    public async Task<DaImporterProfileDto> GetProfileAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Importer not found.");

        if (!user.UserRoles.Any(ur => ClientRoleCodes.Contains(ur.Role.Code)))
        {
            throw new ClientPortalException("NOT_FOUND", "Importer not found.");
        }

        var submission = await _db.AccreditationSubmissions
            .AsNoTracking()
            .Include(s => s.AssignedOfficer).ThenInclude(o => o!.Profile)
            .Include(s => s.Files)
            .Include(s => s.History).ThenInclude(h => h.Actor!).ThenInclude(a => a.Profile)
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var (formSchemaJson, formName) = await GetAccreditationFormMetaAsync(cancellationToken);
        Certificate? accreditationCertificate = null;
        if (submission is not null)
        {
            accreditationCertificate = await AccreditationCertificateLookup.FindForSubmissionAsync(
                _db,
                submission.Uuid,
                cancellationToken: cancellationToken);
        }

        var entries = _db.Entries.AsNoTracking().Where(e => e.UserId == user.Id);
        var entryIds = entries.Select(e => e.Id);
        var entryStatusCounts = await entries
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int EntryCount(EntryStatus status) =>
            entryStatusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        var bills = _db.ClientBills.AsNoTracking().Where(b => b.UserId == user.Id);
        var now = DateTime.UtcNow;
        var openBills = bills.Where(b => b.Status != ClientBillStatus.Paid && b.Status != ClientBillStatus.Cancelled);
        var overdueBills = openBills.Where(b => b.Status == ClientBillStatus.Overdue || (b.DueDate != null && b.DueDate < now));

        var containers = _db.Containers.AsNoTracking().Where(c => entryIds.Contains(c.EntryId));
        var storedContainerIds = await _db.WarehouseInventories
            .AsNoTracking()
            .Where(i => i.Status == WarehouseInventoryStatus.Stored)
            .Select(i => i.ContainerId)
            .ToListAsync(cancellationToken);
        var storedContainers = await containers.CountAsync(
            c => storedContainerIds.Contains(c.Id),
            cancellationToken);

        var activeMavLicenses = await _db.MavLicenses
            .AsNoTracking()
            .CountAsync(l => l.ImporterId == user.Id && l.Status == MavLicenseStatus.Active, cancellationToken);

        var pipelineStats = await BuildPipelineStatsAsync(user.Id, cancellationToken);

        var certificates = await _db.Certificates
            .AsNoTracking()
            .Where(c => c.UserId == user.Id)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new DaImporterCertificateDto(
                c.Uuid,
                c.CertificateNumber,
                c.Title,
                c.Status.ToString(),
                c.IssuedAt,
                c.ExpiresAt,
                c.Entry != null ? c.Entry.ReferenceNo : null))
            .ToListAsync(cancellationToken);

        var billRows = await bills
            .OrderByDescending(b => b.CreatedAt)
            .Take(20)
            .Select(b => new DaImporterBillDto(
                b.Uuid,
                b.BillNumber,
                b.Entry != null ? b.Entry.ReferenceNo : null,
                b.Entry != null ? b.Entry.Agency.Code : null,
                b.Description,
                b.Amount,
                b.Status.ToString(),
                b.DueDate,
                b.Status == ClientBillStatus.Overdue || (b.DueDate != null && b.DueDate < now && b.Status != ClientBillStatus.Paid)))
            .ToListAsync(cancellationToken);

        var roles = user.UserRoles.Select(ur => ur.Role.Code).Distinct().OrderBy(r => r).ToList();
        var fullName = FormatFullName(user);
        var formData = ParseImporterFormData(submission?.FormDataJson);
        var resolvedAddress = AccreditationFormVariableResolver.ResolveAddress(formData, user.Profile?.Address);
        if (string.Equals(resolvedAddress, "N/A", StringComparison.OrdinalIgnoreCase))
        {
            resolvedAddress = null;
        }

        return new DaImporterProfileDto(
            user.Uuid,
            user.Email,
            user.Status.ToString(),
            user.Profile?.FirstName ?? string.Empty,
            user.Profile?.LastName ?? string.Empty,
            fullName,
            user.Profile?.Phone,
            user.Profile?.CompanyName?.Trim(),
            resolvedAddress,
            roles,
            user.CreatedAt,
            user.LastLoginAt,
            user.EmailVerifiedAt,
            submission is null ? null : MapAccreditation(submission, formSchemaJson, formName, accreditationCertificate),
            new DaImporterEntryStatsDto(
                entryStatusCounts.Sum(x => x.Count),
                EntryCount(EntryStatus.Draft),
                EntryCount(EntryStatus.Submitted) + EntryCount(EntryStatus.UnderReview),
                EntryCount(EntryStatus.ForCompliance),
                EntryStatusRules.OperationalPipelineStatuses.Sum(EntryCount),
                EntryCount(EntryStatus.Rejected),
                EntryCount(EntryStatus.Cancelled)),
            new DaImporterWorkflowStatsDto(
                EntryCount(EntryStatus.DaIssueBilling),
                EntryCount(EntryStatus.ForInspection),
                EntryCount(EntryStatus.ReadyForTransport),
                EntryCount(EntryStatus.AwaitingTransport) + EntryCount(EntryStatus.PartiallyConfirmed),
                EntryCount(EntryStatus.InTransit)),
            new DaImporterLogisticsStatsDto(
                await openBills.CountAsync(cancellationToken),
                await overdueBills.CountAsync(cancellationToken),
                storedContainers,
                activeMavLicenses),
            pipelineStats,
            certificates,
            billRows);
    }

    public async Task<PagedResult<DaImporterEntryListItemDto>> ListEntriesAsync(
        Guid uuid,
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Importer not found.");

        var query = _db.Entries
            .AsNoTracking()
            .Include(e => e.Agency)
            .Include(e => e.Detail)
            .Include(e => e.Containers)
            .Include(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .Where(e => e.UserId == user.Id);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EntryStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(e => e.Status == parsedStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(e => e.SubmittedAt ?? e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(entry =>
        {
            var mic = entry.MicUtilizations.OrderByDescending(u => u.UtilizedAt).Select(u => u.Mic).FirstOrDefault();
            var hsCode = mic?.HsCode?.Trim();
            return new DaImporterEntryListItemDto(
                entry.Uuid,
                entry.ReferenceNo,
                entry.EntryType.ToString(),
                entry.Status.ToString(),
                entry.Agency.Code,
                entry.Detail?.CommodityName ?? mic?.CommodityName,
                string.IsNullOrWhiteSpace(hsCode) ? null : hsCode,
                entry.CreatedAt,
                entry.SubmittedAt,
                entry.PaymentStatus.ToString(),
                entry.PaymentAmount,
                entry.Containers.Count,
                DaStockVolumeHelper.ToKilograms(entry),
                entry.ImportTrack.ToString());
        }).ToList();

        return new PagedResult<DaImporterEntryListItemDto>(items, page, pageSize, total);
    }

    private async Task<DaImporterPipelineStatsDto> BuildPipelineStatsAsync(long userId, CancellationToken cancellationToken)
    {
        var storedContainerIds = (await _db.WarehouseInventories
            .AsNoTracking()
            .Where(i => i.Status == WarehouseInventoryStatus.Stored)
            .Select(i => i.ContainerId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var activeStatuses = new[]
        {
            EntryStatus.Submitted,
            EntryStatus.UnderReview,
            EntryStatus.ForCompliance,
            EntryStatus.Approved,
            EntryStatus.DaIssueBilling,
            EntryStatus.ForInspection,
            EntryStatus.ReadyForTransport,
            EntryStatus.AwaitingTransport,
            EntryStatus.PartiallyConfirmed,
            EntryStatus.InTransit,
        };

        var pipelineEntries = await _db.Entries
            .AsNoTracking()
            .Include(e => e.Containers)
            .Where(e =>
                e.UserId == userId
                && e.EntryType == EntryType.Import
                && activeStatuses.Contains(e.Status))
            .ToListAsync(cancellationToken);

        decimal expectedKg = 0;
        var pipelineEntryIds = new HashSet<long>();
        foreach (var entry in pipelineEntries)
        {
            var pendingContainers = entry.Containers
                .Where(c => c.Status != ContainerStatus.Released && !storedContainerIds.Contains(c.Id))
                .ToList();
            if (pendingContainers.Count == 0 && entry.Containers.Count > 0)
            {
                continue;
            }

            pipelineEntryIds.Add(entry.Id);
            var entryVolumeKg = DaStockVolumeHelper.ToKilograms(entry);
            if (pendingContainers.Count == 0)
            {
                expectedKg += entryVolumeKg;
                continue;
            }

            expectedKg += entryVolumeKg;
        }

        var storedInventories = await _db.WarehouseInventories
            .AsNoTracking()
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .Where(i =>
                i.Status == WarehouseInventoryStatus.Stored
                && i.Container.Entry.UserId == userId
                && i.Container.Entry.EntryType == EntryType.Import)
            .ToListAsync(cancellationToken);

        var storedByEntry = storedInventories
            .GroupBy(i => i.Container.EntryId)
            .ToDictionary(g => g.Key, g => g.Count());

        var actualVolume = storedInventories.Sum(item =>
        {
            var entry = item.Container.Entry;
            var split = storedByEntry.GetValueOrDefault(entry.Id, 1);
            return DaStockVolumeHelper.ToKilograms(entry) / Math.Max(split, 1);
        });

        return new DaImporterPipelineStatsDto(expectedKg, actualVolume, pipelineEntryIds.Count);
    }

    private async Task<(string? SchemaJson, string? FormName)> GetAccreditationFormMetaAsync(CancellationToken cancellationToken)
    {
        var template = await _db.FormTemplates
            .AsNoTracking()
            .Include(t => t.Versions)
            .Where(t => t.IsActive && t.Status == FormTemplateStatus.Published && t.FormType == "ACCREDITATION")
            .OrderByDescending(t => t.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null) return (null, null);

        var version = template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        return (version?.SchemaJson, template.Name);
    }

    private static DaImporterAccreditationDto MapAccreditation(
        AccreditationSubmission submission,
        string? formSchemaJson,
        string? formName,
        Certificate? certificate) =>
        new(
            submission.Uuid,
            submission.CompanyName,
            submission.SubmissionType,
            submission.Status.ToString(),
            AccreditationSubmissionStatusMapper.GetDisplayStatus(submission),
            submission.AccreditationNumber,
            submission.SubmittedAt,
            submission.ReviewComments,
            submission.AssignedOfficer?.Profile is not null
                ? $"{submission.AssignedOfficer.Profile.FirstName} {submission.AssignedOfficer.Profile.LastName}".Trim()
                : submission.AssignedOfficer?.Email,
            submission.ClaimedAt,
            submission.FormDataJson,
            formSchemaJson,
            formName,
            certificate?.Uuid,
            certificate?.CertificateNumber,
            submission.Status == AccreditationSubmissionStatus.Approved,
            submission.History
                .OrderByDescending(h => h.CreatedAt)
                .Select(AccreditationHistoryMapper.ForOfficer)
                .ToList(),
            submission.Files
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new DaImporterAccreditationFileDto(
                    f.Uuid,
                    f.OriginalFileName,
                    f.FileSizeBytes,
                    f.CreatedAt))
                .ToList());

    private static string FormatFullName(User user)
    {
        var fullName = user.Profile is not null
            ? $"{user.Profile.FirstName} {user.Profile.LastName}".Trim()
            : user.Email;
        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }

    private static Dictionary<string, string> ParseImporterFormData(string? formDataJson)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(formDataJson)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (System.Text.Json.JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

internal static class DaStockVolumeHelper
{
    public static decimal ToKilograms(Entry entry)
    {
        var quantity = entry.Detail?.Quantity ?? 0m;
        var micVolume = entry.MicUtilizations.Sum(u => u.Volume);
        var source = quantity > 0 ? quantity : micVolume;
        var normalized = (entry.Detail?.Unit ?? "kg").Trim().ToLowerInvariant();
        return normalized switch
        {
            "mt" or "m.t." or "metric ton" or "metric tons" or "ton" or "tons" or "tonne" or "tonnes" => source * 1000m,
            "g" or "gram" or "grams" => source / 1000m,
            _ => source
        };
    }
}
