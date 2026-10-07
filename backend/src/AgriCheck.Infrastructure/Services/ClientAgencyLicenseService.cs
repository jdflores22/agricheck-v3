using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class ClientAgencyLicenseService : IClientAgencyLicenseService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClientAgencyLicenseService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AgencyLicenseTypeOptionDto>> ListLicenseTypesAsync(long? agencyId, CancellationToken cancellationToken = default)
    {
        var query = _db.AgencyLicenseTypes
            .AsNoTracking()
            .Include(t => t.Agency)
            .Where(t => t.Agency.IsActive);

        if (agencyId is long id)
        {
            query = query.Where(t => t.AgencyId == id);
        }

        var rows = await query
            .OrderBy(t => t.Agency.Code)
            .ThenBy(t => t.Code)
            .ToListAsync(cancellationToken);

        return rows.Select(MapType).ToList();
    }

    public async Task<ClientAgencyLicenseOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var now = DateTime.UtcNow;

        var licenses = await _db.AgencyLicenses
            .AsNoTracking()
            .Include(l => l.Agency)
            .Include(l => l.LicenseType)
            .Where(l => l.UserId == user.Id)
            .OrderByDescending(l => l.ExpiresAt)
            .ToListAsync(cancellationToken);

        var applications = await _db.AgencyLicenseApplications
            .AsNoTracking()
            .Include(a => a.Agency)
            .Include(a => a.LicenseType)
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return new ClientAgencyLicenseOverviewDto(
            licenses.Select(l => MapLicense(l, now)).ToList(),
            applications.Select(MapApplicationListItem).ToList());
    }

    public async Task<ClientAgencyLicenseApplicationDto?> GetApplicationAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var application = await _db.AgencyLicenseApplications
            .AsNoTracking()
            .Include(a => a.Agency)
            .Include(a => a.LicenseType)
            .FirstOrDefaultAsync(a => a.Uuid == uuid && a.UserId == user.Id, cancellationToken);

        return application is null ? null : MapApplication(application);
    }

    public async Task<ClientAgencyLicenseApplicationDto> CreateApplicationAsync(
        CreateAgencyLicenseApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var licenseType = await RequireLicenseTypeAsync(request.AgencyId, request.AgencyLicenseTypeId, cancellationToken);

        var hasDraft = await _db.AgencyLicenseApplications.AnyAsync(
            a => a.UserId == user.Id
                && a.AgencyId == request.AgencyId
                && a.AgencyLicenseTypeId == request.AgencyLicenseTypeId
                && a.Status == AgencyLicenseApplicationStatus.Draft,
            cancellationToken);

        if (hasDraft)
        {
            throw new ClientPortalException(
                "DRAFT_EXISTS",
                "You already have a draft application for this bureau license type. Open and submit it instead.");
        }

        var application = new AgencyLicenseApplication
        {
            Uuid = Guid.NewGuid(),
            UserId = user.Id,
            AgencyId = licenseType.AgencyId,
            AgencyLicenseTypeId = licenseType.Id,
            Status = AgencyLicenseApplicationStatus.Draft,
            FormDataJson = request.FormDataJson,
        };

        _db.AgencyLicenseApplications.Add(application);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(application).Reference(a => a.Agency).LoadAsync(cancellationToken);
        await _db.Entry(application).Reference(a => a.LicenseType).LoadAsync(cancellationToken);
        return MapApplication(application);
    }

    public async Task<ClientAgencyLicenseApplicationDto> UpdateApplicationAsync(
        Guid uuid,
        UpdateAgencyLicenseApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var application = await _db.AgencyLicenseApplications
            .Include(a => a.Agency)
            .Include(a => a.LicenseType)
            .FirstOrDefaultAsync(a => a.Uuid == uuid && a.UserId == user.Id, cancellationToken);

        if (application is null)
        {
            throw new ClientPortalException("NOT_FOUND", "Application not found.");
        }

        if (application.Status != AgencyLicenseApplicationStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only draft applications can be edited.");
        }

        application.FormDataJson = request.FormDataJson;
        await _db.SaveChangesAsync(cancellationToken);
        return MapApplication(application);
    }

    public async Task<ClientAgencyLicenseApplicationDto> SubmitApplicationAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var application = await _db.AgencyLicenseApplications
            .Include(a => a.Agency)
            .Include(a => a.LicenseType)
            .FirstOrDefaultAsync(a => a.Uuid == uuid && a.UserId == user.Id, cancellationToken);

        if (application is null)
        {
            throw new ClientPortalException("NOT_FOUND", "Application not found.");
        }

        if (application.Status != AgencyLicenseApplicationStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "This application has already been submitted.");
        }

        application.Status = AgencyLicenseApplicationStatus.Submitted;
        application.SubmittedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapApplication(application);
    }

    public async Task<ClientAgencyLicenseListItemDto> RegisterExistingLicenseAsync(
        RegisterClientAgencyLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var licenseType = await RequireLicenseTypeAsync(request.AgencyId, request.AgencyLicenseTypeId, cancellationToken);

        var licenseNumber = request.LicenseNumber.Trim();
        if (licenseNumber.Length == 0)
        {
            throw new ClientPortalException("VALIDATION", "License number is required.");
        }

        if (request.ExpiresAt <= request.IssuedAt)
        {
            throw new ClientPortalException("VALIDATION", "Expiry date must be after the issue date.");
        }

        var duplicate = await _db.AgencyLicenses.AnyAsync(
            l => l.UserId == user.Id
                && l.AgencyId == licenseType.AgencyId
                && l.AgencyLicenseTypeId == licenseType.Id
                && l.LicenseNumber == licenseNumber
                && l.Status == AgencyLicenseStatus.Active,
            cancellationToken);

        if (duplicate)
        {
            throw new ClientPortalException("LICENSE_EXISTS", "This license number is already on file.");
        }

        var license = new AgencyLicense
        {
            Uuid = Guid.NewGuid(),
            UserId = user.Id,
            AgencyId = licenseType.AgencyId,
            AgencyLicenseTypeId = licenseType.Id,
            LicenseNumber = licenseNumber,
            Status = request.ExpiresAt > DateTime.UtcNow ? AgencyLicenseStatus.Active : AgencyLicenseStatus.Expired,
            IssuedAt = request.IssuedAt.ToUniversalTime(),
            ExpiresAt = request.ExpiresAt.ToUniversalTime(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };

        _db.AgencyLicenses.Add(license);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(license).Reference(l => l.Agency).LoadAsync(cancellationToken);
        await _db.Entry(license).Reference(l => l.LicenseType).LoadAsync(cancellationToken);
        return MapLicense(license, DateTime.UtcNow);
    }

    private async Task<AgencyLicenseType> RequireLicenseTypeAsync(long agencyId, long licenseTypeId, CancellationToken cancellationToken)
    {
        var licenseType = await _db.AgencyLicenseTypes
            .Include(t => t.Agency)
            .FirstOrDefaultAsync(t => t.Id == licenseTypeId && t.AgencyId == agencyId && t.Agency.IsActive, cancellationToken);

        if (licenseType is null)
        {
            throw new ClientPortalException("LICENSE_TYPE_NOT_FOUND", "License type not found for the selected bureau.");
        }

        return licenseType;
    }

    private static AgencyLicenseTypeOptionDto MapType(AgencyLicenseType type) =>
        new(
            type.Id,
            type.AgencyId,
            type.Agency.Code,
            type.Agency.Name,
            type.Code,
            type.Name,
            type.Kind.ToString(),
            type.IsRequiredForSpsic,
            type.Description);

    private static ClientAgencyLicenseListItemDto MapLicense(AgencyLicense license, DateTime now)
    {
        var isActive = license.Status == AgencyLicenseStatus.Active && license.ExpiresAt > now;
        return new ClientAgencyLicenseListItemDto(
            license.Uuid,
            license.AgencyId,
            license.Agency.Code,
            license.Agency.Name,
            license.LicenseType.Code,
            license.LicenseType.Name,
            license.LicenseNumber,
            license.Status.ToString(),
            license.IssuedAt,
            license.ExpiresAt,
            isActive);
    }

    private static ClientAgencyLicenseApplicationListItemDto MapApplicationListItem(AgencyLicenseApplication application) =>
        new(
            application.Uuid,
            application.AgencyId,
            application.Agency.Code,
            application.Agency.Name,
            application.LicenseType.Code,
            application.LicenseType.Name,
            application.Status.ToString(),
            application.SubmittedAt,
            application.CreatedAt);

    private static ClientAgencyLicenseApplicationDto MapApplication(AgencyLicenseApplication application) =>
        new(
            application.Uuid,
            application.AgencyId,
            application.Agency.Code,
            application.Agency.Name,
            application.AgencyLicenseTypeId,
            application.LicenseType.Code,
            application.LicenseType.Name,
            application.Status.ToString(),
            application.FormDataJson,
            application.SubmittedAt,
            application.ReviewComments,
            application.CreatedAt,
            application.UpdatedAt);
}
