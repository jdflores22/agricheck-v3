using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.MavPortal;
using AgriCheck.Application.MavPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class MavDashboardService : IMavDashboardService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MavDashboardService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MavImporterDashboardDto> GetImporterDashboardAsync(CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var openPeriods = await _db.MavApplicationPeriods.CountAsync(p => p.Status == MavApplicationPeriodStatus.Open, cancellationToken);
        var apps = _db.MavApplications.Where(a => a.ImporterId == user.Id);
        var licenses = _db.MavLicenses.Include(l => l.Account).Where(l => l.ImporterId == user.Id && l.Status == MavLicenseStatus.Active);
        var mics = _db.MavImportCertificates.Where(m => m.ImporterId == user.Id && m.Status != MavImportCertificateStatus.Expired);
        return new MavImporterDashboardDto(
            openPeriods,
            await apps.CountAsync(cancellationToken),
            await apps.CountAsync(a => a.Status == MavApplicationStatus.Submitted || a.Status == MavApplicationStatus.UnderReview, cancellationToken),
            await licenses.CountAsync(cancellationToken),
            await mics.CountAsync(cancellationToken),
            await licenses.SumAsync(l => l.Account.AwardedVolume - l.Account.UtilizedVolume, cancellationToken));
    }

    public async Task<MavAdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        var expiredLicenses = await _db.MavLicenses.CountAsync(l => l.Status == MavLicenseStatus.Active && l.ExpiresAt < DateTime.UtcNow, cancellationToken);
        var expiredMics = await _db.MavImportCertificates.CountAsync(m => m.Status != MavImportCertificateStatus.Expired && m.ExpiresAt < DateTime.UtcNow, cancellationToken);
        var overUtilized = await _db.MavAccounts.CountAsync(a => a.UtilizedVolume > a.AwardedVolume, cancellationToken);
        return new MavAdminDashboardDto(
            await _db.MavApplicationPeriods.CountAsync(p => p.Status == MavApplicationPeriodStatus.Open, cancellationToken),
            await _db.MavApplications.CountAsync(a => a.Status == MavApplicationStatus.Submitted || a.Status == MavApplicationStatus.UnderReview, cancellationToken),
            await _db.MavLicenses.CountAsync(l => l.Status == MavLicenseStatus.Active, cancellationToken),
            await _db.MavImportCertificates.CountAsync(m => m.Status == MavImportCertificateStatus.Active || m.Status == MavImportCertificateStatus.PartiallyUtilized, cancellationToken),
            await _db.MavApplications.CountAsync(cancellationToken),
            expiredLicenses + expiredMics + overUtilized);
    }
}

public class MavApplicationPeriodService : IMavApplicationPeriodService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MavApplicationPeriodService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MavApplicationPeriodListItemDto>> ListOpenAsync(CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        return await MapList(_db.MavApplicationPeriods.Where(p => p.Status == MavApplicationPeriodStatus.Open), cancellationToken);
    }

    public async Task<IReadOnlyList<MavApplicationPeriodListItemDto>> ListAdminAsync(CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        return await MapList(_db.MavApplicationPeriods, cancellationToken);
    }

    public async Task<MavApplicationPeriodDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var period = await _db.MavApplicationPeriods.Include(p => p.CommodityAllocations).FirstOrDefaultAsync(p => p.Uuid == uuid, cancellationToken);
        return period is null ? null : MapDetail(period);
    }

    public async Task<MavApplicationPeriodDetailDto> CreateAsync(CreateMavApplicationPeriodRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<MavPoolType>(request.PoolType, true, out var poolType))
        {
            throw new ClientPortalException("INVALID_POOL", "Pool type must be BYP or MYP.");
        }

        var period = new MavApplicationPeriod
        {
            Uuid = Guid.NewGuid(),
            MavYear = request.MavYear,
            PoolType = poolType,
            OpeningDate = request.OpeningDate,
            ClosingDate = request.ClosingDate,
            AgencyId = request.AgencyId,
            Status = MavApplicationPeriodStatus.Upcoming
        };
        _db.MavApplicationPeriods.Add(period);
        MavAuditHelper.Write(_db, admin.Id, "application_period", period.Uuid.ToString(), "created");
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(await _db.MavApplicationPeriods.Include(p => p.CommodityAllocations).FirstAsync(p => p.Id == period.Id, cancellationToken));
    }

    public async Task<MavApplicationPeriodDetailDto> OpenAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var admin = await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var period = await _db.MavApplicationPeriods.Include(p => p.CommodityAllocations).FirstOrDefaultAsync(p => p.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Application period not found.");
        period.Status = MavApplicationPeriodStatus.Open;
        MavAuditHelper.Write(_db, admin.Id, "application_period", period.Uuid.ToString(), "opened");
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(period);
    }

    public async Task<MavApplicationPeriodDetailDto> CloseAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var admin = await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var period = await _db.MavApplicationPeriods.Include(p => p.CommodityAllocations).FirstOrDefaultAsync(p => p.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Application period not found.");
        period.Status = MavApplicationPeriodStatus.Closed;
        MavAuditHelper.Write(_db, admin.Id, "application_period", period.Uuid.ToString(), "closed");
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(period);
    }

    public async Task<MavCommodityAllocationDto> UpsertAllocationAsync(Guid periodUuid, UpsertMavCommodityAllocationRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var period = await _db.MavApplicationPeriods.FirstOrDefaultAsync(p => p.Uuid == periodUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Application period not found.");
        var commodity = await _db.Commodities.FirstOrDefaultAsync(c => c.Id == request.CommodityId, cancellationToken)
            ?? throw new ClientPortalException("COMMODITY_NOT_FOUND", "Commodity not found.");

        var allocation = await _db.MavCommodityAllocations.FirstOrDefaultAsync(a => a.ApplicationPeriodId == period.Id && a.HsCode == request.HsCode, cancellationToken);
        if (allocation is null)
        {
            allocation = new MavCommodityAllocation
            {
                ApplicationPeriodId = period.Id,
                CommodityId = commodity.Id,
                HsCode = request.HsCode,
                CommodityName = request.CommodityName,
                TotalVolume = request.TotalVolume,
                MinimumImportVolume = request.MinimumImportVolume,
                AgencyId = request.AgencyId
            };
            _db.MavCommodityAllocations.Add(allocation);
        }
        else
        {
            allocation.TotalVolume = request.TotalVolume;
            allocation.CommodityName = request.CommodityName;
            allocation.MinimumImportVolume = request.MinimumImportVolume;
            allocation.AgencyId = request.AgencyId;
        }

        MavAuditHelper.Write(_db, admin.Id, "commodity_allocation", allocation.Id.ToString(), "upserted");
        await _db.SaveChangesAsync(cancellationToken);
        return MapAllocation(allocation);
    }

    private async Task<IReadOnlyList<MavApplicationPeriodListItemDto>> MapList(IQueryable<MavApplicationPeriod> query, CancellationToken cancellationToken) =>
        await query.OrderByDescending(p => p.MavYear).Select(p => new MavApplicationPeriodListItemDto(
            p.Uuid, p.MavYear, p.PoolType.ToString(), p.Status.ToString(), p.OpeningDate, p.ClosingDate,
            p.Applications.Count, p.CommodityAllocations.Count)).ToListAsync(cancellationToken);

    private static MavApplicationPeriodDetailDto MapDetail(MavApplicationPeriod period) => new(
        period.Uuid, period.MavYear, period.PoolType.ToString(), period.Status.ToString(), period.OpeningDate, period.ClosingDate,
        period.CommodityAllocations.Select(MapAllocation).ToList());

    private static MavCommodityAllocationDto MapAllocation(MavCommodityAllocation a) => new(
        a.Id, a.HsCode, a.CommodityName, a.TotalVolume, a.AllocatedVolume, a.TotalVolume - a.AllocatedVolume, a.MinimumImportVolume);
}
