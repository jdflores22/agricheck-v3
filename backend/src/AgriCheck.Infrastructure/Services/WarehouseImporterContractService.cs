using System.Text.Json;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class WarehouseImporterContractService : IWarehouseImporterContractService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public WarehouseImporterContractService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WarehouseImporterContractListItemDto>> ListAsync(
        long? warehouseFacilityId,
        CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var warehouseIds = await WarehouseStaffAccessHelper.GetAccessibleWarehouseIdsAsync(
            _db, _currentUser, user.Id, cancellationToken);

        if (warehouseIds.Count == 0)
        {
            return Array.Empty<WarehouseImporterContractListItemDto>();
        }

        if (warehouseFacilityId is long facilityId)
        {
            await WarehouseStaffAccessHelper.EnsureCanAccessWarehouseAsync(
                _db, _currentUser, user.Id, facilityId, cancellationToken);
            warehouseIds = new List<long> { facilityId };
        }

        var allocations = await _db.WarehouseImporterAllocations.AsNoTracking()
            .Include(a => a.ImporterUser).ThenInclude(u => u.Profile)
            .Include(a => a.WarehouseFacility)
            .Where(a => warehouseIds.Contains(a.WarehouseFacilityId))
            .OrderBy(a => a.WarehouseFacility.Name)
            .ThenBy(a => a.ImporterUser.Email)
            .ThenByDescending(a => a.ValidFrom)
            .ToListAsync(cancellationToken);

        var rows = new List<WarehouseImporterContractListItemDto>();
        var scopeByWarehouse = new Dictionary<long, WarehouseFacilityScopeResolution>();

        async Task<WarehouseFacilityScopeResolution> ScopeForAsync(long whId)
        {
            if (!scopeByWarehouse.TryGetValue(whId, out var scope))
            {
                scope = await WarehouseFacilityProductScopeHelper.ResolveAsync(_db, whId, cancellationToken);
                scopeByWarehouse[whId] = scope;
            }

            return scope;
        }

        foreach (var allocation in allocations)
        {
            var committed = await WarehouseAllocationMetricsHelper.SumImporterCommittedKgAsync(
                _db,
                allocation.ImporterUserId,
                allocation.WarehouseFacilityId,
                null,
                cancellationToken);

            var allowed = WarehouseCommodityClassHelper.FormatClassLabels(
                WarehouseCommodityClassHelper.ParseClassListJson(allocation.AllowedCommodityClassesJson));

            var allocated = allocation.AllocatedCapacityKg;
            decimal? headroom = allocated > 0 ? allocated - committed : null;
            var facilityScope = await ScopeForAsync(allocation.WarehouseFacilityId);

            rows.Add(Map(allocation, committed, headroom, allowed, facilityScope));
        }

        var activeContractKeys = allocations
            .Where(a => a.Status == WarehouseImporterAllocationStatus.Active)
            .Select(a => (a.WarehouseFacilityId, a.ImporterUserId))
            .ToHashSet();

        var affiliations = await _db.ImporterFacilityAffiliations.AsNoTracking()
            .Include(a => a.User).ThenInclude(u => u.Profile)
            .Include(a => a.WarehouseFacility)
            .Where(a => warehouseIds.Contains(a.WarehouseFacilityId))
            .ToListAsync(cancellationToken);

        foreach (var affiliation in affiliations)
        {
            if (activeContractKeys.Contains((affiliation.WarehouseFacilityId, affiliation.UserId)))
            {
                continue;
            }

            var committed = await WarehouseAllocationMetricsHelper.SumImporterCommittedKgAsync(
                _db,
                affiliation.UserId,
                affiliation.WarehouseFacilityId,
                null,
                cancellationToken);

            var facilityScope = await ScopeForAsync(affiliation.WarehouseFacilityId);
            var facilityLabels = WarehouseCommodityClassHelper.FormatClassLabels(facilityScope.AllowedClasses);

            rows.Add(new WarehouseImporterContractListItemDto(
                null,
                affiliation.WarehouseFacilityId,
                affiliation.WarehouseFacility.Code,
                affiliation.WarehouseFacility.Name,
                affiliation.User.Uuid,
                affiliation.User.Email,
                FormatImporterName(affiliation.User),
                true,
                null,
                null,
                committed,
                null,
                Array.Empty<string>(),
                null,
                null,
                null,
                facilityLabels,
                facilityScope.DisplayHint,
                facilityScope.CswLeadAgencyCode));
        }

        return rows
            .OrderBy(r => r.WarehouseName)
            .ThenBy(r => r.ImporterDisplayName)
            .ToList();
    }

    public async Task<WarehouseImporterContractListItemDto> UpsertAsync(
        UpsertWarehouseImporterContractRequest request,
        CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);

        if (request.AllocatedCapacityKg <= 0)
        {
            throw new ClientPortalException("INVALID_CAPACITY", "Allocated capacity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.ContractReference))
        {
            throw new ClientPortalException("CONTRACT_REFERENCE_REQUIRED", "Contract reference is required.");
        }

        WarehouseImporterAllocation allocation;

        if (request.AllocationUuid is Guid allocationUuid)
        {
            allocation = await _db.WarehouseImporterAllocations
                .Include(a => a.ImporterUser).ThenInclude(u => u.Profile)
                .Include(a => a.WarehouseFacility)
                .FirstOrDefaultAsync(a => a.Uuid == allocationUuid, cancellationToken)
                ?? throw new ClientPortalException("CONTRACT_NOT_FOUND", "Storage contract not found.");

            await WarehouseStaffAccessHelper.EnsureCanAccessWarehouseAsync(
                _db, _currentUser, user.Id, allocation.WarehouseFacilityId, cancellationToken);
        }
        else
        {
            await WarehouseStaffAccessHelper.EnsureCanAccessWarehouseAsync(
                _db, _currentUser, user.Id, request.WarehouseFacilityId, cancellationToken);

            var importer = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Uuid == request.ImporterUserUuid, cancellationToken)
                ?? throw new ClientPortalException("IMPORTER_NOT_FOUND", "Importer account not found.");

            var linked = await _db.ImporterFacilityAffiliations.AsNoTracking()
                .AnyAsync(
                    a => a.UserId == importer.Id && a.WarehouseFacilityId == request.WarehouseFacilityId,
                    cancellationToken);

            if (!linked)
            {
                throw new ClientPortalException(
                    "IMPORTER_NOT_LINKED",
                    "This importer has not linked your warehouse under My Facilities yet.");
            }

            var existing = await _db.WarehouseImporterAllocations
                .Include(a => a.ImporterUser).ThenInclude(u => u.Profile)
                .Include(a => a.WarehouseFacility)
                .Where(a => a.ImporterUserId == importer.Id
                    && a.WarehouseFacilityId == request.WarehouseFacilityId
                    && a.Status == WarehouseImporterAllocationStatus.Active)
                .OrderByDescending(a => a.ValidFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                allocation = existing;
            }
            else
            {
                allocation = new WarehouseImporterAllocation
                {
                    ImporterUserId = importer.Id,
                    WarehouseFacilityId = request.WarehouseFacilityId,
                    ValidFrom = DateTime.UtcNow,
                    Status = WarehouseImporterAllocationStatus.Active,
                };
                _db.WarehouseImporterAllocations.Add(allocation);
                await _db.SaveChangesAsync(cancellationToken);
                await _db.Entry(allocation).Reference(a => a.ImporterUser).LoadAsync(cancellationToken);
                await _db.Entry(allocation.ImporterUser).Reference(u => u.Profile).LoadAsync(cancellationToken);
                await _db.Entry(allocation).Reference(a => a.WarehouseFacility).LoadAsync(cancellationToken);
            }
        }

        var committed = await WarehouseAllocationMetricsHelper.SumImporterCommittedKgAsync(
            _db,
            allocation.ImporterUserId,
            allocation.WarehouseFacilityId,
            null,
            cancellationToken);

        if (request.AllocatedCapacityKg < committed)
        {
            throw new ClientPortalException(
                "CONTRACT_CAPACITY_BELOW_COMMITTED",
                $"Allocated capacity cannot be below committed volume ({FormatMt(committed)} already on entries for this importer).");
        }

        var allowedClasses = ParseAllowedClasses(request.AllowedCommodityClasses);
        var facilityScope = await WarehouseFacilityProductScopeHelper.ResolveAsync(
            _db,
            allocation.WarehouseFacilityId,
            cancellationToken);
        WarehouseFacilityProductScopeHelper.ValidateContractScope(allowedClasses, facilityScope);

        allocation.ContractReference = request.ContractReference.Trim();
        allocation.AllocatedCapacityKg = request.AllocatedCapacityKg;
        allocation.AllowedCommodityClassesJson = JsonSerializer.Serialize(
            allowedClasses.Select(c => c.ToString()).ToList());

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<WarehouseImporterAllocationStatus>(request.Status, true, out var parsedStatus))
        {
            allocation.Status = parsedStatus;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var allowedLabels = WarehouseCommodityClassHelper.FormatClassLabels(allowedClasses);
        var headroom = allocation.AllocatedCapacityKg - committed;
        return Map(allocation, committed, headroom, allowedLabels, facilityScope);
    }

    private static List<WarehouseCommodityClass> ParseAllowedClasses(IReadOnlyList<string> raw)
    {
        var list = new List<WarehouseCommodityClass>();
        foreach (var item in raw)
        {
            if (Enum.TryParse<WarehouseCommodityClass>(item, true, out var parsed))
            {
                list.Add(parsed);
            }
        }

        return list;
    }

    private static string FormatMt(decimal kg) =>
        kg >= 1000 ? $"{Math.Round(kg / 1000m, 2)} MT" : $"{Math.Round(kg, 0)} kg";

    private static WarehouseImporterContractListItemDto Map(
        WarehouseImporterAllocation allocation,
        decimal committedKg,
        decimal? headroomKg,
        IReadOnlyList<string> allowedClasses,
        WarehouseFacilityScopeResolution facilityScope)
    {
        var facilityLabels = WarehouseCommodityClassHelper.FormatClassLabels(facilityScope.AllowedClasses);
        return new WarehouseImporterContractListItemDto(
            allocation.Uuid,
            allocation.WarehouseFacilityId,
            allocation.WarehouseFacility.Code,
            allocation.WarehouseFacility.Name,
            allocation.ImporterUser.Uuid,
            allocation.ImporterUser.Email,
            FormatImporterName(allocation.ImporterUser),
            true,
            allocation.ContractReference,
            allocation.AllocatedCapacityKg,
            committedKg,
            headroomKg,
            allowedClasses,
            allocation.Status.ToString(),
            allocation.ValidFrom,
            allocation.ValidTo,
            facilityLabels,
            facilityScope.DisplayHint,
            facilityScope.CswLeadAgencyCode);
    }

    private static string FormatImporterName(Domain.Entities.User importer)
    {
        var company = importer.Profile?.CompanyName?.Trim();
        if (!string.IsNullOrWhiteSpace(company))
        {
            return company;
        }

        var first = importer.Profile?.FirstName?.Trim() ?? string.Empty;
        var last = importer.Profile?.LastName?.Trim() ?? string.Empty;
        var full = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(full) ? importer.Email : full;
    }
}
