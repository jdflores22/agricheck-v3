using AgriCheck.Application.ClientPortal;
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

        var inventory = await _db.WarehouseInventories
            .AsNoTracking()
            .Include(i => i.Container).ThenInclude(c => c.Entry)
            .Include(i => i.ReceivedBy).ThenInclude(u => u.Profile)
            .Where(i => i.WarehouseFacilityId == id)
            .OrderByDescending(i => i.Status == WarehouseInventoryStatus.Stored)
            .ThenByDescending(i => i.ReceivedAt)
            .Take(100)
            .Select(i => new DaWarehouseInventoryItemDto(
                i.Uuid,
                i.Container.ContainerNumber,
                i.Container.ContainerType,
                i.Container.Entry.ReferenceNo,
                i.LocationCode,
                i.Status.ToString(),
                i.ReceivedAt,
                i.ReceivedBy.Profile != null
                    ? (i.ReceivedBy.Profile.FirstName + " " + i.ReceivedBy.Profile.LastName).Trim()
                    : i.ReceivedBy.Email))
            .ToListAsync(cancellationToken);

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
