using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.DaPortal;
using AgriCheck.Application.DaPortal.Dtos;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class DaWarehouseLtoApplicationService : IDaWarehouseLtoApplicationService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDaWarehouseManagementService _warehouseManagement;

    public DaWarehouseLtoApplicationService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IDaWarehouseManagementService warehouseManagement)
    {
        _db = db;
        _currentUser = currentUser;
        _warehouseManagement = warehouseManagement;
    }

    public async Task<IReadOnlyList<DaWarehouseLtoApplicationListItemDto>> ListPendingAsync(
        CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var items = await _db.WarehouseOperatingLicenseApplications.AsNoTracking()
            .Include(a => a.WarehouseFacility)
            .Include(a => a.SubmittedByUser)
            .Where(a => a.Status == WarehouseOperatingLicenseApplicationStatus.Pending)
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);

        return items.Select(MapListItem).ToList();
    }

    public async Task<IReadOnlyList<DaWarehouseLtoApplicationListItemDto>> ListRecentAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var take = Math.Clamp(limit, 1, 100);
        var items = await _db.WarehouseOperatingLicenseApplications.AsNoTracking()
            .Include(a => a.WarehouseFacility)
            .Include(a => a.SubmittedByUser)
            .OrderByDescending(a => a.SubmittedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return items.Select(MapListItem).ToList();
    }

    public async Task<WarehouseLtoApplicationDto> ResolveAsync(
        Guid applicationUuid,
        ResolveWarehouseLtoApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewer = await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var application = await _db.WarehouseOperatingLicenseApplications
            .Include(a => a.WarehouseFacility)
            .FirstOrDefaultAsync(a => a.Uuid == applicationUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "LTO application not found.");

        if (application.Status != WarehouseOperatingLicenseApplicationStatus.Pending)
        {
            throw new ClientPortalException("INVALID_STATUS", "This application has already been resolved.");
        }

        application.ReviewedByUserId = reviewer.Id;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewComments = string.IsNullOrWhiteSpace(request.ReviewComments)
            ? null
            : request.ReviewComments.Trim();

        if (!request.Approve)
        {
            application.Status = WarehouseOperatingLicenseApplicationStatus.Rejected;
            await _db.SaveChangesAsync(cancellationToken);
            return MapApplication(application);
        }

        if (request.IssueLicense is null)
        {
            throw new ClientPortalException("VALIDATION", "License details are required when approving an LTO application.");
        }

        var license = await _warehouseManagement.IssueOperatingLicenseAsync(
            application.WarehouseFacilityId,
            request.IssueLicense,
            cancellationToken);

        var licenseEntity = await _db.WarehouseOperatingLicenses
            .FirstOrDefaultAsync(
                l => l.WarehouseFacilityId == application.WarehouseFacilityId
                     && l.LicenseNumber == license.LicenseNumber,
                cancellationToken);

        application.Status = WarehouseOperatingLicenseApplicationStatus.Approved;
        application.IssuedLicenseId = licenseEntity?.Id;
        await _db.SaveChangesAsync(cancellationToken);

        return MapApplication(application);
    }

    private static DaWarehouseLtoApplicationListItemDto MapListItem(WarehouseOperatingLicenseApplication application) =>
        new(
            application.Uuid,
            application.WarehouseFacilityId,
            application.WarehouseFacility.Code,
            application.WarehouseFacility.Name,
            application.OperatorLegalName,
            application.Status.ToString(),
            application.SubmittedAt,
            application.SubmittedByUser.Email);

    private static WarehouseLtoApplicationDto MapApplication(WarehouseOperatingLicenseApplication application) =>
        new(
            application.Uuid,
            application.WarehouseFacilityId,
            application.WarehouseFacility.Code,
            application.WarehouseFacility.Name,
            application.OperatorLegalName,
            application.ContactEmail,
            application.ContactPhone,
            application.Notes,
            application.Status.ToString(),
            application.SubmittedAt,
            application.ReviewComments,
            application.ReviewedAt);
}
