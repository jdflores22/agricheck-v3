using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using static AgriCheck.Infrastructure.Helpers.RsasRenewalHelper;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class ClientFacilityRegistrationService : IClientFacilityRegistrationService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClientFacilityRegistrationService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ClientFacilityRegistrationListItemDto>> ListAsync(
        long? entryAgencyId = null,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);

        var affiliatedIds = await _db.ImporterFacilityAffiliations
            .AsNoTracking()
            .Where(a => a.UserId == user.Id)
            .Select(a => a.WarehouseFacilityId)
            .ToListAsync(cancellationToken);

        if (affiliatedIds.Count == 0)
        {
            return Array.Empty<ClientFacilityRegistrationListItemDto>();
        }

        string? entryAgencyCode = null;
        if (entryAgencyId is > 0)
        {
            entryAgencyCode = await _db.Agencies.AsNoTracking()
                .Where(a => a.Id == entryAgencyId.Value && a.IsActive)
                .Select(a => a.Code)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var warehouses = await _db.WarehouseFacilities
            .AsNoTracking()
            .Where(w => w.IsActive && affiliatedIds.Contains(w.Id))
            .OrderBy(w => w.Code)
            .ToListAsync(cancellationToken);

        var registrations = await _db.FacilityRegistrations
            .AsNoTracking()
            .Include(r => r.Accreditations)
            .ThenInclude(a => a.LeadAgency)
            .Where(r => affiliatedIds.Contains(r.WarehouseFacilityId))
            .ToListAsync(cancellationToken);

        var regByWarehouse = registrations.ToDictionary(r => r.WarehouseFacilityId);
        var now = DateTime.UtcNow;
        var rows = new List<ClientFacilityRegistrationListItemDto>();

        foreach (var warehouse in warehouses)
        {
            var scope = await WarehouseFacilityProductScopeHelper.ResolveAsync(_db, warehouse.Id, cancellationToken);
            if (entryAgencyCode is not null
                && !WarehouseEntryAgencyCompatibilityHelper.IsCompatibleWithEntryAgency(scope, entryAgencyCode))
            {
                continue;
            }

            rows.Add(MapListItem(
                warehouse,
                regByWarehouse.GetValueOrDefault(warehouse.Id),
                now,
                scope));
        }

        return rows;
    }

    public async Task<IReadOnlyList<ClientFacilityRegistrationListItemDto>> SearchLicensedWarehousesAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var licensedIds = await WarehouseOperatingLicenseHelper.GetActiveLicensedWarehouseIdsAsync(_db, cancellationToken);
        if (licensedIds.Count == 0)
        {
            return Array.Empty<ClientFacilityRegistrationListItemDto>();
        }

        var alreadyLinked = await _db.ImporterFacilityAffiliations
            .AsNoTracking()
            .Where(a => a.UserId == user.Id)
            .Select(a => a.WarehouseFacilityId)
            .ToListAsync(cancellationToken);

        var availableIds = licensedIds.Except(alreadyLinked).ToHashSet();
        if (availableIds.Count == 0)
        {
            return Array.Empty<ClientFacilityRegistrationListItemDto>();
        }

        var q = _db.WarehouseFacilities.AsNoTracking()
            .Where(w => w.IsActive && availableIds.Contains(w.Id));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(w => w.Name.Contains(term) || w.Code.Contains(term) || (w.Location != null && w.Location.Contains(term)));
        }

        var warehouses = await q.OrderBy(w => w.Name).Take(25).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return warehouses.Select(w => MapListItem(w, null, now)).ToList();
    }

    public async Task<ClientFacilityRegistrationListItemDto> LinkWarehouseAsync(
        long warehouseFacilityId,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);

        var warehouse = await _db.WarehouseFacilities.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == warehouseFacilityId && w.IsActive, cancellationToken);

        if (warehouse is null)
        {
            throw new ClientPortalException("WAREHOUSE_NOT_FOUND", "Warehouse facility not found.");
        }

        if (!await WarehouseOperatingLicenseHelper.HasActiveLicenseAsync(_db, warehouse.Id, cancellationToken))
        {
            throw new ClientPortalException(
                "WAREHOUSE_LTO_REQUIRED",
                "This warehouse does not have an active DA warehouse operating license (LTO).");
        }

        var exists = await _db.ImporterFacilityAffiliations
            .AnyAsync(a => a.UserId == user.Id && a.WarehouseFacilityId == warehouse.Id, cancellationToken);

        if (!exists)
        {
            _db.ImporterFacilityAffiliations.Add(new ImporterFacilityAffiliation
            {
                UserId = user.Id,
                WarehouseFacilityId = warehouse.Id,
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        var registration = await _db.FacilityRegistrations
            .AsNoTracking()
            .Include(r => r.Accreditations)
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouse.Id, cancellationToken);

        return MapListItem(warehouse, registration, DateTime.UtcNow);
    }

    public async Task<ClientFacilityRegistrationDto?> GetByWarehouseAsync(long warehouseFacilityId, CancellationToken cancellationToken = default)
    {
        await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var registration = await _db.FacilityRegistrations
            .AsNoTracking()
            .Include(r => r.WarehouseFacility)
            .Include(r => r.Accreditations)
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouseFacilityId, cancellationToken);

        return registration is null ? null : Map(registration);
    }

    public Task<ClientFacilityRegistrationDto> UpsertAsync(UpsertFacilityRegistrationRequest request, CancellationToken cancellationToken = default) =>
        throw new ClientPortalException(
            "RSAS_OPERATOR_ONLY",
            "RSAS registration is managed by the warehouse operator in the Warehouse Portal (Compliance), not by importers.");

    public Task<ClientFacilityRegistrationDto> SubmitAsync(Guid registrationUuid, CancellationToken cancellationToken = default) =>
        throw new ClientPortalException(
            "RSAS_OPERATOR_ONLY",
            "RSAS registration is managed by the warehouse operator in the Warehouse Portal (Compliance), not by importers.");

    public Task<FacilityAccreditationSummaryDto> RegisterCswAsync(RegisterFacilityCswRequest request, CancellationToken cancellationToken = default) =>
        throw new ClientPortalException(
            "CSW_OPERATOR_ONLY",
            "CSW accreditation is recorded by DA oversight, not by importers.");

    private static ClientFacilityRegistrationListItemDto MapListItem(
        WarehouseFacility warehouse,
        FacilityRegistration? registration,
        DateTime now,
        WarehouseFacilityScopeResolution? scope = null)
    {
        var csw = registration?.Accreditations.OrderByDescending(a => a.AccreditedAt).FirstOrDefault();
        var facilityLabels = scope is not null
            ? WarehouseCommodityClassHelper.FormatClassLabels(scope.AllowedClasses)
            : null;

        return new ClientFacilityRegistrationListItemDto(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.Location,
            registration?.Uuid,
            registration?.Status.ToString() ?? "NotStarted",
            registration?.RsasExternalRef,
            csw?.Status.ToString(),
            registration?.RegisteredAt,
            registration?.ExpiresAt,
            registration?.IsBmbeExempt ?? false,
            IsRenewalWindowOpen(now),
            IsRenewalDue(registration?.ExpiresAt, now),
            scope?.CswLeadAgencyCode ?? csw?.LeadAgency?.Code,
            facilityLabels,
            scope?.DisplayHint);
    }

    private static ClientFacilityRegistrationDto Map(FacilityRegistration registration)
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
