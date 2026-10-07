using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgriCheck.Infrastructure.Services;

public class EntryDestinationValidationService : IEntryDestinationValidationService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ComplianceOptions _options;

    public EntryDestinationValidationService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IOptions<ComplianceOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _options = options.Value;
    }

    public async Task<EntryDestinationValidationResultDto> ValidateForCurrentUserAsync(
        EntryDestinationValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        return await ValidateForImporterAsync(user.Id, request, cancellationToken);
    }

    public Task<EntryDestinationValidationResultDto> ValidateForImporterAsync(
        long importerUserId,
        EntryDestinationValidationRequest request,
        CancellationToken cancellationToken = default) =>
        ValidateInternalAsync(importerUserId, request, cancellationToken);

    public async Task RequireValidForEntryAsync(
        long importerUserId,
        long entryId,
        long warehouseFacilityId,
        decimal shipmentKg,
        string? formDataJson,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnforceEntryDestinationValidation)
        {
            return;
        }

        var entryMeta = await _db.Entries.AsNoTracking()
            .Where(e => e.Id == entryId)
            .Select(e => new { e.Uuid, e.AgencyId })
            .FirstOrDefaultAsync(cancellationToken);

        var result = await ValidateInternalAsync(
            importerUserId,
            new EntryDestinationValidationRequest(
                warehouseFacilityId,
                shipmentKg,
                "kg",
                entryMeta?.Uuid == Guid.Empty ? null : entryMeta?.Uuid,
                formDataJson,
                entryMeta?.AgencyId),
            cancellationToken);

        if (!result.IsValid)
        {
            var blocker = result.Checks.FirstOrDefault(c => c.Severity == "error")
                ?? result.Checks[0];
            throw new ClientPortalException(blocker.Code, blocker.Message);
        }
    }

    private async Task<EntryDestinationValidationResultDto> ValidateInternalAsync(
        long importerUserId,
        EntryDestinationValidationRequest request,
        CancellationToken cancellationToken)
    {
        var checks = new List<EntryDestinationCheckDto>();

        if (request.WarehouseFacilityId is null or <= 0)
        {
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_WAREHOUSE_REQUIRED",
                "info",
                "Select a final warehouse to validate capacity and product scope.",
                null));
            return new EntryDestinationValidationResultDto(false, checks, null);
        }

        var warehouseId = request.WarehouseFacilityId.Value;
        var warehouse = await _db.WarehouseFacilities.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == warehouseId && w.IsActive, cancellationToken);

        if (warehouse is null)
        {
            checks.Add(new EntryDestinationCheckDto(
                "WAREHOUSE_NOT_FOUND",
                "error",
                "Warehouse facility not found or inactive.",
                null));
            return new EntryDestinationValidationResultDto(false, checks, null);
        }

        var incomingKg = request.ShipmentQuantity is > 0
            ? ShipmentQuantityHelper.ToKg(request.ShipmentQuantity.Value, request.ShipmentUnit)
            : 0m;

        var incomingClass = await WarehouseCommodityClassHelper.ResolveFromFormDataAsync(
            _db,
            request.FormDataJson,
            cancellationToken);

        var hasAffiliation = await _db.ImporterFacilityAffiliations.AsNoTracking()
            .AnyAsync(a => a.UserId == importerUserId && a.WarehouseFacilityId == warehouseId, cancellationToken);

        if (!hasAffiliation)
        {
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_NOT_LINKED",
                "error",
                "Link this warehouse under My Facilities before using it as your import destination.",
                null));
        }

        var registration = await _db.FacilityRegistrations.AsNoTracking()
            .FirstOrDefaultAsync(r => r.WarehouseFacilityId == warehouseId, cancellationToken);

        var facilityScope = await WarehouseFacilityProductScopeHelper.ResolveAsync(_db, warehouseId, cancellationToken);
        var facilityAllowed = facilityScope.AllowedClasses;
        var facilityAllowedLabels = WarehouseCommodityClassHelper.FormatClassLabels(facilityAllowed);

        if (request.EntryAgencyId is > 0)
        {
            var entryAgencyCode = await _db.Agencies.AsNoTracking()
                .Where(a => a.Id == request.EntryAgencyId.Value && a.IsActive)
                .Select(a => a.Code)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(entryAgencyCode)
                && !WarehouseEntryAgencyCompatibilityHelper.IsCompatibleWithEntryAgency(
                    facilityScope,
                    entryAgencyCode))
            {
                checks.Add(new EntryDestinationCheckDto(
                    "DESTINATION_AGENCY_SCOPE",
                    "error",
                    $"This warehouse cannot be used as the final destination for {entryAgencyCode} entries.",
                    WarehouseEntryAgencyCompatibilityHelper.DestinationFilterHint(entryAgencyCode)));
            }
        }

        if (facilityAllowed.Count > 0 && incomingClass is not null
            && !facilityAllowed.Contains(incomingClass.Value))
        {
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_COMMODITY_SCOPE",
                "error",
                "This commodity class is not allowed at the selected warehouse (RSAS/CSW product scope).",
                $"{facilityScope.DisplayHint} Incoming: {incomingClass}."));
        }
        else if (facilityAllowed.Count == 0 && incomingClass is not null && incomingKg > 0)
        {
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_COMMODITY_SCOPE",
                "warning",
                "Warehouse product scope is not on file yet; confirm RSAS/CSW before importing.",
                facilityScope.DisplayHint));
        }
        else if (facilityAllowed.Count > 0 && incomingClass is null && incomingKg > 0)
        {
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_COMMODITY_UNKNOWN",
                "warning",
                "Select a MAV HS commodity so we can verify warehouse product scope.",
                null));
        }

        var now = DateTime.UtcNow;
        var allocation = await _db.WarehouseImporterAllocations.AsNoTracking()
            .Where(a => a.ImporterUserId == importerUserId
                && a.WarehouseFacilityId == warehouseId
                && a.Status == WarehouseImporterAllocationStatus.Active
                && a.ValidFrom <= now
                && (a.ValidTo == null || a.ValidTo >= now))
            .OrderByDescending(a => a.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

        var contractAllowed = WarehouseCommodityClassHelper.ParseClassListJson(allocation?.AllowedCommodityClassesJson);
        var contractAllowedLabels = WarehouseCommodityClassHelper.FormatClassLabels(contractAllowed);

        if (allocation is null)
        {
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_NO_CONTRACT",
                _options.RequireImporterWarehouseAllocation ? "error" : "warning",
                "No active storage contract (capacity allocation) with this warehouse.",
                "Ask the warehouse operator or DA to record your contracted storage headroom."));
        }
        else if (contractAllowed.Count > 0 && incomingClass is not null
            && !contractAllowed.Contains(incomingClass.Value))
        {
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_CONTRACT_COMMODITY",
                "error",
                "Your storage contract does not cover this commodity class at this warehouse.",
                $"Contract allows: {string.Join(", ", contractAllowedLabels)}."));
        }

        var excludeEntryId = request.ExcludeEntryUuid is Guid eu
            ? await _db.Entries.AsNoTracking().Where(e => e.Uuid == eu).Select(e => (long?)e.Id).FirstOrDefaultAsync(cancellationToken)
            : null;

        var importerCommittedKg = await WarehouseAllocationMetricsHelper.SumImporterCommittedKgAsync(
            _db,
            importerUserId,
            warehouseId,
            excludeEntryId,
            cancellationToken);

        var facilityUsedKg = await SumFacilityCommittedKgAsync(warehouseId, excludeEntryId, cancellationToken);

        var importerStoredKg = await SumImporterStoredKgAsync(importerUserId, warehouseId, cancellationToken);

        if (incomingClass is not null)
        {
            var storedClasses = await GetStoredCommodityClassesAtFacilityAsync(warehouseId, cancellationToken);
            foreach (var stored in storedClasses)
            {
                if (WarehouseCommodityClassHelper.ClassesSegregationConflict(stored, incomingClass.Value))
                {
                    checks.Add(new EntryDestinationCheckDto(
                        "DESTINATION_SEGREGATION",
                        "error",
                        "Cold storage segregation rule: meat and fish products cannot share the same warehouse.",
                        $"Warehouse already holds {stored}; incoming is {incomingClass}."));
                    break;
                }
            }
        }

        decimal? facilityMaxKg = registration?.MaxCapacityKg;
        if (facilityMaxKg is null or <= 0 && warehouse.Capacity > 0)
        {
            facilityMaxKg = warehouse.Capacity * 1000m;
        }

        decimal? facilityHeadroom = facilityMaxKg is > 0 ? facilityMaxKg - facilityUsedKg : null;
        if (facilityMaxKg is > 0 && incomingKg > 0 && facilityUsedKg + incomingKg > facilityMaxKg)
        {
            var over = facilityUsedKg + incomingKg - facilityMaxKg.Value;
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_FACILITY_FULL",
                "error",
                "The warehouse does not have enough total capacity for this shipment.",
                $"Over by {FormatMt(over)} (facility max {FormatMt(facilityMaxKg.Value)}, used {FormatMt(facilityUsedKg)}, incoming {FormatMt(incomingKg)})."));
        }
        else if (facilityMaxKg is > 0 && incomingKg > 0)
        {
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_FACILITY_CAPACITY",
                "ok",
                $"Facility headroom after this shipment: about {FormatMt(facilityHeadroom!.Value - incomingKg)}.",
                null));
        }

        decimal contractAllocated = allocation?.AllocatedCapacityKg ?? 0;
        decimal importerHeadroom = contractAllocated > 0 ? contractAllocated - importerCommittedKg : 0;

        if (allocation is not null && incomingKg > 0 && importerCommittedKg + incomingKg > contractAllocated)
        {
            var over = importerCommittedKg + incomingKg - contractAllocated;
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_CONTRACT_CAPACITY",
                "error",
                "This import exceeds your contracted storage headroom at the warehouse.",
                $"Over by {FormatMt(over)} (contract {FormatMt(contractAllocated)}, already committed {FormatMt(importerCommittedKg)} incl. stock, incoming {FormatMt(incomingKg)})."));
        }
        else if (allocation is not null && incomingKg > 0)
        {
            checks.Add(new EntryDestinationCheckDto(
                "DESTINATION_CONTRACT_CAPACITY",
                "ok",
                $"Contract headroom after this shipment: about {FormatMt(importerHeadroom - incomingKg)}.",
                null));
        }

        var snapshot = new EntryDestinationCapacitySnapshotDto(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            facilityMaxKg,
            facilityUsedKg,
            facilityHeadroom,
            allocation is null ? null : contractAllocated,
            importerCommittedKg,
            importerStoredKg,
            importerHeadroom,
            incomingKg,
            incomingClass?.ToString(),
            facilityAllowedLabels,
            contractAllowedLabels);

        var hasError = checks.Any(c => c.Severity == "error");
        var isValid = !hasError && hasAffiliation
            && (!_options.RequireImporterWarehouseAllocation || allocation is not null);

        return new EntryDestinationValidationResultDto(isValid, checks, snapshot);
    }

    private static readonly EntryStatus[] ExcludedEntryStatuses =
    {
        EntryStatus.Cancelled,
        EntryStatus.Rejected,
    };

    private async Task<decimal> SumFacilityCommittedKgAsync(
        long warehouseFacilityId,
        long? excludeEntryId,
        CancellationToken cancellationToken)
    {
        var query = _db.Entries.AsNoTracking()
            .Include(e => e.Detail)
            .Where(e => e.FinalWarehouseFacilityId == warehouseFacilityId
                && !ExcludedEntryStatuses.Contains(e.Status));

        if (excludeEntryId is not null)
        {
            query = query.Where(e => e.Id != excludeEntryId.Value);
        }

        var entries = await query.ToListAsync(cancellationToken);
        return entries.Sum(e => ShipmentQuantityHelper.ToKg(e.Detail?.Quantity ?? 0, e.Detail?.Unit));
    }

    private async Task<decimal> SumImporterStoredKgAsync(
        long importerUserId,
        long warehouseFacilityId,
        CancellationToken cancellationToken)
    {
        var entryIds = await _db.WarehouseInventories.AsNoTracking()
            .Where(i => i.WarehouseFacilityId == warehouseFacilityId && i.Status == WarehouseInventoryStatus.Stored)
            .Select(i => i.Container.EntryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (entryIds.Count == 0)
        {
            return 0;
        }

        var entries = await _db.Entries.AsNoTracking()
            .Include(e => e.Detail)
            .Where(e => entryIds.Contains(e.Id) && e.UserId == importerUserId)
            .ToListAsync(cancellationToken);

        return entries.Sum(e => ShipmentQuantityHelper.ToKg(e.Detail?.Quantity ?? 0, e.Detail?.Unit));
    }

    private async Task<HashSet<WarehouseCommodityClass>> GetStoredCommodityClassesAtFacilityAsync(
        long warehouseFacilityId,
        CancellationToken cancellationToken)
    {
        var entryIds = await _db.WarehouseInventories.AsNoTracking()
            .Where(i => i.WarehouseFacilityId == warehouseFacilityId && i.Status == WarehouseInventoryStatus.Stored)
            .Select(i => i.Container.EntryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (entryIds.Count == 0)
        {
            return new HashSet<WarehouseCommodityClass>();
        }

        var entries = await _db.Entries.AsNoTracking()
            .Where(e => entryIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var set = new HashSet<WarehouseCommodityClass>();
        foreach (var entry in entries)
        {
            var cls = await WarehouseCommodityClassHelper.ResolveFromFormDataAsync(_db, entry.FormDataJson, cancellationToken);
            if (cls is not null)
            {
                set.Add(cls.Value);
            }
        }

        return set;
    }

    private static string FormatMt(decimal kg) =>
        kg >= 1000 ? $"{Math.Round(kg / 1000m, 2)} MT" : $"{Math.Round(kg, 0)} kg";
}
