using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Compliance;
using AgriCheck.Application.Common;
using AgriCheck.Application.Notifications;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgriCheck.Infrastructure.Services;

public class WarehouseOpsService : IWarehouseOpsService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IContainerImportComplianceGate _importCompliance;

    public WarehouseOpsService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IContainerImportComplianceGate importCompliance)
    {
        _db = db;
        _currentUser = currentUser;
        _importCompliance = importCompliance;
    }

    public async Task<WarehouseDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var today = DateTime.UtcNow.Date;
        var stored = await _db.WarehouseInventories.CountAsync(i => i.Status == WarehouseInventoryStatus.Stored, cancellationToken);
        var pendingReleases = await _db.ReleaseAuthorizations
            .CountAsync(a => !a.ReleaseRecords.Any(), cancellationToken);
        var releasedToday = await _db.ReleaseRecords.CountAsync(r => r.ReleasedAt >= today, cancellationToken);
        var facilities = await _db.WarehouseFacilities.CountAsync(f => f.IsActive, cancellationToken);
        return new WarehouseDashboardDto(stored, pendingReleases, releasedToday, facilities);
    }

    public async Task<IReadOnlyList<WarehouseFacilityDto>> ListFacilitiesAsync(CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var warehouseIds = await WarehouseStaffAccessHelper.GetAccessibleWarehouseIdsAsync(
            _db, _currentUser, user.Id, cancellationToken);

        if (warehouseIds.Count == 0)
        {
            return Array.Empty<WarehouseFacilityDto>();
        }

        var facilities = await _db.WarehouseFacilities.AsNoTracking()
            .Where(f => f.IsActive && warehouseIds.Contains(f.Id))
            .Include(f => f.Region)
            .Include(f => f.Province)
            .Include(f => f.City)
            .Include(f => f.Barangay)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        return facilities.Select(f => new WarehouseFacilityDto(
            f.Id,
            f.Code,
            f.Name,
            f.Location,
            f.Capacity,
            WarehouseAddressFormatter.Format(
                f.StreetAddress,
                f.Barangay?.Name,
                f.City?.Name,
                f.Province?.Name,
                f.Region?.Name,
                f.ZipCode ?? f.Barangay?.ZipCode,
                f.Location),
            f.Latitude,
            f.Longitude,
            f.Region?.Name,
            f.Province?.Name,
            f.City?.Name,
            f.Barangay?.Name,
            f.StreetAddress,
            f.ZipCode ?? f.Barangay?.ZipCode)).ToList();
    }

    public async Task<PagedResult<WarehouseInventoryListItemDto>> ListInventoryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.WarehouseInventories
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .Include(i => i.WarehouseFacility)
            .OrderByDescending(i => i.ReceivedAt);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = new List<WarehouseInventoryListItemDto>();
        foreach (var row in rows)
        {
            items.Add(await MapInventoryItemAsync(row, cancellationToken));
        }

        return new PagedResult<WarehouseInventoryListItemDto>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<ContainerListItemDto>> ListReceivableContainersAsync(CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var storedIds = _db.WarehouseInventories
            .Where(i => i.Status == WarehouseInventoryStatus.Stored)
            .Select(i => i.ContainerId);

        var containers = await _db.Containers
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .Include(c => c.Locations)
            .Include(c => c.DoctorInspections)
            .Where(c => !storedIds.Contains(c.Id) &&
                        c.Status == ContainerStatus.Inspected &&
                        c.DoctorInspections.Any(i => i.Status == DoctorInspectionStatus.Approved))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

        var items = new List<ContainerListItemDto>();
        foreach (var container in containers)
        {
            items.Add(await MapContainerWithMavCommodityAsync(container, cancellationToken));
        }

        return items;
    }

    public async Task<WarehouseInventoryListItemDto> ReceiveContainerAsync(ReceiveContainerRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .FirstOrDefaultAsync(c => c.Uuid == request.ContainerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (container.Status is not ContainerStatus.Inspected)
        {
            throw new ClientPortalException("INVALID_STATUS", "Container must be doctor-inspected before warehouse receive.");
        }

        var doctorApproved = await _db.ContainerDoctorInspections
            .AnyAsync(i => i.ContainerId == container.Id && i.Status == DoctorInspectionStatus.Approved, cancellationToken);
        if (!doctorApproved)
        {
            throw new ClientPortalException("DOCTOR_APPROVAL_REQUIRED", "Doctor inspection approval is required before warehouse receive.");
        }

        if (container.Status is ContainerStatus.Released)
        {
            throw new ClientPortalException("INVALID_STATUS", "Container has already been released.");
        }

        if (await _db.WarehouseInventories.AnyAsync(i => i.ContainerId == container.Id && i.Status == WarehouseInventoryStatus.Stored, cancellationToken))
        {
            throw new ClientPortalException("ALREADY_RECEIVED", "Container is already in warehouse inventory.");
        }

        var facility = await _db.WarehouseFacilities.FirstOrDefaultAsync(f => f.Id == request.WarehouseFacilityId && f.IsActive, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Warehouse facility not found.");

        await _importCompliance.ValidateCswReceiveAsync(container.Entry, container, facility.Id, cancellationToken);

        if (facility.Capacity > 0)
        {
            var storedCount = await _db.WarehouseInventories.CountAsync(
                i => i.WarehouseFacilityId == facility.Id && i.Status == WarehouseInventoryStatus.Stored,
                cancellationToken);
            if (storedCount >= facility.Capacity)
            {
                throw new ClientPortalException(
                    "COMPLIANCE_WAREHOUSE_CAPACITY",
                    "Warehouse is at capacity (DA MC 7.2).");
            }
        }

        var inventory = new WarehouseInventory
        {
            Uuid = Guid.NewGuid(),
            ContainerId = container.Id,
            WarehouseFacilityId = facility.Id,
            LocationCode = request.LocationCode,
            ReceivedByUserId = staff.Id,
            Status = WarehouseInventoryStatus.Stored
        };
        container.Status = ContainerStatus.AtWarehouse;
        container.WarehouseFacilityId = facility.Id;
        container.ArrivalTime ??= DateTime.UtcNow;
        container.Entry.CompliancePhase = EntryCompliancePhase.AtWarehouse;
        _db.WarehouseInventories.Add(inventory);
        var libraryCommodity = await EntryMavCommodityResolver.ResolveLibraryCommodityAsync(_db, container.Entry, cancellationToken);
        _db.StorageTallySheets.Add(new StorageTallySheet
        {
            WarehouseFacilityId = facility.Id,
            ContainerId = container.Id,
            EntryId = container.EntryId,
            Direction = "In",
            CommodityDescription = libraryCommodity?.MavHsLabel ?? libraryCommodity?.CommodityName,
            RecordedByUserId = staff.Id,
        });
        ComplianceTimelineRecorder.Add(
            _db,
            container.EntryId,
            "Compliance.Tis",
            "Warehouse check-in (TIS)",
            $"Container {container.ContainerNumber} received at {facility.Name}.",
            staff.Id);
        await _db.SaveChangesAsync(cancellationToken);

        inventory.WarehouseFacility = facility;
        inventory.Container = container;
        return await MapInventoryItemAsync(inventory, cancellationToken);
    }

    public async Task<IReadOnlyList<ReleaseAuthorizationListItemDto>> ListReleaseAuthorizationsAsync(CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var rows = await _db.ReleaseAuthorizations
            .Include(a => a.Entry).ThenInclude(e => e.Detail)
            .Include(a => a.Entry).ThenInclude(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .Include(a => a.ReleaseRecords)
            .OrderByDescending(a => a.AuthorizedAt)
            .ToListAsync(cancellationToken);

        var items = new List<ReleaseAuthorizationListItemDto>();
        foreach (var row in rows)
        {
            var lib = await EntryMavCommodityResolver.ResolveLibraryCommodityAsync(_db, row.Entry, cancellationToken);
            items.Add(new ReleaseAuthorizationListItemDto(
                row.Uuid,
                row.Entry.ReferenceNo,
                row.RecipientName,
                row.RecipientIdNumber,
                row.AuthorizedAt,
                row.ReleaseRecords.Count,
                lib is not null,
                lib?.HsCode,
                lib?.CommodityName,
                lib?.MavHsLabel));
        }

        return items;
    }

    public async Task<ReleaseAuthorizationListItemDto> CreateReleaseAuthorizationAsync(CreateReleaseAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries
            .Include(e => e.Detail)
            .Include(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .FirstOrDefaultAsync(e => e.Uuid == request.EntryUuid && e.Status == EntryStatus.Approved, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Approved entry not found.");

        var auth = new ReleaseAuthorization
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            AuthorizedByUserId = staff.Id,
            RecipientName = request.RecipientName.Trim(),
            RecipientIdNumber = request.RecipientIdNumber.Trim()
        };
        _db.ReleaseAuthorizations.Add(auth);
        await _db.SaveChangesAsync(cancellationToken);

        var lib = await EntryMavCommodityResolver.ResolveLibraryCommodityAsync(_db, entry, cancellationToken);
        return new ReleaseAuthorizationListItemDto(
            auth.Uuid,
            entry.ReferenceNo,
            auth.RecipientName,
            auth.RecipientIdNumber,
            auth.AuthorizedAt,
            0,
            lib is not null,
            lib?.HsCode,
            lib?.CommodityName,
            lib?.MavHsLabel);
    }

    public async Task<ReleaseRecordListItemDto> ExecuteReleaseAsync(ExecuteReleaseRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var inventory = await _db.WarehouseInventories
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(i => i.Container).ThenInclude(c => c.Entry).ThenInclude(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .FirstOrDefaultAsync(i => i.Uuid == request.InventoryUuid && i.Status == WarehouseInventoryStatus.Stored, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Stored inventory item not found.");

        var authorization = await _db.ReleaseAuthorizations
            .Include(a => a.Entry)
            .FirstOrDefaultAsync(a => a.Uuid == request.AuthorizationUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Release authorization not found.");

        if (authorization.EntryId != inventory.Container.EntryId)
        {
            throw new ClientPortalException("MISMATCH", "Release authorization does not match container entry.");
        }

        var record = new ReleaseRecord
        {
            Uuid = Guid.NewGuid(),
            WarehouseInventoryId = inventory.Id,
            ReleaseAuthorizationId = authorization.Id,
            ReleasedByUserId = staff.Id,
            RecipientSignaturePath = request.RecipientSignaturePath
        };
        inventory.Status = WarehouseInventoryStatus.Released;
        inventory.Container.Status = ContainerStatus.Released;
        _db.ReleaseRecords.Add(record);
        var releaseCommodity = await EntryMavCommodityResolver.ResolveLibraryCommodityAsync(
            _db,
            inventory.Container.Entry,
            cancellationToken);
        _db.StorageTallySheets.Add(new StorageTallySheet
        {
            WarehouseFacilityId = inventory.WarehouseFacilityId,
            ContainerId = inventory.ContainerId,
            EntryId = inventory.Container.EntryId,
            Direction = "Out",
            CommodityDescription = releaseCommodity?.MavHsLabel ?? releaseCommodity?.CommodityName,
            RecordedByUserId = staff.Id,
        });

        var entry = inventory.Container.Entry;
        var otherStored = await _db.WarehouseInventories.AnyAsync(
            i => i.Container.EntryId == entry.Id
                && i.Id != inventory.Id
                && i.Status == WarehouseInventoryStatus.Stored,
            cancellationToken);
        if (!otherStored)
        {
            entry.CompliancePhase = EntryCompliancePhase.Released;
        }

        ComplianceTimelineRecorder.Add(
            _db,
            entry.Id,
            "Compliance.Tos",
            "Warehouse check-out (TOS)",
            $"Container {inventory.Container.ContainerNumber} released to {authorization.RecipientName}.",
            staff.Id);

        await _db.SaveChangesAsync(cancellationToken);

        return new ReleaseRecordListItemDto(
            record.Uuid,
            inventory.Container.ContainerNumber,
            authorization.RecipientName,
            record.ReleasedAt);
    }

    private async Task<WarehouseInventoryListItemDto> MapInventoryItemAsync(
        WarehouseInventory inventory,
        CancellationToken cancellationToken)
    {
        var entry = inventory.Container.Entry;
        var library = await EntryMavCommodityResolver.ResolveLibraryCommodityAsync(_db, entry, cancellationToken);
        return new WarehouseInventoryListItemDto(
            inventory.Uuid,
            entry.Uuid,
            inventory.Container.ContainerNumber,
            entry.ReferenceNo,
            inventory.WarehouseFacility.Name,
            inventory.LocationCode,
            inventory.Status.ToString(),
            inventory.ReceivedAt,
            library is not null,
            library?.HsCode,
            library?.CommodityName,
            library?.MavHsLabel,
            library?.CategoryDescription,
            library?.VolumeKg);
    }

    private async Task<ContainerListItemDto> MapContainerWithMavCommodityAsync(
        Container container,
        CancellationToken cancellationToken)
    {
        var mapped = OpsDtoMapper.MapContainer(container);
        var library = await EntryMavCommodityResolver.ResolveLibraryCommodityAsync(_db, container.Entry, cancellationToken);
        return new ContainerListItemDto(
            mapped.Uuid,
            mapped.ContainerNumber,
            mapped.EntryUuid,
            mapped.EntryReference,
            mapped.Status,
            mapped.DepartureTime,
            mapped.ArrivalTime,
            mapped.LastLatitude,
            mapped.LastLongitude,
            mapped.LastLocationAt,
            mapped.AssignedDriverUuid,
            mapped.AssignedDriverName,
            mapped.AssignedVehiclePlate,
            library is not null,
            library?.HsCode,
            library?.CommodityName,
            library?.MavHsLabel,
            library?.CategoryDescription,
            library?.VolumeKg,
            mapped.Pickup,
            mapped.Destination);
    }
}

public class DriverOpsService : IDriverOpsService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IFileStorageService _fileStorage;
    private readonly IAgriTrackPushService _push;
    private readonly INotificationService _notifications;

    public DriverOpsService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IConfiguration configuration,
        IFileStorageService fileStorage,
        IAgriTrackPushService push,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _configuration = configuration;
        _fileStorage = fileStorage;
        _push = push;
        _notifications = notifications;
    }

    public async Task<DriverDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var containers = _db.Containers.Where(c => c.AssignedDriverUserId == driver.Id);
        var profile = await _db.DriverProfiles.FirstOrDefaultAsync(p => p.UserId == driver.Id, cancellationToken);
        return new DriverDashboardDto(
            await containers.CountAsync(cancellationToken),
            await containers.CountAsync(c => c.Status == ContainerStatus.InTransit, cancellationToken),
            profile?.CompletionPercentage ?? 0,
            profile?.FaceVerified ?? false);
    }

    public async Task<DriverProfileDto?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var profile = await _db.DriverProfiles
            .Include(p => p.Documents)
            .Include(p => p.User).ThenInclude(u => u.Profile)
            .Include(p => p.OperatorUser).ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(p => p.UserId == driver.Id, cancellationToken);
        return profile is null ? null : await MapProfileAsync(profile, cancellationToken);
    }

    public async Task<DriverProfileDto> UpdateProfileAsync(UpdateDriverProfileRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var profile = await _db.DriverProfiles
            .Include(p => p.Documents)
            .Include(p => p.User).ThenInclude(u => u.Profile)
            .Include(p => p.OperatorUser).ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(p => p.UserId == driver.Id, cancellationToken);
        if (profile is null)
        {
            profile = new DriverProfile { UserId = driver.Id };
            _db.DriverProfiles.Add(profile);
        }

        if (!string.IsNullOrWhiteSpace(request.FullName) && driver.Profile is not null)
        {
            var parts = request.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            driver.Profile.FirstName = parts.Length > 0 ? parts[0] : request.FullName.Trim();
            driver.Profile.LastName = parts.Length > 1 ? parts[1] : string.Empty;
        }

        profile.BirthDate = request.BirthDate?.Date ?? profile.BirthDate;
        profile.LicenseNumber = request.LicenseNumber?.Trim() ?? profile.LicenseNumber;
        profile.LicenseExpiryDate = request.LicenseExpiryDate ?? profile.LicenseExpiryDate;
        profile.VehicleType = request.VehicleType?.Trim() ?? profile.VehicleType;
        profile.VehicleRegistration = request.VehicleRegistration?.Trim() ?? profile.VehicleRegistration;
        profile.PhoneNumber = request.PhoneNumber?.Trim() ?? profile.PhoneNumber;
        profile.EmergencyContact = request.EmergencyContact?.Trim() ?? profile.EmergencyContact;
        profile.EmergencyPhone = request.EmergencyPhone?.Trim() ?? profile.EmergencyPhone;
        profile.RegionId = request.RegionId ?? profile.RegionId;
        profile.ProvinceId = request.ProvinceId ?? profile.ProvinceId;
        profile.CityId = request.CityId ?? profile.CityId;
        profile.BarangayId = request.BarangayId ?? profile.BarangayId;
        profile.ZipCode = request.ZipCode?.Trim() ?? profile.ZipCode;
        profile.StreetAddress = request.StreetAddress?.Trim() ?? profile.StreetAddress;
        profile.Address = request.Address?.Trim()
            ?? string.Join(", ", new[] { profile.StreetAddress, profile.ZipCode }.Where(x => !string.IsNullOrWhiteSpace(x)));
        profile.CompletionPercentage = CalculateCompletion(profile);
        profile.SubmittedAt ??= DateTime.UtcNow;
        profile.ApprovedAt ??= DateTime.UtcNow;

        _db.DriverProfileHistories.Add(new DriverProfileHistory
        {
            DriverProfile = profile,
            ChangedByUserId = driver.Id,
            ChangeDescription = "Profile updated"
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await MapProfileAsync(profile, cancellationToken);
    }

    public async Task<IReadOnlyList<ContainerListItemDto>> ListAssignedContainersAsync(CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var containers = await _db.Containers
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.FinalWarehouseFacility)
            .Include(c => c.Locations)
            .Where(c => c.AssignedDriverUserId == driver.Id)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
        var results = new List<ContainerListItemDto>();
        foreach (var container in containers)
        {
            results.Add(await MapDriverContainerAsync(container, cancellationToken));
        }

        return results;
    }

    public async Task<ContainerListItemDto> UpdateContainerStatusAsync(Guid containerUuid, UpdateContainerStatusRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<ContainerStatus>(request.Status, true, out var status))
        {
            throw new ClientPortalException("INVALID_STATUS", "Invalid container status.");
        }

        var container = await _db.Containers
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.FinalWarehouseFacility)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        if (status is ContainerStatus.AtWarehouse
            or ContainerStatus.UnderInspection
            or ContainerStatus.Inspected
            or ContainerStatus.Released)
        {
            throw new ClientPortalException(
                "FORBIDDEN",
                "Delivery cannot be ended from the driver app. Present the delivery QR for NMIS/Doctor scanning at the warehouse.");
        }

        if (status != ContainerStatus.InTransit)
        {
            throw new ClientPortalException("INVALID_STATUS", "Drivers may only start in-transit delivery from this endpoint.");
        }

        if (container.Status is not (ContainerStatus.PickedUp or ContainerStatus.Assigned or ContainerStatus.ReadyForTransport or ContainerStatus.Inspected))
        {
            throw new ClientPortalException("INVALID_STATUS", "Confirm pickup before starting in-transit delivery.");
        }

        if (status == ContainerStatus.InTransit)
        {
            if (request.Latitude is null || request.Longitude is null)
            {
                throw new ClientPortalException("LOCATION_REQUIRED", "Current GPS is required to start warehouse navigation.");
            }

            var pickupTarget = ContainerDeliveryPointHelper.ResolvePickup(container, container.Entry);
            var distanceToPickup = GeoDistanceHelper.DistanceMeters(
                request.Latitude.Value,
                request.Longitude.Value,
                pickupTarget.Latitude,
                pickupTarget.Longitude);
            if (distanceToPickup > GeoDistanceHelper.DeliveryStopGeofenceMeters)
            {
                throw new ClientPortalException(
                    "OUTSIDE_GEOFENCE",
                    $"Move within {GeoDistanceHelper.DeliveryStopGeofenceMeters:0} m of the pickup point before navigating to the warehouse.");
            }
        }

        container.Status = status;
        if (container.DepartureTime is null)
        {
            container.DepartureTime = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapDriverContainerAsync(container, cancellationToken);
    }

    public async Task<ContainerListItemDto> ConfirmContainerPickupAsync(
        Guid containerUuid,
        ConfirmContainerPickupRequest request,
        Stream? geotagPhoto = null,
        string? geotagFileName = null,
        DateTime? capturedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.FinalWarehouseFacility)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        if (container.Status is ContainerStatus.AtWarehouse or ContainerStatus.Released)
        {
            throw new ClientPortalException("INVALID_STATUS", "This delivery is already completed.");
        }

        var pickupTarget = ContainerDeliveryPointHelper.ResolvePickup(container, container.Entry);
        var distanceToPickup = GeoDistanceHelper.DistanceMeters(
            request.Latitude,
            request.Longitude,
            pickupTarget.Latitude,
            pickupTarget.Longitude);
        if (distanceToPickup > GeoDistanceHelper.DeliveryStopGeofenceMeters)
        {
            throw new ClientPortalException(
                "OUTSIDE_GEOFENCE",
                $"Move within {GeoDistanceHelper.DeliveryStopGeofenceMeters:0} m of the pickup point to confirm.");
        }

        if (RequireGeotagProof() && geotagPhoto is null)
        {
            throw new ClientPortalException(
                "GEOTAG_REQUIRED",
                "Take a geotagged photo at the pickup point before confirming.");
        }

        var label = string.IsNullOrWhiteSpace(request.Label)
            ? pickupTarget.Label
            : request.Label.Trim();

        if (geotagPhoto is not null)
        {
            await SaveDriverGeotagProofAsync(
                container,
                ContainerInspectionPhotoType.DriverPickupGeotag,
                geotagPhoto,
                geotagFileName ?? "pickup-geotag.jpg",
                request.Latitude,
                request.Longitude,
                capturedAtUtc ?? DateTime.UtcNow,
                cancellationToken);
        }

        // Keep planned pickup coordinates; only record driver GPS in the trail.
        ContainerDeliveryPointHelper.SetPickup(container, label, pickupTarget.Latitude, pickupTarget.Longitude);
        container.Status = ContainerStatus.PickedUp;
        _db.ContainerLocations.Add(new ContainerLocation
        {
            ContainerId = container.Id,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RecordedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await MapDriverContainerAsync(container, cancellationToken);
    }

    public async Task RecordContainerLocationAsync(Guid containerUuid, RecordContainerLocationRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers.FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        _db.ContainerLocations.Add(new ContainerLocation
        {
            ContainerId = container.Id,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RecordedAt = ContainerLocationTimestampHelper.NormalizeRecordedAt(request.RecordedAt),
            CapturedOffline = request.CapturedOffline,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RecordContainerLocationsBatchAsync(
        Guid containerUuid,
        RecordContainerLocationsBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Points is null || request.Points.Count == 0)
        {
            return 0;
        }

        if (request.Points.Count > 250)
        {
            throw new ClientPortalException("VALIDATION", "At most 250 location points per batch.");
        }

        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers.FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        foreach (var point in request.Points.OrderBy(p => p.RecordedAt))
        {
            _db.ContainerLocations.Add(new ContainerLocation
            {
                ContainerId = container.Id,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                RecordedAt = ContainerLocationTimestampHelper.NormalizeRecordedAt(point.RecordedAt),
                CapturedOffline = point.CapturedOffline,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return request.Points.Count;
    }

    public async Task<ContainerTrackDto> GetContainerTrackAsync(Guid containerUuid, CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.FinalWarehouseFacility)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        var pickup = ResolveTrackPickup(container);
        var destination = await ResolveTrackDestinationAsync(container, cancellationToken);
        var trail = container.Locations
            .Where(l => l.Latitude != null && l.Longitude != null)
            .OrderBy(l => l.RecordedAt)
            .Select(l => new ContainerLocationPointDto(l.Latitude!.Value, l.Longitude!.Value, l.RecordedAt))
            .ToList();
        var last = trail.LastOrDefault();

        return new ContainerTrackDto(
            container.Uuid,
            container.ContainerNumber,
            container.Entry.ReferenceNo,
            container.Status.ToString(),
            last?.Latitude,
            last?.Longitude,
            pickup,
            destination,
            trail,
            container.WarehouseHandoffEligibleAt is not null &&
            container.Status is ContainerStatus.InTransit &&
            !DeliveryComplianceHoldHelper.HasActiveHold(container),
            DeliveryComplianceHoldHelper.HasActiveHold(container),
            DeliveryComplianceHoldHelper.HasActiveHold(container)
                ? container.DeliveryComplianceHoldReason
                : null);
    }

    private static ContainerTrackDestinationDto ResolveTrackPickup(Container container)
    {
        var pickup = ContainerDeliveryPointHelper.ResolvePickup(container, container.Entry);
        return new ContainerTrackDestinationDto(pickup.Label, pickup.Latitude, pickup.Longitude);
    }

    private async Task<ContainerListItemDto> MapDriverContainerAsync(Container container, CancellationToken cancellationToken)
    {
        var mapped = OpsDtoMapper.MapContainer(container);
        var pickup = ContainerDeliveryPointHelper.ResolvePickup(container, container.Entry);
        var destination = await ResolveTrackDestinationAsync(container, cancellationToken);
        return new ContainerListItemDto(
            mapped.Uuid,
            mapped.ContainerNumber,
            mapped.EntryUuid,
            mapped.EntryReference,
            mapped.Status,
            mapped.DepartureTime,
            mapped.ArrivalTime,
            mapped.LastLatitude,
            mapped.LastLongitude,
            mapped.LastLocationAt,
            mapped.AssignedDriverUuid,
            mapped.AssignedDriverName,
            mapped.AssignedVehiclePlate,
            mapped.MavLibraryLinked,
            mapped.MavHsCode,
            mapped.MavCommodityName,
            mapped.MavHsLabel,
            mapped.MavCategoryName,
            mapped.VolumeKg,
            new ContainerDeliveryStopDto(pickup.Label, pickup.Latitude, pickup.Longitude),
            new ContainerDeliveryStopDto(destination.Name, destination.Latitude, destination.Longitude),
            container.WarehouseHandoffEligibleAt is not null &&
            container.Status == ContainerStatus.InTransit &&
            !DeliveryComplianceHoldHelper.HasActiveHold(container),
            DeliveryComplianceHoldHelper.HasActiveHold(container),
            DeliveryComplianceHoldHelper.HasActiveHold(container)
                ? container.DeliveryComplianceHoldReason
                : null);
    }

    private async Task<ContainerTrackDestinationDto> ResolveTrackDestinationAsync(Container container, CancellationToken cancellationToken)
    {
        var storedDestination = ContainerDeliveryPointHelper.TryGetStoredDestination(container);
        if (storedDestination is not null)
        {
            return new ContainerTrackDestinationDto(
                storedDestination.Value.Label,
                storedDestination.Value.Latitude,
                storedDestination.Value.Longitude);
        }

        var finalFacility = container.Entry.FinalWarehouseFacility;
        if (finalFacility?.Latitude is not null && finalFacility.Longitude is not null)
        {
            return new ContainerTrackDestinationDto(finalFacility.Name, finalFacility.Latitude.Value, finalFacility.Longitude.Value);
        }

        var booking = await _db.WarehouseBookings
            .Include(b => b.WarehouseFacility)
            .Where(b => b.UserId == container.Entry.UserId)
            .Where(b => b.ContainerReference == container.ContainerNumber || b.ContainerReference == string.Empty)
            .OrderByDescending(b => b.ScheduledDate)
            .FirstOrDefaultAsync(cancellationToken);

        var facility = booking?.WarehouseFacility;
        if (facility?.Latitude is null || facility.Longitude is null)
        {
            facility = await _db.WarehouseFacilities
                .Where(w => w.IsActive && w.Latitude != null && w.Longitude != null)
                .OrderBy(w => w.Code == "WH-MNL" ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (facility?.Latitude is null || facility.Longitude is null)
        {
            throw new ClientPortalException("NO_DESTINATION", "Warehouse destination coordinates are not configured.");
        }

        return new ContainerTrackDestinationDto(facility.Name, facility.Latitude.Value, facility.Longitude.Value);
    }

    public async Task<DriverDeliveryHandoffQrDto> GetDeliveryHandoffQrAsync(
        Guid containerUuid,
        CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        if (container.Status is not ContainerStatus.InTransit and not ContainerStatus.PickedUp)
        {
            throw new ClientPortalException("INVALID_STATUS", "Delivery QR is only available while the trip is in progress at the assigned warehouse.");
        }

        if (DeliveryComplianceHoldHelper.HasActiveHold(container))
        {
            throw new ClientPortalException(
                "COMPLIANCE_HOLD",
                DeliveryComplianceHoldHelper.HoldUserMessage(container.DeliveryComplianceHoldReason));
        }

        if (container.WarehouseHandoffEligibleAt is null)
        {
            throw new ClientPortalException(
                "HANDOFF_NOT_READY",
                "Confirm arrival at your assigned warehouse first. The QR code is generated only when you are at the correct warehouse.");
        }

        var destination = await ResolveTrackDestinationAsync(container, cancellationToken);
        var last = container.Locations
            .Where(l => l.Latitude is not null && l.Longitude is not null)
            .OrderByDescending(l => l.RecordedAt)
            .FirstOrDefault();
        if (last is not null)
        {
            var dist = GeoDistanceHelper.DistanceMeters(
                last.Latitude!.Value,
                last.Longitude!.Value,
                destination.Latitude,
                destination.Longitude);
            if (dist > GeoDistanceHelper.DeliveryStopGeofenceMeters)
            {
                throw new ClientPortalException(
                    "OUTSIDE_GEOFENCE",
                    "You must stay within the assigned warehouse area. Mis-warehouse delivery is not allowed.");
            }
        }

        var now = DateTime.UtcNow;
        var expiresAt = now.Add(DeliveryHandoffQrCodec.MaxAge);
        var nonce = Guid.NewGuid();
        var signingKey = TransportQrSigningKeyResolver.Resolve(_configuration);
        var handoffPayload = new DeliveryHandoffQrPayload(
            nonce,
            container.Uuid,
            container.ContainerNumber,
            container.Entry.ReferenceNo,
            now);
        var qrPayload = DeliveryHandoffQrCodec.Encode(handoffPayload, signingKey);
        var qrCodeData = QrCodeGenerator.ToBase64Png(qrPayload);

        var staleTokens = await _db.ContainerHandoffTokens
            .Where(t => t.ContainerId == container.Id && t.ConsumedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var stale in staleTokens)
        {
            stale.ExpiresAt = now;
        }

        _db.ContainerHandoffTokens.Add(new ContainerHandoffToken
        {
            Nonce = nonce,
            ContainerId = container.Id,
            IssuedByDriverUserId = driver.Id,
            IssuedAt = now,
            ExpiresAt = expiresAt,
            QrPayload = qrPayload,
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new DriverDeliveryHandoffQrDto(
            container.Uuid,
            container.ContainerNumber,
            container.Entry.ReferenceNo,
            qrPayload,
            qrCodeData,
            "This QR refreshes automatically and cannot be reused from a screenshot. Only NMIS/Doctor can scan it to end the trip.",
            expiresAt,
            (int)DeliveryHandoffQrCodec.MaxAge.TotalSeconds - 5);
    }

    public async Task<DriverTransportQrPreviewDto> PreviewTransportQrAsync(
        ScanTransportQrRequest request,
        CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var payload = VerifyTransportQr(request.QrData);
        var container = await LoadContainerForTransportAsync(payload.ContainerUuid, cancellationToken);
        var destination = await ResolveTrackDestinationAsync(container, cancellationToken);
        var (canAccept, blockReason) = EvaluateDriverAcceptance(container);

        return new DriverTransportQrPreviewDto(
            container.Uuid,
            container.ContainerNumber,
            container.Entry.ReferenceNo,
            destination.Name,
            null,
            destination.Latitude,
            destination.Longitude,
            payload.ScheduledWarehouseDate,
            container.Status.ToString(),
            canAccept,
            blockReason);
    }

    public async Task<ContainerListItemDto> AcceptDeliveryFromQrAsync(
        ScanTransportQrRequest request,
        CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var payload = VerifyTransportQr(request.QrData);
        var container = await LoadContainerForTransportAsync(payload.ContainerUuid, cancellationToken);
        var (canAccept, blockReason) = EvaluateDriverAcceptance(container);
        if (!canAccept)
        {
            throw new ClientPortalException("CANNOT_ACCEPT", blockReason ?? "This delivery cannot be accepted.");
        }

        container.AssignedDriverUserId = driver.Id;
        container.Status = ContainerStatus.Assigned;
        if (container.ClaimedByUserId is null)
        {
            container.ClaimedByUserId = driver.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _push.NotifyDriverAssignmentAsync(driver.Id, container.ContainerNumber, container.Uuid, cancellationToken);
        return OpsDtoMapper.MapContainer(container);
    }

    public async Task<DriverWarehouseCheckInResultDto> CheckInAtWarehouseAsync(
        Guid containerUuid,
        DriverWarehouseCheckInRequest request,
        Stream? geotagPhoto = null,
        string? geotagFileName = null,
        DateTime? capturedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        var destination = await ResolveTrackDestinationAsync(container, cancellationToken);
        var distance = GeoDistanceHelper.DistanceMeters(
            request.Latitude,
            request.Longitude,
            destination.Latitude,
            destination.Longitude);
        if (container.Status is ContainerStatus.AtWarehouse or ContainerStatus.Released or ContainerStatus.Inspected)
        {
            throw new ClientPortalException("INVALID_STATUS", "This delivery is already at the warehouse.");
        }

        if (container.Status is not ContainerStatus.InTransit and not ContainerStatus.PickedUp and not ContainerStatus.UnderInspection)
        {
            throw new ClientPortalException("INVALID_STATUS", "Check-in is only available while delivery is in progress.");
        }

        if (distance > GeoDistanceHelper.DeliveryStopGeofenceMeters)
        {
            DeliveryComplianceHoldHelper.ApplyHold(container, DeliveryComplianceEventTypes.WrongWarehouse);
            await RecordDeliveryComplianceEventAsync(
                container.Id,
                driver.Id,
                DeliveryComplianceEventTypes.WrongWarehouse,
                request.Latitude,
                request.Longitude,
                $"distance_to_assigned_warehouse_m={distance:0}",
                cancellationToken,
                applyHold: false);
            await _db.SaveChangesAsync(cancellationToken);
            throw new ClientPortalException(
                "WRONG_WAREHOUSE",
                "Wrong warehouse location. DA has been alerted. You cannot end this trip until DA authority clears the delivery.");
        }

        if (RequireGeotagProof() && geotagPhoto is null)
        {
            throw new ClientPortalException(
                "GEOTAG_REQUIRED",
                "Take a geotagged photo at the warehouse before confirming arrival.");
        }

        if (geotagPhoto is not null)
        {
            await SaveDriverGeotagProofAsync(
                container,
                ContainerInspectionPhotoType.DriverWarehouseGeotag,
                geotagPhoto,
                geotagFileName ?? "warehouse-geotag.jpg",
                request.Latitude,
                request.Longitude,
                capturedAtUtc ?? DateTime.UtcNow,
                cancellationToken);
        }

        _db.ContainerLocations.Add(new ContainerLocation
        {
            ContainerId = container.Id,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RecordedAt = DateTime.UtcNow
        });

        var withinGeofence = distance <= GeoDistanceHelper.DeliveryStopGeofenceMeters;
        if (container.Status is ContainerStatus.InTransit or ContainerStatus.PickedUp)
        {
            container.WarehouseHandoffEligibleAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var mapped = await MapDriverContainerAsync(container, cancellationToken);
        return new DriverWarehouseCheckInResultDto(withinGeofence, distance, mapped);
    }

    public async Task ReportDeliveryComplianceAsync(
        Guid containerUuid,
        ReportDeliveryComplianceRequest request,
        CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        var eventType = request.EventType.Trim().ToLowerInvariant();
        if (eventType is not DeliveryComplianceEventTypes.Misroute
            and not DeliveryComplianceEventTypes.WrongWarehouse
            and not DeliveryComplianceEventTypes.OffRouteBlocked)
        {
            throw new ClientPortalException("INVALID_EVENT", "Unsupported compliance event type.");
        }

        var details = request.Details?.Trim();
        if (request.OffRouteMeters is not null)
        {
            details = string.IsNullOrWhiteSpace(details)
                ? $"off_route_m={request.OffRouteMeters:0}"
                : $"{details} | off_route_m={request.OffRouteMeters:0}";
        }

        await RecordDeliveryComplianceEventAsync(
            container.Id,
            driver.Id,
            eventType,
            request.Latitude,
            request.Longitude,
            details,
            cancellationToken);
    }

    private async Task RecordDeliveryComplianceEventAsync(
        long containerId,
        long? driverUserId,
        string eventType,
        decimal? latitude,
        decimal? longitude,
        string? details,
        CancellationToken cancellationToken,
        bool applyHold = true)
    {
        var since = DateTime.UtcNow.AddMinutes(-10);
        var duplicate = await _db.DeliveryComplianceEvents.AnyAsync(
            e => e.ContainerId == containerId &&
                 e.EventType == eventType &&
                 e.RecordedAt >= since,
            cancellationToken);

        if (!duplicate)
        {
            _db.DeliveryComplianceEvents.Add(new DeliveryComplianceEvent
            {
                ContainerId = containerId,
                DriverUserId = driverUserId,
                EventType = eventType,
                Latitude = latitude,
                Longitude = longitude,
                Details = details,
                RecordedAt = DateTime.UtcNow,
            });
        }

        if (applyHold && DeliveryComplianceHoldHelper.ShouldApplyHoldForEvent(eventType))
        {
            var container = await _db.Containers.FirstOrDefaultAsync(c => c.Id == containerId, cancellationToken);
            if (container is not null)
            {
                DeliveryComplianceHoldHelper.ApplyHold(container, eventType);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContainerListItemDto> ResetDeliveryTestAsync(
        Guid containerUuid,
        ResetDriverDeliveryTestRequest? request,
        CancellationToken cancellationToken = default)
    {
        var allowReset = _configuration.GetValue("AgriTrack:AllowDeliveryTestReset", true);
        if (!allowReset)
        {
            throw new ClientPortalException("FORBIDDEN", "Delivery test reset is disabled on this server.");
        }

        var user = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var isAdmin = _currentUser.IsInRole("ROLE_ADMIN");

        var query = _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.FinalWarehouseFacility)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(c => c.AssignedDriverUserId == user.Id);
        }

        var container = await query.FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        if (container.AssignedDriverUserId is null && !isAdmin)
        {
            throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");
        }

        var seedLat = request?.Latitude ?? DriverDeliveryTestReset.SeedLatitude;
        var seedLng = request?.Longitude ?? DriverDeliveryTestReset.SeedLongitude;

        _db.ContainerLocations.RemoveRange(container.Locations);
        ContainerDeliveryPointHelper.ClearStoredPickup(container);
        ContainerDeliveryPointHelper.ClearStoredDestination(container);
        ContainerDeliveryPointHelper.SetPickup(
            container,
            DriverDeliveryTestReset.PickupLabel,
            DriverDeliveryTestReset.PickupLatitude,
            DriverDeliveryTestReset.PickupLongitude);
        ContainerDeliveryPointHelper.SetDestination(
            container,
            DriverDeliveryTestReset.WarehouseLabel,
            DriverDeliveryTestReset.WarehouseLatitude,
            DriverDeliveryTestReset.WarehouseLongitude);

        container.Status = ContainerStatus.Assigned;
        container.DepartureTime = null;
        container.ArrivalTime = null;
        if (container.Entry.CompliancePhase is EntryCompliancePhase.AtWarehouse or EntryCompliancePhase.Released)
        {
            container.Entry.CompliancePhase = EntryCompliancePhase.PortCleared;
        }

        _db.ContainerLocations.Add(new ContainerLocation
        {
            ContainerId = container.Id,
            Latitude = seedLat,
            Longitude = seedLng,
            RecordedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await MapDriverContainerAsync(container, cancellationToken);
    }

    public async Task<DriverDocumentDto> UploadDocumentAsync(
        string documentType,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<DriverDocumentType>(documentType, true, out var parsedType))
        {
            throw new ClientPortalException("INVALID_DOCUMENT", "Unsupported document type.");
        }

        var profile = await _db.DriverProfiles
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.UserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("PROFILE_REQUIRED", "Complete driver profile before uploading documents.");

        var (storedFileName, _) = await _fileStorage.SaveAsync(
            fileStream,
            $"driver-documents/{driver.Uuid}",
            fileName,
            cancellationToken);

        var existing = profile.Documents.FirstOrDefault(d => d.DocumentType == parsedType);
        if (existing is null)
        {
            existing = new DriverDocument
            {
                DriverProfileId = profile.Id,
                DocumentType = parsedType,
                OriginalFileName = fileName,
                StoredFileName = storedFileName,
            };
            profile.Documents.Add(existing);
        }
        else
        {
            existing.OriginalFileName = fileName;
            existing.StoredFileName = storedFileName;
        }

        profile.CompletionPercentage = CalculateCompletion(profile);
        await _db.SaveChangesAsync(cancellationToken);
        return new DriverDocumentDto(parsedType.ToString(), fileName, existing.CreatedAt);
    }

    public async Task<DriverDocumentFileDto> DownloadDocumentAsync(
        string documentType,
        CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<DriverDocumentType>(documentType, true, out var parsedType))
        {
            throw new ClientPortalException("INVALID_DOCUMENT", "Unsupported document type.");
        }

        var profile = await _db.DriverProfiles
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.UserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("PROFILE_REQUIRED", "Driver profile not found.");

        var document = profile.Documents.FirstOrDefault(d => d.DocumentType == parsedType)
            ?? throw new ClientPortalException("NOT_FOUND", "Document not uploaded yet.");

        var folder = $"driver-documents/{driver.Uuid}";
        var path = _fileStorage.GetPhysicalPath(folder, document.StoredFileName);
        if (!File.Exists(path))
        {
            throw new ClientPortalException("NOT_FOUND", "Document file not found.");
        }

        var contentType = ResolveDriverDocumentContentType(document.OriginalFileName);
        return new DriverDocumentFileDto(File.OpenRead(path), contentType, document.OriginalFileName);
    }

    public async Task<DriverProfileDto> SubmitFaceVerificationAsync(
        SubmitFaceVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var profile = await _db.DriverProfiles
            .Include(p => p.Documents)
            .Include(p => p.User).ThenInclude(u => u.Profile)
            .Include(p => p.OperatorUser).ThenInclude(u => u!.Profile)
            .FirstOrDefaultAsync(p => p.UserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("PROFILE_REQUIRED", "Driver profile not found.");

        var hasLicenseFront = profile.Documents.Any(d => d.DocumentType == DriverDocumentType.LicenseFront);
        var hasSelfie = profile.Documents.Any(d => d.DocumentType == DriverDocumentType.Selfie);
        if (!hasLicenseFront || !hasSelfie)
        {
            throw new ClientPortalException("DOCUMENTS_REQUIRED", "Upload license front and selfie before face verification.");
        }

        profile.FaceVerified = true;
        profile.FaceVerifiedAt = DateTime.UtcNow;
        profile.CompletionPercentage = CalculateCompletion(profile);
        _db.FaceVerificationLogs.Add(new FaceVerificationLog
        {
            UserId = driver.Id,
            Success = true,
            Confidence = request.Confidence,
            Notes = request.Notes,
        });
        await _db.SaveChangesAsync(cancellationToken);
        return await MapProfileAsync(profile, cancellationToken);
    }

    private TransportQrPayload VerifyTransportQr(string qrData)
    {
        if (string.IsNullOrWhiteSpace(qrData))
        {
            throw new ClientPortalException("QR_REQUIRED", "QR data is required.");
        }

        try
        {
            return TransportQrCodec.Verify(qrData.Trim(), TransportQrSigningKeyResolver.Resolve(_configuration));
        }
        catch (InvalidOperationException ex)
        {
            throw new ClientPortalException("INVALID_QR", ex.Message);
        }
    }

    private async Task<Container> LoadContainerForTransportAsync(Guid containerUuid, CancellationToken cancellationToken)
    {
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .Include(c => c.TransportTags)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        var tag = container.TransportTags.OrderByDescending(t => t.TaggedAt).FirstOrDefault();
        if (tag is null || string.IsNullOrWhiteSpace(tag.QrPayload))
        {
            throw new ClientPortalException("NOT_TAGGED", "Container does not have an active transport tag.");
        }

        return container;
    }

    private static (bool CanAccept, string? BlockReason) EvaluateDriverAcceptance(Container container)
    {
        if (container.Status is ContainerStatus.Released)
        {
            return (false, "Container has already been released.");
        }

        if (container.Status is ContainerStatus.AtWarehouse or ContainerStatus.Inspected)
        {
            return (false, "Container has already arrived at the warehouse.");
        }

        if (container.AssignedDriverUserId is not null
            && container.Status is not ContainerStatus.Assigned and not ContainerStatus.AwaitingConfirmation)
        {
            return (false, "Another driver is already assigned to this container.");
        }

        if (container.Status is not ContainerStatus.AwaitingConfirmation and not ContainerStatus.Assigned and not ContainerStatus.ReadyForTransport)
        {
            return (false, "Container is not ready for driver acceptance.");
        }

        return (true, null);
    }

    private static int CalculateCompletion(DriverProfile profile)
    {
        var fields = new object?[]
        {
            profile.LicenseNumber,
            profile.LicenseExpiryDate,
            profile.PhoneNumber,
            profile.StreetAddress,
            profile.RegionId,
            profile.BirthDate,
            profile.Documents.Any(d => d.DocumentType == DriverDocumentType.LicenseFront),
            profile.Documents.Any(d => d.DocumentType == DriverDocumentType.LicenseBack),
            profile.Documents.Any(d => d.DocumentType == DriverDocumentType.Selfie),
            profile.FaceVerified,
        };
        var filled = fields.Count(f => f is not null && f is not false && (f is not string s || !string.IsNullOrWhiteSpace(s)));
        return (int)Math.Round(filled * 100.0 / fields.Length);
    }

    private static Task<DriverProfileDto> MapProfileAsync(DriverProfile profile, CancellationToken cancellationToken)
    {
        var fullName = profile.User.Profile is null
            ? profile.User.Email
            : $"{profile.User.Profile.FirstName} {profile.User.Profile.LastName}".Trim();
        var operatorName = profile.OperatorUser?.Profile is null
            ? null
            : $"{profile.OperatorUser.Profile.FirstName} {profile.OperatorUser.Profile.LastName}".Trim();

        var documents = profile.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DriverDocumentDto(d.DocumentType.ToString(), d.OriginalFileName, d.CreatedAt))
            .ToList();

        string? profilePhotoUrl = null;
        if (profile.Documents.Any(d => d.DocumentType == DriverDocumentType.Selfie))
        {
            profilePhotoUrl = "/api/v1/ops/driver/profile/photo";
        }

        return Task.FromResult(new DriverProfileDto(
            fullName,
            profile.BirthDate,
            profile.LicenseNumber,
            profile.LicenseExpiryDate,
            profile.VehicleType,
            profile.VehicleRegistration,
            profile.PhoneNumber,
            profile.EmergencyContact,
            profile.EmergencyPhone,
            profile.Address,
            profile.RegionId,
            profile.ProvinceId,
            profile.CityId,
            profile.BarangayId,
            profile.ZipCode,
            profile.StreetAddress,
            operatorName,
            documents,
            profile.CompletionPercentage,
            profile.FaceVerified,
            profile.SubmittedAt,
            profile.ApprovedAt,
            profilePhotoUrl));
    }

    private static string ResolveDriverDocumentContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };
    }

    private bool RequireGeotagProof() =>
        _configuration.GetValue("AgriTrack:RequireGeotagProof", true);

    private async Task SaveDriverGeotagProofAsync(
        Container container,
        ContainerInspectionPhotoType photoType,
        Stream photoStream,
        string fileName,
        decimal latitude,
        decimal longitude,
        DateTime capturedAtUtc,
        CancellationToken cancellationToken)
    {
        using var raw = new MemoryStream();
        await photoStream.CopyToAsync(raw, cancellationToken);
        var stamped = GeotagImageStamper.StampJpeg(raw.ToArray(), latitude, longitude, capturedAtUtc);
        await using var stampedStream = new MemoryStream(stamped);
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "geotag.jpg" : fileName.Trim();
        var (storedFileName, _) = await _fileStorage.SaveAsync(
            stampedStream,
            $"container-delivery-geotag/{container.Uuid}",
            safeName,
            cancellationToken);

        var existing = await _db.ContainerInspectionPhotos
            .FirstOrDefaultAsync(p => p.ContainerId == container.Id && p.PhotoType == photoType, cancellationToken);

        if (existing is null)
        {
            existing = new ContainerInspectionPhoto
            {
                Uuid = Guid.NewGuid(),
                ContainerId = container.Id,
                EntryId = container.EntryId,
                PhotoType = photoType,
                OriginalFileName = safeName,
                StoredFileName = storedFileName,
                ContentType = "image/jpeg",
                CaptureLatitude = latitude,
                CaptureLongitude = longitude,
                CapturedAtUtc = capturedAtUtc,
                ReviewDecision = EvaluationDecision.Approved,
            };
            _db.ContainerInspectionPhotos.Add(existing);
        }
        else
        {
            existing.OriginalFileName = safeName;
            existing.StoredFileName = storedFileName;
            existing.ContentType = "image/jpeg";
            existing.CaptureLatitude = latitude;
            existing.CaptureLongitude = longitude;
            existing.CapturedAtUtc = capturedAtUtc;
            existing.ReviewDecision = EvaluationDecision.Approved;
        }
    }

}

public class MobileSyncService : IMobileSyncService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDriverOpsService _driverOps;

    public MobileSyncService(AgriCheckDbContext db, ICurrentUserService currentUser, IDriverOpsService driverOps)
    {
        _db = db;
        _currentUser = currentUser;
        _driverOps = driverOps;
    }

    public async Task<MobileSyncPushResultDto> PushAsync(MobileSyncPushRequest request, CancellationToken cancellationToken = default)
    {
        var user = await OpsContextHelper.RequireMobileUserAsync(_db, _currentUser, cancellationToken);
        var results = new List<MobileSyncItemResultDto>();
        var processed = 0;
        var failed = 0;

        foreach (var item in request.Items)
        {
            var existing = await _db.OfflineSyncQueue.FirstOrDefaultAsync(q => q.UserId == user.Id && q.ClientId == item.ClientId, cancellationToken);
            if (existing is not null && existing.Status == OfflineSyncStatus.Completed)
            {
                results.Add(new MobileSyncItemResultDto(item.ClientId, "duplicate", null));
                continue;
            }

            var queueItem = existing ?? new OfflineSyncQueue
            {
                Uuid = Guid.NewGuid(),
                UserId = user.Id,
                ClientId = item.ClientId,
                EntityType = item.EntityType,
                PayloadJson = item.PayloadJson
            };

            if (existing is null)
            {
                _db.OfflineSyncQueue.Add(queueItem);
            }
            else
            {
                queueItem.PayloadJson = item.PayloadJson;
                queueItem.Status = OfflineSyncStatus.Pending;
            }

            try
            {
                await ProcessQueueItemAsync(queueItem, cancellationToken);
                queueItem.Status = OfflineSyncStatus.Completed;
                queueItem.SyncedAt = DateTime.UtcNow;
                queueItem.ErrorMessage = null;
                processed++;
                results.Add(new MobileSyncItemResultDto(item.ClientId, "completed", null));
            }
            catch (ClientPortalException ex)
            {
                queueItem.Status = OfflineSyncStatus.Failed;
                queueItem.ErrorMessage = ex.Message;
                failed++;
                results.Add(new MobileSyncItemResultDto(item.ClientId, "failed", ex.Message));
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new MobileSyncPushResultDto(request.Items.Count, processed, failed, results);
    }

    public async Task<MobileSyncPullResultDto> PullAsync(CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireMobileUserAsync(_db, _currentUser, cancellationToken);
        var containers = await _driverOps.ListAssignedContainersAsync(cancellationToken);
        return new MobileSyncPullResultDto(containers, DateTime.UtcNow);
    }

    private async Task ProcessQueueItemAsync(OfflineSyncQueue item, CancellationToken cancellationToken)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(item.PayloadJson);
        var root = doc.RootElement;

        switch (item.EntityType.ToLowerInvariant())
        {
            case "container_status":
                var containerUuid = root.GetProperty("containerUuid").GetGuid();
                var status = root.GetProperty("status").GetString() ?? throw new ClientPortalException("INVALID_PAYLOAD", "Status required.");
                await _driverOps.UpdateContainerStatusAsync(containerUuid, new UpdateContainerStatusRequest(status), cancellationToken);
                break;
            case "container_location":
                containerUuid = root.GetProperty("containerUuid").GetGuid();
                var lat = root.GetProperty("latitude").GetDecimal();
                var lng = root.GetProperty("longitude").GetDecimal();
                await _driverOps.RecordContainerLocationAsync(containerUuid, new RecordContainerLocationRequest(lat, lng), cancellationToken);
                break;
            default:
                throw new ClientPortalException("UNSUPPORTED", $"Unsupported sync entity type: {item.EntityType}");
        }
    }
}
