using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Compliance;
using AgriCheck.Application.DaPortal.Dtos;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using static AgriCheck.Infrastructure.Helpers.RsasRenewalHelper;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgriCheck.Infrastructure.Services;

public class WarehouseComplianceService : IWarehouseComplianceService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ComplianceOptions _options;

    public WarehouseComplianceService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IOptions<ComplianceOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<WarehouseComplianceFacilityListItemDto>> ListFacilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var warehouseIds = await WarehouseStaffAccessHelper.GetAccessibleWarehouseIdsAsync(
            _db, _currentUser, user.Id, cancellationToken);

        if (warehouseIds.Count == 0)
        {
            return Array.Empty<WarehouseComplianceFacilityListItemDto>();
        }

        var warehouses = await _db.WarehouseFacilities.AsNoTracking()
            .Where(w => w.IsActive && warehouseIds.Contains(w.Id))
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

        var registrations = await _db.FacilityRegistrations.AsNoTracking()
            .Include(r => r.Accreditations)
            .Where(r => warehouseIds.Contains(r.WarehouseFacilityId))
            .ToListAsync(cancellationToken);

        var regByWarehouse = registrations.ToDictionary(r => r.WarehouseFacilityId);
        var now = DateTime.UtcNow;
        var licenses = await WarehouseOperatingLicenseHelper.LoadActiveLicensesByWarehouseIdsAsync(_db, warehouseIds, now, cancellationToken);
        var pendingApps = await _db.WarehouseOperatingLicenseApplications.AsNoTracking()
            .Where(a => warehouseIds.Contains(a.WarehouseFacilityId)
                        && a.Status == WarehouseOperatingLicenseApplicationStatus.Pending)
            .ToListAsync(cancellationToken);
        var pendingByWarehouse = pendingApps.ToDictionary(a => a.WarehouseFacilityId);

        return warehouses.Select(w =>
        {
            var reg = regByWarehouse.GetValueOrDefault(w.Id);
            var csw = reg?.Accreditations.OrderByDescending(a => a.AccreditedAt).FirstOrDefault();
            var hasLto = licenses.TryGetValue(w.Id, out var license);
            var rsasStatus = reg?.Status.ToString() ?? "NotStarted";
            var cswStatus = csw?.Status.ToString();
            var destinationReady = hasLto
                && reg?.Status == FacilityRegistrationStatus.Registered
                && csw?.Status == FacilityAccreditationStatus.Active
                && csw.ExpiresAt > now;

            var ltoAppStatus = pendingByWarehouse.ContainsKey(w.Id)
                ? "Pending"
                : hasLto
                    ? "Issued"
                    : "None";

            return new WarehouseComplianceFacilityListItemDto(
                w.Id,
                w.Code,
                w.Name,
                w.Location,
                hasLto,
                hasLto ? license!.LicenseNumber : null,
                hasLto ? license!.ExpiresAt : null,
                ltoAppStatus,
                rsasStatus,
                cswStatus,
                destinationReady);
        }).ToList();
    }

    public async Task<WarehouseComplianceDetailDto> GetDetailAsync(
        long warehouseFacilityId,
        CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await WarehouseStaffAccessHelper.EnsureCanAccessWarehouseAsync(
            _db, _currentUser, user.Id, warehouseFacilityId, cancellationToken);

        var warehouse = await _db.WarehouseFacilities.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == warehouseFacilityId && w.IsActive, cancellationToken)
            ?? throw new ClientPortalException("WAREHOUSE_NOT_FOUND", "Warehouse facility not found.");

        var now = DateTime.UtcNow;
        var licenseEntity = await WarehouseOperatingLicenseHelper.GetCurrentLicenseAsync(_db, warehouse.Id, cancellationToken);
        WarehouseOperatingLicenseDto? activeLicense = licenseEntity is null
            ? null
            : new WarehouseOperatingLicenseDto(
                licenseEntity.LicenseNumber,
                licenseEntity.Status.ToString(),
                licenseEntity.IssuedAt,
                licenseEntity.ExpiresAt,
                licenseEntity.Notes);

        var pendingApp = await _db.WarehouseOperatingLicenseApplications.AsNoTracking()
            .Include(a => a.WarehouseFacility)
            .Where(a => a.WarehouseFacilityId == warehouse.Id
                        && a.Status == WarehouseOperatingLicenseApplicationStatus.Pending)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var latestApp = await _db.WarehouseOperatingLicenseApplications.AsNoTracking()
            .Include(a => a.WarehouseFacility)
            .Where(a => a.WarehouseFacilityId == warehouse.Id)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var registration = await _db.FacilityRegistrations.AsNoTracking()
            .Include(r => r.WarehouseFacility)
            .Include(r => r.Accreditations)
            .ThenInclude(a => a.LeadAgency)
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouse.Id, cancellationToken);

        var csw = registration?.Accreditations.OrderByDescending(a => a.AccreditedAt).FirstOrDefault();
        var destinationReady = activeLicense is not null
            && registration?.Status == FacilityRegistrationStatus.Registered
            && csw?.Status == FacilityAccreditationStatus.Active
            && csw.ExpiresAt > now;

        return new WarehouseComplianceDetailDto(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.Location,
            activeLicense is not null,
            activeLicense,
            pendingApp is null ? null : MapApplication(pendingApp),
            latestApp is null ? null : MapApplication(latestApp),
            registration is null ? null : MapRegistration(registration),
            csw?.Status.ToString() ?? "NotStarted",
            destinationReady);
    }

    public async Task<WarehouseLtoApplicationDto> SubmitLtoApplicationAsync(
        SubmitWarehouseLtoApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await WarehouseStaffAccessHelper.EnsureCanAccessWarehouseAsync(
            _db, _currentUser, user.Id, request.WarehouseFacilityId, cancellationToken);

        var warehouse = await _db.WarehouseFacilities.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseFacilityId && w.IsActive, cancellationToken)
            ?? throw new ClientPortalException("WAREHOUSE_NOT_FOUND", "Warehouse facility not found.");

        if (await WarehouseOperatingLicenseHelper.HasActiveLicenseAsync(_db, warehouse.Id, cancellationToken))
        {
            throw new ClientPortalException("LTO_ALREADY_ACTIVE", "This warehouse already has an active DA operating LTO.");
        }

        var hasPending = await _db.WarehouseOperatingLicenseApplications.AnyAsync(
            a => a.WarehouseFacilityId == warehouse.Id
                 && a.Status == WarehouseOperatingLicenseApplicationStatus.Pending,
            cancellationToken);

        if (hasPending)
        {
            throw new ClientPortalException("LTO_APPLICATION_PENDING", "An LTO application is already pending DA review.");
        }

        var operatorName = request.OperatorLegalName.Trim();
        if (operatorName.Length == 0)
        {
            throw new ClientPortalException("VALIDATION", "Operator legal name is required.");
        }

        var application = new WarehouseOperatingLicenseApplication
        {
            Uuid = Guid.NewGuid(),
            WarehouseFacilityId = warehouse.Id,
            SubmittedByUserId = user.Id,
            OperatorLegalName = operatorName,
            ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = WarehouseOperatingLicenseApplicationStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
        };

        _db.WarehouseOperatingLicenseApplications.Add(application);
        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(application).Reference(a => a.WarehouseFacility).LoadAsync(cancellationToken);

        return MapApplication(application);
    }

    public async Task<ClientFacilityRegistrationDto> UpsertRsasAsync(
        UpsertFacilityRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await WarehouseStaffAccessHelper.EnsureCanAccessWarehouseAsync(
            _db, _currentUser, user.Id, request.WarehouseFacilityId, cancellationToken);

        var warehouse = await _db.WarehouseFacilities.FirstOrDefaultAsync(
            w => w.Id == request.WarehouseFacilityId && w.IsActive,
            cancellationToken) ?? throw new ClientPortalException("WAREHOUSE_NOT_FOUND", "Warehouse facility not found.");

        if (!await WarehouseOperatingLicenseHelper.HasActiveLicenseAsync(_db, warehouse.Id, cancellationToken))
        {
            throw new ClientPortalException(
                "WAREHOUSE_LTO_REQUIRED",
                "RSAS registration requires an active DA warehouse operating license (LTO). Submit an LTO application first.");
        }

        var ownerName = request.OwnerName.Trim();
        if (ownerName.Length == 0)
        {
            throw new ClientPortalException("VALIDATION", "Facility owner name is required.");
        }

        var registration = await _db.FacilityRegistrations
            .Include(r => r.WarehouseFacility)
            .Include(r => r.Accreditations)
            .ThenInclude(a => a.LeadAgency)
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == request.WarehouseFacilityId, cancellationToken);

        if (registration is null)
        {
            registration = new FacilityRegistration
            {
                Uuid = Guid.NewGuid(),
                WarehouseFacilityId = warehouse.Id,
                SubmittedByUserId = user.Id,
                Status = FacilityRegistrationStatus.Draft,
            };
            _db.FacilityRegistrations.Add(registration);
        }
        else if (registration.Status is not (FacilityRegistrationStatus.Draft or FacilityRegistrationStatus.Rejected))
        {
            throw new ClientPortalException("INVALID_STATUS", "This facility registration can no longer be edited.");
        }

        registration.OwnerName = ownerName;
        registration.MaxCapacityKg = request.MaxCapacityKg;
        registration.RsasExternalRef = string.IsNullOrWhiteSpace(request.RsasExternalRef) ? null : request.RsasExternalRef.Trim();
        registration.ProductsJson = request.ProductsJson;
        registration.IsBmbeExempt = request.IsBmbeExempt;
        registration.SubmittedByUserId = user.Id;

        var rsasClasses = WarehouseCommodityClassHelper.ParseClassListJson(request.ProductsJson);
        WarehouseFacilityProductScopeHelper.ValidateRsasProductsJson(rsasClasses);

        await _db.SaveChangesAsync(cancellationToken);
        return MapRegistration(registration);
    }

    public async Task<ClientFacilityRegistrationDto> SubmitRsasAsync(
        Guid registrationUuid,
        CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);

        var registration = await _db.FacilityRegistrations
            .Include(r => r.WarehouseFacility)
            .Include(r => r.Accreditations)
            .FirstOrDefaultAsync(r => r.Uuid == registrationUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Facility registration not found.");

        await WarehouseStaffAccessHelper.EnsureCanAccessWarehouseAsync(
            _db, _currentUser, user.Id, registration.WarehouseFacilityId, cancellationToken);

        if (registration.Status != FacilityRegistrationStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only draft registrations can be submitted.");
        }

        if (!_options.RequireDaRsasVerification && !string.IsNullOrWhiteSpace(registration.RsasExternalRef))
        {
            registration.Status = FacilityRegistrationStatus.Registered;
            registration.RegisteredAt = DateTime.UtcNow;
            registration.ExpiresAt = DateTime.UtcNow.AddYears(1);
            EnqueueRsasSync(registration);
        }
        else
        {
            registration.Status = FacilityRegistrationStatus.Submitted;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return MapRegistration(registration);
    }

    private void EnqueueRsasSync(FacilityRegistration registration)
    {
        _db.ComplianceSyncOutbox.Add(new ComplianceSyncOutbox
        {
            EntityType = "FacilityRegistration",
            EntityId = registration.Id,
            TargetSystem = ComplianceSyncTargetSystem.Rsas,
            Operation = "RSAS_REGISTER",
            PayloadJson = $"{{\"warehouseFacilityId\":{registration.WarehouseFacilityId},\"rsasRef\":\"{registration.RsasExternalRef}\"}}",
        });
    }

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

    private static ClientFacilityRegistrationDto MapRegistration(FacilityRegistration registration)
    {
        var now = DateTime.UtcNow;
        var csw = registration.Accreditations.OrderByDescending(a => a.AccreditedAt).FirstOrDefault();
        return new ClientFacilityRegistrationDto(
            registration.Uuid,
            registration.WarehouseFacilityId,
            registration.WarehouseFacility.Code,
            registration.WarehouseFacility.Name,
            registration.Status.ToString(),
            registration.OwnerName,
            registration.MaxCapacityKg,
            registration.RsasExternalRef,
            registration.ProductsJson,
            registration.RegisteredAt,
            registration.ExpiresAt,
            registration.ReviewComments,
            registration.IsBmbeExempt,
            IsRenewalWindowOpen(now),
            IsRenewalDue(registration.ExpiresAt, now),
            csw is null
                ? null
                : new FacilityAccreditationSummaryDto(
                    csw.Uuid,
                    csw.Status.ToString(),
                    csw.CertificateNumber,
                    csw.AccreditedAt,
                    csw.ExpiresAt,
                    csw.LeadAgency?.Code));
    }
}
