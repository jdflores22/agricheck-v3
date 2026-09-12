using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class WarehouseOpsService : IWarehouseOpsService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public WarehouseOpsService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
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

    public async Task<PagedResult<WarehouseInventoryListItemDto>> ListInventoryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.WarehouseInventories
            .Include(i => i.Container).ThenInclude(c => c.Entry)
            .Include(i => i.WarehouseFacility)
            .OrderByDescending(i => i.ReceivedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new WarehouseInventoryListItemDto(
                i.Uuid,
                i.Container.ContainerNumber,
                i.Container.Entry.ReferenceNo,
                i.WarehouseFacility.Name,
                i.LocationCode,
                i.Status.ToString(),
                i.ReceivedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseInventoryListItemDto>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<ContainerListItemDto>> ListReceivableContainersAsync(CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var storedIds = _db.WarehouseInventories
            .Where(i => i.Status == WarehouseInventoryStatus.Stored)
            .Select(i => i.ContainerId);

        var containers = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .Include(c => c.DoctorInspections)
            .Where(c => !storedIds.Contains(c.Id) &&
                        c.Status == ContainerStatus.Inspected &&
                        c.DoctorInspections.Any(i => i.Status == DoctorInspectionStatus.Approved))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

        return containers.Select(OpsDtoMapper.MapContainer).ToList();
    }

    public async Task<WarehouseInventoryListItemDto> ReceiveContainerAsync(ReceiveContainerRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers.Include(c => c.Entry)
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
        container.ArrivalTime ??= DateTime.UtcNow;
        _db.WarehouseInventories.Add(inventory);
        await _db.SaveChangesAsync(cancellationToken);

        return new WarehouseInventoryListItemDto(
            inventory.Uuid,
            container.ContainerNumber,
            container.Entry.ReferenceNo,
            facility.Name,
            inventory.LocationCode,
            inventory.Status.ToString(),
            inventory.ReceivedAt);
    }

    public async Task<IReadOnlyList<ReleaseAuthorizationListItemDto>> ListReleaseAuthorizationsAsync(CancellationToken cancellationToken = default)
    {
        await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        return await _db.ReleaseAuthorizations
            .Include(a => a.Entry)
            .Include(a => a.ReleaseRecords)
            .OrderByDescending(a => a.AuthorizedAt)
            .Select(a => new ReleaseAuthorizationListItemDto(
                a.Uuid,
                a.Entry.ReferenceNo,
                a.RecipientName,
                a.RecipientIdNumber,
                a.AuthorizedAt,
                a.ReleaseRecords.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReleaseAuthorizationListItemDto> CreateReleaseAuthorizationAsync(CreateReleaseAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Uuid == request.EntryUuid && e.Status == EntryStatus.Approved, cancellationToken)
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

        return new ReleaseAuthorizationListItemDto(
            auth.Uuid,
            entry.ReferenceNo,
            auth.RecipientName,
            auth.RecipientIdNumber,
            auth.AuthorizedAt,
            0);
    }

    public async Task<ReleaseRecordListItemDto> ExecuteReleaseAsync(ExecuteReleaseRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await OpsContextHelper.RequireWarehouseStaffAsync(_db, _currentUser, cancellationToken);
        var inventory = await _db.WarehouseInventories
            .Include(i => i.Container).ThenInclude(c => c.Entry)
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
        await _db.SaveChangesAsync(cancellationToken);

        return new ReleaseRecordListItemDto(
            record.Uuid,
            inventory.Container.ContainerNumber,
            authorization.RecipientName,
            record.ReleasedAt);
    }
}

public class DriverOpsService : IDriverOpsService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DriverOpsService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
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
        var profile = await _db.DriverProfiles.FirstOrDefaultAsync(p => p.UserId == driver.Id, cancellationToken);
        return profile is null ? null : MapProfile(profile);
    }

    public async Task<DriverProfileDto> UpdateProfileAsync(UpdateDriverProfileRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var profile = await _db.DriverProfiles.FirstOrDefaultAsync(p => p.UserId == driver.Id, cancellationToken);
        if (profile is null)
        {
            profile = new DriverProfile { UserId = driver.Id };
            _db.DriverProfiles.Add(profile);
        }

        profile.LicenseNumber = request.LicenseNumber?.Trim();
        profile.LicenseExpiryDate = request.LicenseExpiryDate;
        profile.VehicleType = request.VehicleType?.Trim();
        profile.VehicleRegistration = request.VehicleRegistration?.Trim();
        profile.PhoneNumber = request.PhoneNumber?.Trim();
        profile.EmergencyContact = request.EmergencyContact?.Trim();
        profile.EmergencyPhone = request.EmergencyPhone?.Trim();
        profile.Address = request.Address?.Trim();
        profile.CompletionPercentage = CalculateCompletion(profile);
        profile.SubmittedAt ??= DateTime.UtcNow;

        _db.DriverProfileHistories.Add(new DriverProfileHistory
        {
            DriverProfile = profile,
            ChangedByUserId = driver.Id,
            ChangeDescription = "Profile updated"
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapProfile(profile);
    }

    public async Task<IReadOnlyList<ContainerListItemDto>> ListAssignedContainersAsync(CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var containers = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .Where(c => c.AssignedDriverUserId == driver.Id)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
        return containers.Select(OpsDtoMapper.MapContainer).ToList();
    }

    public async Task<ContainerListItemDto> UpdateContainerStatusAsync(Guid containerUuid, UpdateContainerStatusRequest request, CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<ContainerStatus>(request.Status, true, out var status))
        {
            throw new ClientPortalException("INVALID_STATUS", "Invalid container status.");
        }

        var container = await _db.Containers.Include(c => c.Entry).Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

        container.Status = status;
        if (status == ContainerStatus.InTransit && container.DepartureTime is null)
        {
            container.DepartureTime = DateTime.UtcNow;
        }

        if (status == ContainerStatus.AtWarehouse && container.ArrivalTime is null)
        {
            container.ArrivalTime = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return OpsDtoMapper.MapContainer(container);
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
            RecordedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContainerTrackDto> GetContainerTrackAsync(Guid containerUuid, CancellationToken cancellationToken = default)
    {
        var driver = await OpsContextHelper.RequireDriverAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.AssignedDriverUserId == driver.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Assigned container not found.");

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
            destination,
            trail);
    }

    private async Task<ContainerTrackDestinationDto> ResolveTrackDestinationAsync(Container container, CancellationToken cancellationToken)
    {
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

    private static int CalculateCompletion(DriverProfile profile)
    {
        var fields = new object?[]
        {
            profile.LicenseNumber,
            profile.LicenseExpiryDate,
            profile.VehicleType,
            profile.VehicleRegistration,
            profile.PhoneNumber,
            profile.EmergencyContact,
            profile.EmergencyPhone,
            profile.Address
        };
        var filled = fields.Count(f => f is not null && (f is not string s || !string.IsNullOrWhiteSpace(s)));
        return (int)Math.Round(filled * 100.0 / fields.Length);
    }

    private static DriverProfileDto MapProfile(DriverProfile profile) => new(
        profile.LicenseNumber,
        profile.LicenseExpiryDate,
        profile.VehicleType,
        profile.VehicleRegistration,
        profile.PhoneNumber,
        profile.EmergencyContact,
        profile.EmergencyPhone,
        profile.Address,
        profile.CompletionPercentage,
        profile.FaceVerified,
        profile.SubmittedAt,
        profile.ApprovedAt);

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
