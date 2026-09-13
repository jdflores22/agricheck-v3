using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.MavPortal;
using AgriCheck.Application.MavPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AgriCheck.Infrastructure.Services;

public class MavApplicationService : IMavApplicationService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMavNotificationService _mavNotifications;

    public MavApplicationService(AgriCheckDbContext db, ICurrentUserService currentUser, IMavNotificationService mavNotifications)
    {
        _db = db;
        _currentUser = currentUser;
        _mavNotifications = mavNotifications;
    }

    public async Task<IReadOnlyList<MavApplicationListItemDto>> ListMineAsync(CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        return await MapList(_db.MavApplications.Where(a => a.ImporterId == user.Id), cancellationToken);
    }

    public async Task<IReadOnlyList<MavApplicationListItemDto>> ListAdminAsync(string? status, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.MavApplications.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MavApplicationStatus>(status, true, out var parsed))
        {
            query = query.Where(a => a.Status == parsed);
        }

        return await MapList(query, cancellationToken);
    }

    public async Task<MavApplicationDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var app = await LoadApplication(uuid, cancellationToken);
        if (app is null) return null;
        if (!_currentUser.IsInRole("ROLE_MAV_ADMIN") && !_currentUser.IsInRole("ROLE_MAV_EVALUATOR")
            && !_currentUser.IsInRole("ROLE_MAV_SECRETARY") && !_currentUser.IsInRole("ROLE_ADMIN")
            && app.ImporterId != user.Id)
        {
            throw new ClientPortalException("FORBIDDEN", "Access denied.");
        }

        return MapDetail(app);
    }

    public async Task<MavApplicationDetailDto> CreateAsync(CreateMavApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var period = await _db.MavApplicationPeriods.FirstOrDefaultAsync(p => p.Uuid == request.PeriodUuid, cancellationToken)
            ?? throw new ClientPortalException("PERIOD_NOT_FOUND", "Application period not found.");
        if (period.Status != MavApplicationPeriodStatus.Open)
        {
            throw new ClientPortalException("PERIOD_CLOSED", "Application period is not open.");
        }

        if (request.RequestedVolume <= 0)
        {
            throw new ClientPortalException("INVALID_VOLUME", "Requested volume must be positive.");
        }

        var (hsCode, commodityName, hsDetailId) = await MavHsLibraryService.ResolveApplicationCommodityAsync(_db, request, cancellationToken, period.AgencyId);

        var app = new MavApplication
        {
            Uuid = Guid.NewGuid(),
            ReferenceNumber = await ReferenceNumberGenerator.MavApplicationAsync(_db, cancellationToken),
            ApplicationPeriodId = period.Id,
            ImporterId = user.Id,
            AccreditationSubmissionId = request.AccreditationSubmissionId,
            MavHsDetailId = hsDetailId,
            HsCode = hsCode,
            CommodityName = commodityName,
            RequestedVolume = request.RequestedVolume,
            Status = MavApplicationStatus.Draft
        };
        _db.MavApplications.Add(app);
        MavAuditHelper.Write(_db, user.Id, "application", app.Uuid.ToString(), "created");
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(await LoadApplication(app.Uuid, cancellationToken) ?? app);
    }

    public async Task<MavApplicationDetailDto> UpdateAsync(Guid uuid, UpdateMavApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var app = await _db.MavApplications.Include(a => a.ApplicationPeriod)
            .FirstOrDefaultAsync(a => a.Uuid == uuid && a.ImporterId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Application not found.");
        if (app.Status != MavApplicationStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only draft applications can be edited.");
        }

        if (request.RequestedVolume <= 0)
        {
            throw new ClientPortalException("INVALID_VOLUME", "Requested volume must be positive.");
        }

        var (hsCode, commodityName, hsDetailId) = await MavHsLibraryService.ResolveApplicationCommodityAsync(
            _db,
            new CreateMavApplicationRequest(app.ApplicationPeriod.Uuid, request.HsCode, request.CommodityName, request.RequestedVolume, request.AccreditationSubmissionId, request.HsDetailUuid),
            cancellationToken,
            app.ApplicationPeriod.AgencyId);

        app.HsCode = hsCode;
        app.CommodityName = commodityName;
        app.MavHsDetailId = hsDetailId;
        app.RequestedVolume = request.RequestedVolume;
        app.AccreditationSubmissionId = request.AccreditationSubmissionId;
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(await LoadApplication(app.Uuid, cancellationToken) ?? app);
    }

    public async Task<MavApplicationDetailDto> SubmitAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var app = await _db.MavApplications.Include(a => a.ApplicationPeriod)
            .FirstOrDefaultAsync(a => a.Uuid == uuid && a.ImporterId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Application not found.");
        if (app.Status != MavApplicationStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only draft applications can be submitted.");
        }

        if (app.ApplicationPeriod.Status != MavApplicationPeriodStatus.Open)
        {
            throw new ClientPortalException("PERIOD_CLOSED", "Application period is not open.");
        }

        app.Status = MavApplicationStatus.Submitted;
        app.SubmittedAt = DateTime.UtcNow;
        MavAuditHelper.Write(_db, user.Id, "application", app.Uuid.ToString(), "submitted");
        await _db.SaveChangesAsync(cancellationToken);
        await _mavNotifications.NotifyApplicationSubmittedAsync(app.ImporterId, app.Uuid, app.ReferenceNumber, app.CommodityName, cancellationToken);
        return MapDetail(await LoadApplication(app.Uuid, cancellationToken) ?? app);
    }

    public async Task<MavApplicationDetailDto> ApproveAsync(Guid uuid, ReviewMavApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        var app = await _db.MavApplications
            .Include(a => a.ApplicationPeriod)
            .Include(a => a.Importer)
            .FirstOrDefaultAsync(a => a.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Application not found.");

        if (app.Status is not MavApplicationStatus.Submitted and not MavApplicationStatus.UnderReview)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only submitted applications can be approved.");
        }

        if (request.AllocatedVolume <= 0)
        {
            throw new ClientPortalException("INVALID_VOLUME", "Allocated volume must be positive.");
        }

        var allocation = await _db.MavCommodityAllocations.FirstOrDefaultAsync(
            a => a.ApplicationPeriodId == app.ApplicationPeriodId && a.HsCode == app.HsCode, cancellationToken);
        if (allocation is not null && allocation.AllocatedVolume + request.AllocatedVolume > allocation.TotalVolume)
        {
            throw new ClientPortalException("INSUFFICIENT_POOL", "Allocated volume exceeds commodity pool.");
        }

        app.Status = MavApplicationStatus.Approved;
        app.AllocatedVolume = request.AllocatedVolume;
        app.ReviewedAt = DateTime.UtcNow;
        app.ReviewedByUserId = staff.Id;

        if (allocation is not null)
        {
            allocation.AllocatedVolume += request.AllocatedVolume;
        }

        var license = new MavLicense
        {
            Uuid = Guid.NewGuid(),
            LicenseNumber = await ReferenceNumberGenerator.MavLicenseAsync(_db, cancellationToken),
            ApplicationId = app.Id,
            ImporterId = app.ImporterId,
            MavYear = app.ApplicationPeriod.MavYear,
            HsCode = app.HsCode,
            CommodityName = app.CommodityName,
            AwardedVolume = request.AllocatedVolume,
            PoolType = app.ApplicationPeriod.PoolType,
            Status = MavLicenseStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = new DateTime(app.ApplicationPeriod.MavYear, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };
        var account = new MavAccount { AwardedVolume = request.AllocatedVolume, UtilizedVolume = 0 };
        license.Account = account;
        _db.MavLicenses.Add(license);

        MavAuditHelper.Write(_db, staff.Id, "application", app.Uuid.ToString(), "approved", payload: new { request.AllocatedVolume, license.LicenseNumber });
        await _db.SaveChangesAsync(cancellationToken);
        await _mavNotifications.NotifyApplicationApprovedAsync(app.ImporterId, app.Uuid, app.ReferenceNumber, license.LicenseNumber, request.AllocatedVolume, cancellationToken);
        return MapDetail(await LoadApplication(app.Uuid, cancellationToken) ?? app);
    }

    public async Task<MavApplicationDetailDto> RejectAsync(Guid uuid, RejectMavApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        var app = await _db.MavApplications.FirstOrDefaultAsync(a => a.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Application not found.");
        if (app.Status is not MavApplicationStatus.Submitted and not MavApplicationStatus.UnderReview)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only submitted applications can be rejected.");
        }

        app.Status = MavApplicationStatus.Rejected;
        app.RejectionReason = request.Reason.Trim();
        app.ReviewedAt = DateTime.UtcNow;
        app.ReviewedByUserId = staff.Id;
        MavAuditHelper.Write(_db, staff.Id, "application", app.Uuid.ToString(), "rejected", payload: new { request.Reason });
        await _db.SaveChangesAsync(cancellationToken);
        await _mavNotifications.NotifyApplicationRejectedAsync(app.ImporterId, app.Uuid, app.ReferenceNumber, request.Reason.Trim(), cancellationToken);
        return MapDetail(await LoadApplication(app.Uuid, cancellationToken) ?? app);
    }

    private async Task<MavApplication?> LoadApplication(Guid uuid, CancellationToken cancellationToken) =>
        await _db.MavApplications
            .Include(a => a.ApplicationPeriod).ThenInclude(p => p.Agency)
            .Include(a => a.Importer).ThenInclude(u => u.Profile)
            .Include(a => a.License)
            .FirstOrDefaultAsync(a => a.Uuid == uuid, cancellationToken);

    private async Task<IReadOnlyList<MavApplicationListItemDto>> MapList(IQueryable<MavApplication> query, CancellationToken cancellationToken) =>
        await query.Include(a => a.ApplicationPeriod).Include(a => a.Importer).ThenInclude(u => u.Profile)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new MavApplicationListItemDto(
                a.Uuid, a.ReferenceNumber, a.Status.ToString(), a.ApplicationPeriod.MavYear, a.ApplicationPeriod.PoolType.ToString(),
                a.HsCode, a.CommodityName, a.RequestedVolume, a.AllocatedVolume,
                a.Importer.Profile != null ? a.Importer.Profile.FirstName + " " + a.Importer.Profile.LastName : a.Importer.Email,
                a.SubmittedAt))
            .ToListAsync(cancellationToken);

    private static MavApplicationDetailDto MapDetail(MavApplication app) => new(
        app.Uuid, app.ReferenceNumber, app.Status.ToString(), app.ApplicationPeriod.Uuid, app.ApplicationPeriod.MavYear,
        app.ApplicationPeriod.PoolType.ToString(), app.HsCode, app.CommodityName, app.RequestedVolume, app.AllocatedVolume,
        app.SubmittedAt, app.ReviewedAt, app.RejectionReason, app.License?.Uuid,
        app.ApplicationPeriod.AgencyId, app.ApplicationPeriod.Agency?.Code);
}

public class MavLicenseService : IMavLicenseService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MavLicenseService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MavLicenseListItemDto>> ListMineAsync(CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        return await MapList(_db.MavLicenses.Include(l => l.Account).Include(l => l.Importer).ThenInclude(u => u.Profile).Where(l => l.ImporterId == user.Id), cancellationToken);
    }

    public async Task<IReadOnlyList<MavLicenseListItemDto>> ListAdminAsync(CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        return await MapList(_db.MavLicenses.Include(l => l.Account).Include(l => l.Importer).ThenInclude(u => u.Profile), cancellationToken);
    }

    public async Task<MavLicenseDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var license = await _db.MavLicenses
            .Include(l => l.Account).ThenInclude(a => a.Transactions)
            .Include(l => l.ImportCertificates)
            .Include(l => l.Importer).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(l => l.Uuid == uuid, cancellationToken);
        if (license is null) return null;
        if (!_currentUser.IsInRole("ROLE_MAV_ADMIN") && !_currentUser.IsInRole("ROLE_MAV_EVALUATOR")
            && !_currentUser.IsInRole("ROLE_MAV_SECRETARY") && !_currentUser.IsInRole("ROLE_ADMIN")
            && license.ImporterId != user.Id)
        {
            throw new ClientPortalException("FORBIDDEN", "Access denied.");
        }

        return MapDetail(license);
    }

    public async Task<MavLicenseListItemDto> RevokeAsync(Guid uuid, string reason, CancellationToken cancellationToken = default)
    {
        var admin = await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        var license = await _db.MavLicenses.Include(l => l.Account).Include(l => l.Importer).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(l => l.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "License not found.");
        license.Status = MavLicenseStatus.Revoked;
        license.RevokedReason = reason.Trim();
        MavAuditHelper.Write(_db, admin.Id, "license", license.Uuid.ToString(), "revoked", payload: new { reason });
        await _db.SaveChangesAsync(cancellationToken);
        return MapListItem(license);
    }

    private async Task<IReadOnlyList<MavLicenseListItemDto>> MapList(IQueryable<MavLicense> query, CancellationToken cancellationToken) =>
        await query.OrderByDescending(l => l.IssuedAt).Select(l => new MavLicenseListItemDto(
            l.Uuid, l.LicenseNumber, l.Status.ToString(), l.MavYear, l.PoolType.ToString(), l.HsCode, l.CommodityName,
            l.AwardedVolume, l.Account.AwardedVolume - l.Account.UtilizedVolume, l.IssuedAt, l.ExpiresAt,
            l.Importer.Profile != null ? l.Importer.Profile.FirstName + " " + l.Importer.Profile.LastName : l.Importer.Email))
            .ToListAsync(cancellationToken);

    private static MavLicenseListItemDto MapListItem(MavLicense l) => new(
        l.Uuid, l.LicenseNumber, l.Status.ToString(), l.MavYear, l.PoolType.ToString(), l.HsCode, l.CommodityName,
        l.AwardedVolume, l.Account.AwardedVolume - l.Account.UtilizedVolume, l.IssuedAt, l.ExpiresAt,
        l.Importer.Profile is not null ? $"{l.Importer.Profile.FirstName} {l.Importer.Profile.LastName}" : l.Importer.Email);

    private static MavLicenseDetailDto MapDetail(MavLicense license) => new(
        license.Uuid, license.LicenseNumber, license.Status.ToString(), license.MavYear, license.PoolType.ToString(),
        license.HsCode, license.CommodityName, license.AwardedVolume, license.Account.UtilizedVolume,
        license.Account.AwardedVolume - license.Account.UtilizedVolume, license.IssuedAt, license.ExpiresAt,
        license.ImportCertificates.OrderByDescending(m => m.IssuedAt).Select(MavMicMapper.MapListItem).ToList(),
        license.Account.Transactions.OrderByDescending(t => t.CreatedAt).Take(20).Select(t => new MavAccountTransactionDto(
            t.Id, t.TransactionType.ToString(), t.Volume, t.BalanceBefore, t.BalanceAfter, t.Reference, t.CreatedAt)).ToList());
}

internal static class MavMicMapper
{
    public static MavMicListItemDto MapListItem(MavImportCertificate m) => new(
        m.Uuid, m.CertificateNumber, m.Status.ToString(), m.HsCode, m.CommodityName,
        m.AuthorizedVolume, m.UtilizedVolume, m.AuthorizedVolume - m.UtilizedVolume, m.IssuedAt, m.ExpiresAt);
}

public class MavMicService : IMavMicService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMavNotificationService _mavNotifications;

    public MavMicService(AgriCheckDbContext db, ICurrentUserService currentUser, IMavNotificationService mavNotifications)
    {
        _db = db;
        _currentUser = currentUser;
        _mavNotifications = mavNotifications;
    }

    public async Task<MavMicListItemDto> IssueAsync(Guid licenseUuid, IssueMicRequest request, CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var license = await _db.MavLicenses.Include(l => l.Account).FirstOrDefaultAsync(l => l.Uuid == licenseUuid && l.ImporterId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "License not found.");
        if (license.Status != MavLicenseStatus.Active || license.ExpiresAt < DateTime.UtcNow)
        {
            throw new ClientPortalException("LICENSE_INACTIVE", "License is not active.");
        }

        var available = license.Account.AwardedVolume - license.Account.UtilizedVolume;
        if (request.Volume <= 0 || request.Volume > available)
        {
            throw new ClientPortalException("INSUFFICIENT_BALANCE", $"Insufficient account balance. Available: {available:0.###}");
        }

        var certNumber = await ReferenceNumberGenerator.MavMicAsync(_db, cancellationToken);
        var twoMonths = DateTime.UtcNow.AddMonths(2);
        var yearEnd = new DateTime(license.MavYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var expiresAt = twoMonths < yearEnd ? twoMonths : yearEnd;
        var balanceBefore = available;

        license.Account.UtilizedVolume += request.Volume;
        license.Account.LastTransactionAt = DateTime.UtcNow;
        var balanceAfter = license.Account.AwardedVolume - license.Account.UtilizedVolume;

        var mic = new MavImportCertificate
        {
            Uuid = Guid.NewGuid(),
            CertificateNumber = certNumber,
            LicenseId = license.Id,
            AccountId = license.Account.Id,
            ImporterId = user.Id,
            HsCode = license.HsCode,
            CommodityName = license.CommodityName,
            AuthorizedVolume = request.Volume,
            ExpiresAt = expiresAt
        };
        _db.MavImportCertificates.Add(mic);
        _db.MavAccountTransactions.Add(new MavAccountTransaction
        {
            AccountId = license.Account.Id,
            TransactionType = MavAccountTransactionType.MicIssuance,
            Volume = request.Volume,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Reference = certNumber,
            CreatedByUserId = user.Id
        });
        MavAuditHelper.Write(_db, user.Id, "mic", mic.Uuid.ToString(), "issued", payload: new { request.Volume, certNumber });
        await _db.SaveChangesAsync(cancellationToken);
        await _mavNotifications.NotifyMicIssuedAsync(user.Id, mic.Uuid, mic.CertificateNumber, request.Volume, cancellationToken);
        return MavMicMapper.MapListItem(mic);
    }

    public async Task<MavMicListItemDto> UtilizeAsync(Guid micUuid, UtilizeMicRequest request, CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var mic = await _db.MavImportCertificates.FirstOrDefaultAsync(m => m.Uuid == micUuid && m.ImporterId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "MIC not found.");
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Uuid == request.EntryUuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.Status != EntryStatus.Draft)
        {
            throw new ClientPortalException("ENTRY_NOT_EDITABLE", "MIC can only be linked to draft entries.");
        }

        if (entry.EntryType != EntryType.Import)
        {
            throw new ClientPortalException("INVALID_ENTRY_TYPE", "MIC utilization applies to import entries only.");
        }

        if (mic.Status == MavImportCertificateStatus.Expired || mic.ExpiresAt < DateTime.UtcNow)
        {
            throw new ClientPortalException("MIC_EXPIRED", "MIC has expired.");
        }

        var available = mic.AuthorizedVolume - mic.UtilizedVolume;
        if (request.Volume <= 0 || request.Volume > available)
        {
            throw new ClientPortalException("INSUFFICIENT_MIC", $"Insufficient MIC volume. Available: {available:0.###}");
        }

        mic.UtilizedVolume += request.Volume;
        mic.Status = mic.UtilizedVolume >= mic.AuthorizedVolume
            ? MavImportCertificateStatus.FullyUtilized
            : MavImportCertificateStatus.PartiallyUtilized;

        _db.MicUtilizations.Add(new MicUtilization { MicId = mic.Id, EntryId = entry.Id, Volume = request.Volume, UtilizedAt = DateTime.UtcNow });
        MavAuditHelper.Write(_db, user.Id, "mic", mic.Uuid.ToString(), "utilized", payload: new { request.Volume, entry.Uuid });
        await _db.SaveChangesAsync(cancellationToken);
        return MavMicMapper.MapListItem(mic);
    }

    public async Task<IReadOnlyList<MavMicListItemDto>> ListAvailableAsync(CancellationToken cancellationToken = default)
    {
        var user = await MavContextHelper.RequireImporterAsync(_db, _currentUser, cancellationToken);
        var now = DateTime.UtcNow;
        return await _db.MavImportCertificates
            .Where(m =>
                m.ImporterId == user.Id
                && m.Status != MavImportCertificateStatus.Expired
                && m.ExpiresAt >= now
                && m.UtilizedVolume < m.AuthorizedVolume)
            .OrderByDescending(m => m.IssuedAt)
            .Select(m => new MavMicListItemDto(
                m.Uuid, m.CertificateNumber, m.Status.ToString(), m.HsCode, m.CommodityName,
                m.AuthorizedVolume, m.UtilizedVolume, m.AuthorizedVolume - m.UtilizedVolume, m.IssuedAt, m.ExpiresAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MavMicListItemDto>> ListAdminAsync(CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        return await _db.MavImportCertificates.OrderByDescending(m => m.IssuedAt)
            .Select(m => new MavMicListItemDto(
                m.Uuid, m.CertificateNumber, m.Status.ToString(), m.HsCode, m.CommodityName,
                m.AuthorizedVolume, m.UtilizedVolume, m.AuthorizedVolume - m.UtilizedVolume, m.IssuedAt, m.ExpiresAt))
            .ToListAsync(cancellationToken);
    }
}

public class MavComplianceService : IMavComplianceService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MavComplianceService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MavComplianceDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        return await BuildSummaryAsync(cancellationToken);
    }

    public async Task<MavComplianceAlertsDto> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(30);
        var summary = await BuildSummaryAsync(cancellationToken);

        var expiringLicensesRaw = await _db.MavLicenses
            .Include(l => l.Importer).ThenInclude(u => u.Profile)
            .Include(l => l.Account)
            .Where(l => l.Status == MavLicenseStatus.Active && l.ExpiresAt >= now && l.ExpiresAt <= expiringThreshold)
            .OrderBy(l => l.ExpiresAt)
            .ToListAsync(cancellationToken);
        var expiringLicenses = expiringLicensesRaw.Select(l => new MavExpiringLicenseAlertDto(
            l.Uuid,
            l.LicenseNumber,
            ResolveHolderName(l.Importer),
            l.HsCode,
            l.CommodityName,
            l.Account.AwardedVolume - l.Account.UtilizedVolume,
            l.ExpiresAt,
            Math.Max(0, (int)Math.Ceiling((l.ExpiresAt - now).TotalDays)))).ToList();

        var expiredLicenses = await _db.MavLicenses
            .Include(l => l.Importer).ThenInclude(u => u.Profile)
            .Where(l => l.Status == MavLicenseStatus.Active && l.ExpiresAt < now)
            .OrderByDescending(l => l.ExpiresAt)
            .Select(l => new MavExpiredLicenseAlertDto(
                l.Uuid,
                l.LicenseNumber,
                l.Importer.Profile != null ? $"{l.Importer.Profile.FirstName} {l.Importer.Profile.LastName}".Trim() : l.Importer.Email,
                l.HsCode,
                l.CommodityName,
                l.ExpiresAt))
            .ToListAsync(cancellationToken);

        var expiringMicsRaw = await _db.MavImportCertificates
            .Include(m => m.Importer).ThenInclude(u => u.Profile)
            .Where(m => m.Status != MavImportCertificateStatus.Expired && m.ExpiresAt >= now && m.ExpiresAt <= expiringThreshold)
            .OrderBy(m => m.ExpiresAt)
            .ToListAsync(cancellationToken);
        var expiringMics = expiringMicsRaw.Select(m => new MavExpiringMicAlertDto(
            m.Uuid,
            m.CertificateNumber,
            ResolveHolderName(m.Importer),
            m.HsCode,
            m.CommodityName,
            m.AuthorizedVolume - m.UtilizedVolume,
            m.ExpiresAt,
            Math.Max(0, (int)Math.Ceiling((m.ExpiresAt - now).TotalDays)))).ToList();

        var expiredMics = await _db.MavImportCertificates
            .Include(m => m.Importer).ThenInclude(u => u.Profile)
            .Where(m => m.Status != MavImportCertificateStatus.Expired && m.ExpiresAt < now)
            .OrderByDescending(m => m.ExpiresAt)
            .Select(m => new MavExpiredMicAlertDto(
                m.Uuid,
                m.CertificateNumber,
                m.Importer.Profile != null ? $"{m.Importer.Profile.FirstName} {m.Importer.Profile.LastName}".Trim() : m.Importer.Email,
                m.HsCode,
                m.CommodityName,
                m.ExpiresAt))
            .ToListAsync(cancellationToken);

        var lowUtilization = await _db.MavLicenses
            .Include(l => l.Importer).ThenInclude(u => u.Profile)
            .Include(l => l.Account)
            .Where(l => l.Status == MavLicenseStatus.Active && l.Account.AwardedVolume > 0)
            .Where(l => l.Account.UtilizedVolume / l.Account.AwardedVolume < 0.2m)
            .OrderBy(l => l.Account.UtilizedVolume / l.Account.AwardedVolume)
            .Select(l => new MavLowUtilizationAlertDto(
                l.Uuid,
                l.LicenseNumber,
                l.Importer.Profile != null ? $"{l.Importer.Profile.FirstName} {l.Importer.Profile.LastName}".Trim() : l.Importer.Email,
                l.HsCode,
                l.CommodityName,
                l.Account.AwardedVolume,
                l.Account.UtilizedVolume,
                l.Account.AwardedVolume == 0 ? 0 : Math.Round(l.Account.UtilizedVolume / l.Account.AwardedVolume * 100m, 1)))
            .ToListAsync(cancellationToken);

        var overUtilized = await _db.MavLicenses
            .Include(l => l.Importer).ThenInclude(u => u.Profile)
            .Include(l => l.Account)
            .Where(l => l.Account.UtilizedVolume > l.Account.AwardedVolume)
            .OrderByDescending(l => l.Account.UtilizedVolume - l.Account.AwardedVolume)
            .Select(l => new MavOverUtilizedAlertDto(
                l.Uuid,
                l.LicenseNumber,
                l.Importer.Profile != null ? $"{l.Importer.Profile.FirstName} {l.Importer.Profile.LastName}".Trim() : l.Importer.Email,
                l.HsCode,
                l.CommodityName,
                l.Account.AwardedVolume,
                l.Account.UtilizedVolume))
            .ToListAsync(cancellationToken);

        return new MavComplianceAlertsDto(
            summary,
            expiringLicenses,
            expiringMics,
            lowUtilization,
            overUtilized,
            expiredLicenses,
            expiredMics);
    }

    private async Task<MavComplianceDashboardDto> BuildSummaryAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(30);
        return new MavComplianceDashboardDto(
            await _db.MavLicenses.CountAsync(l => l.Status == MavLicenseStatus.Active && l.ExpiresAt < now, cancellationToken),
            await _db.MavImportCertificates.CountAsync(m => m.Status != MavImportCertificateStatus.Expired && m.ExpiresAt < now, cancellationToken),
            await _db.MavLicenses.CountAsync(l => l.Status == MavLicenseStatus.Active && l.ExpiresAt >= now && l.ExpiresAt <= expiringThreshold, cancellationToken),
            await _db.MavImportCertificates.CountAsync(m => m.Status != MavImportCertificateStatus.Expired && m.ExpiresAt >= now && m.ExpiresAt <= expiringThreshold, cancellationToken),
            await _db.MavLicenses
                .Include(l => l.Account)
                .CountAsync(l => l.Status == MavLicenseStatus.Active && l.Account.AwardedVolume > 0 && l.Account.UtilizedVolume / l.Account.AwardedVolume < 0.2m, cancellationToken),
            await _db.MavAccounts.CountAsync(a => a.UtilizedVolume > a.AwardedVolume, cancellationToken),
            await _db.MavApplications.CountAsync(a => a.Status == MavApplicationStatus.Submitted || a.Status == MavApplicationStatus.UnderReview, cancellationToken));
    }

    private static string ResolveHolderName(User user) =>
        user.Profile is not null ? $"{user.Profile.FirstName} {user.Profile.LastName}".Trim() : user.Email;
}

public class MavReportService : IMavReportService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MavReportService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MavReportSummaryDto> GetSummaryAsync(int? mavYear, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        return await BuildSummaryAsync(mavYear, cancellationToken);
    }

    public async Task<MavReportDetailDto> GetDetailAsync(int? mavYear, CancellationToken cancellationToken = default)
    {
        await MavContextHelper.RequireMavStaffAsync(_db, _currentUser, cancellationToken);
        var summary = await BuildSummaryAsync(mavYear, cancellationToken);
        var apps = FilterApplications(mavYear);
        var licenses = FilterLicenses(mavYear);

        var byCommodity = await apps
            .GroupBy(a => new { a.HsCode, a.CommodityName })
            .Select(g => new MavReportCommodityRowDto(
                g.Key.HsCode,
                g.Key.CommodityName,
                g.Count(),
                0,
                0m,
                0m))
            .ToListAsync(cancellationToken);

        var licenseGroups = await licenses
            .Include(l => l.Account)
            .GroupBy(l => new { l.HsCode, l.CommodityName })
            .Select(g => new
            {
                g.Key.HsCode,
                g.Key.CommodityName,
                ActiveLicenses = g.Count(l => l.Status == MavLicenseStatus.Active),
                AwardedVolume = g.Sum(l => l.AwardedVolume),
                UtilizedVolume = g.Sum(l => l.Account.UtilizedVolume)
            })
            .ToListAsync(cancellationToken);

        var commodityRows = byCommodity
            .Select(row =>
            {
                var licenseRow = licenseGroups.FirstOrDefault(l => l.HsCode == row.HsCode && l.CommodityName == row.CommodityName);
                return row with
                {
                    ActiveLicenses = licenseRow?.ActiveLicenses ?? 0,
                    AwardedVolume = licenseRow?.AwardedVolume ?? 0m,
                    UtilizedVolume = licenseRow?.UtilizedVolume ?? 0m
                };
            })
            .OrderByDescending(r => r.AwardedVolume)
            .ToList();

        var byPool = await apps
            .Include(a => a.ApplicationPeriod)
            .GroupBy(a => a.ApplicationPeriod.PoolType)
            .Select(g => new MavReportPoolRowDto(
                g.Key.ToString(),
                g.Count(),
                0,
                0m,
                0m))
            .ToListAsync(cancellationToken);

        var poolLicenseGroups = await licenses
            .Include(l => l.Account)
            .GroupBy(l => l.PoolType)
            .Select(g => new
            {
                PoolType = g.Key.ToString(),
                ActiveLicenses = g.Count(l => l.Status == MavLicenseStatus.Active),
                AwardedVolume = g.Sum(l => l.AwardedVolume),
                UtilizedVolume = g.Sum(l => l.Account.UtilizedVolume)
            })
            .ToListAsync(cancellationToken);

        var poolRows = byPool
            .Select(row =>
            {
                var licenseRow = poolLicenseGroups.FirstOrDefault(l => l.PoolType == row.PoolType);
                return row with
                {
                    ActiveLicenses = licenseRow?.ActiveLicenses ?? 0,
                    AwardedVolume = licenseRow?.AwardedVolume ?? 0m,
                    UtilizedVolume = licenseRow?.UtilizedVolume ?? 0m
                };
            })
            .ToList();

        return new MavReportDetailDto(summary, commodityRows, poolRows);
    }

    public async Task<byte[]> ExportCsvAsync(int? mavYear, CancellationToken cancellationToken = default)
    {
        var detail = await GetDetailAsync(mavYear, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("Section,Key,Metric,Value");
        sb.AppendLine($"Summary,Total Applications,,{detail.Summary.TotalApplications}");
        sb.AppendLine($"Summary,Approved Applications,,{detail.Summary.ApprovedApplications}");
        sb.AppendLine($"Summary,Rejected Applications,,{detail.Summary.RejectedApplications}");
        sb.AppendLine($"Summary,Active Licenses,,{detail.Summary.ActiveLicenses}");
        sb.AppendLine($"Summary,Issued MICs,,{detail.Summary.IssuedMics}");
        sb.AppendLine($"Summary,Total Awarded Volume,,{detail.Summary.TotalAwardedVolume}");
        sb.AppendLine($"Summary,Total Utilized Volume,,{detail.Summary.TotalUtilizedVolume}");
        foreach (var row in detail.ByCommodity)
        {
            sb.AppendLine($"Commodity,{row.HsCode} - {row.CommodityName},Applications,{row.ApplicationCount}");
            sb.AppendLine($"Commodity,{row.HsCode} - {row.CommodityName},Active Licenses,{row.ActiveLicenses}");
            sb.AppendLine($"Commodity,{row.HsCode} - {row.CommodityName},Awarded Volume,{row.AwardedVolume}");
            sb.AppendLine($"Commodity,{row.HsCode} - {row.CommodityName},Utilized Volume,{row.UtilizedVolume}");
        }

        foreach (var row in detail.ByPoolType)
        {
            sb.AppendLine($"Pool,{row.PoolType},Applications,{row.ApplicationCount}");
            sb.AppendLine($"Pool,{row.PoolType},Active Licenses,{row.ActiveLicenses}");
            sb.AppendLine($"Pool,{row.PoolType},Awarded Volume,{row.AwardedVolume}");
            sb.AppendLine($"Pool,{row.PoolType},Utilized Volume,{row.UtilizedVolume}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportPdfAsync(int? mavYear, CancellationToken cancellationToken = default)
    {
        var detail = await GetDetailAsync(mavYear, cancellationToken);
        return MavReportPdfGenerator.Generate(detail, mavYear);
    }

    private IQueryable<MavApplication> FilterApplications(int? mavYear)
    {
        var apps = _db.MavApplications.AsQueryable();
        if (mavYear is not null)
        {
            apps = apps.Where(a => a.ApplicationPeriod.MavYear == mavYear);
        }

        return apps;
    }

    private IQueryable<MavLicense> FilterLicenses(int? mavYear)
    {
        var licenses = _db.MavLicenses.AsQueryable();
        if (mavYear is not null)
        {
            licenses = licenses.Where(l => l.MavYear == mavYear);
        }

        return licenses;
    }

    private async Task<MavReportSummaryDto> BuildSummaryAsync(int? mavYear, CancellationToken cancellationToken)
    {
        var apps = FilterApplications(mavYear);
        var licenses = FilterLicenses(mavYear);
        return new MavReportSummaryDto(
            await apps.CountAsync(cancellationToken),
            await apps.CountAsync(a => a.Status == MavApplicationStatus.Approved, cancellationToken),
            await apps.CountAsync(a => a.Status == MavApplicationStatus.Rejected, cancellationToken),
            await licenses.CountAsync(l => l.Status == MavLicenseStatus.Active, cancellationToken),
            await _db.MavImportCertificates.CountAsync(cancellationToken),
            await licenses.SumAsync(l => l.AwardedVolume, cancellationToken),
            await _db.MavAccounts.SumAsync(a => a.UtilizedVolume, cancellationToken));
    }
}

public class MavYearTransitionService : IMavYearTransitionService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MavYearTransitionService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MavYearTransitionResultDto> TransitionAsync(MavYearTransitionRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await MavContextHelper.RequireMavAdminAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<MavPoolType>(request.PoolType, true, out var poolType))
        {
            throw new ClientPortalException("INVALID_POOL", "Pool type must be BYP or MYP.");
        }

        var sourcePeriods = await _db.MavApplicationPeriods
            .Include(p => p.CommodityAllocations)
            .Where(p => p.MavYear == request.FromYear && p.PoolType == poolType)
            .ToListAsync(cancellationToken);

        var periodsCreated = 0;
        var allocationsCopied = 0;
        foreach (var source in sourcePeriods)
        {
            if (await _db.MavApplicationPeriods.AnyAsync(p => p.MavYear == request.ToYear && p.PoolType == poolType && p.AgencyId == source.AgencyId, cancellationToken))
            {
                continue;
            }

            var period = new MavApplicationPeriod
            {
                Uuid = Guid.NewGuid(),
                MavYear = request.ToYear,
                PoolType = poolType,
                OpeningDate = new DateTime(request.ToYear, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ClosingDate = new DateTime(request.ToYear, 3, 31, 23, 59, 59, DateTimeKind.Utc),
                Status = MavApplicationPeriodStatus.Upcoming,
                AgencyId = source.AgencyId,
                CommodityAllocations = source.CommodityAllocations.Select(a => new MavCommodityAllocation
                {
                    CommodityId = a.CommodityId,
                    HsCode = a.HsCode,
                    CommodityName = a.CommodityName,
                    TotalVolume = a.TotalVolume,
                    AllocatedVolume = 0,
                    MinimumImportVolume = a.MinimumImportVolume,
                    AgencyId = a.AgencyId
                }).ToList()
            };
            _db.MavApplicationPeriods.Add(period);
            periodsCreated++;
            allocationsCopied += period.CommodityAllocations.Count;
        }

        MavAuditHelper.Write(_db, admin.Id, "year_transition", $"{request.FromYear}->{request.ToYear}", "completed",
            payload: new { request.FromYear, request.ToYear, request.PoolType, periodsCreated, allocationsCopied });
        await _db.SaveChangesAsync(cancellationToken);
        return new MavYearTransitionResultDto(periodsCreated, allocationsCopied);
    }
}
