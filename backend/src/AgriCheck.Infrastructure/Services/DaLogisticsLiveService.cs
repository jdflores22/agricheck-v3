using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using StoredFileDownload = AgriCheck.Application.ClientPortal.Dtos.StoredFileDownload;
using AgriCheck.Application.DaPortal;
using AgriCheck.Application.DaPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class DaLogisticsLiveService : IDaLogisticsLiveService
{
    private static readonly ContainerStatus[] InTransitMapStatuses =
    {
        ContainerStatus.PickedUp,
        ContainerStatus.InTransit,
    };

    /// <summary>No GPS ping within this window is treated as offline (AgriTrack foreground service target ~2–4 min).</summary>
    private const int DriverGpsOfflineThresholdMinutes = 3;

    private static readonly ContainerInspectionPhotoType[] DeliveryProofPhotoTypes =
    {
        ContainerInspectionPhotoType.DriverPickupGeotag,
        ContainerInspectionPhotoType.DriverWarehouseGeotag,
        ContainerInspectionPhotoType.DriverIncidentGeotag,
        ContainerInspectionPhotoType.DriverIncidentGeotag2,
    };

    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public DaLogisticsLiveService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<DaLiveLogisticsSnapshotDto> GetLiveSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var inTransit = await LoadDeliveriesAsync(
            InTransitMapStatuses,
            recentTrailLimit: 400,
            liveMapTrailOnly: true,
            cancellationToken);

        return new DaLiveLogisticsSnapshotDto(DateTime.UtcNow, inTransit);
    }

    public async Task<DaLogisticsAlertsSnapshotDto> GetAlertsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var alertCutoff = DateTime.UtcNow.AddHours(-72);
        var alertRows = await _db.DeliveryComplianceEvents
            .AsNoTracking()
            .Where(e => e.RecordedAt >= alertCutoff)
            .OrderByDescending(e => e.RecordedAt)
            .Take(80)
            .ToListAsync(cancellationToken);

        var alertDtos = await MapComplianceAlertsAsync(alertRows, cancellationToken);
        var openAlerts = alertDtos.Where(a => a.IsOpen).ToList();
        var alertHistory = alertDtos.Where(a => !a.IsOpen).Take(40).ToList();

        var arrivedCutoff = DateTime.UtcNow.AddHours(-48);
        var recentlyArrived = await _db.Containers
            .AsNoTracking()
            .Where(c =>
                c.AssignedDriverUserId != null &&
                ((c.Status == ContainerStatus.AtWarehouse && c.ArrivalTime >= arrivedCutoff) ||
                 (c.WarehouseHandoffEligibleAt >= arrivedCutoff)))
            .OrderByDescending(c => c.ArrivalTime ?? c.WarehouseHandoffEligibleAt)
            .Take(25)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var arrivedDtos = recentlyArrived.Count == 0
            ? Array.Empty<DaLiveDeliveryDto>()
            : await LoadDeliveriesByIdsAsync(recentlyArrived, recentTrailLimit: 0, liveMapTrailOnly: false, cancellationToken);

        var holdDtos = await LoadActiveComplianceHoldsAsync(cancellationToken);

        return new DaLogisticsAlertsSnapshotDto(
            DateTime.UtcNow,
            openAlerts,
            alertHistory,
            holdDtos,
            arrivedDtos);
    }

    public async Task<DaDeliveryComplianceAlertDto> AcknowledgeComplianceAlertAsync(
        long alertId,
        DaAcknowledgeComplianceAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var notes = request.DaReviewNotes?.Trim();
        if (string.IsNullOrWhiteSpace(notes))
        {
            throw new ClientPortalException("NOTES_REQUIRED", "Enter DA remarks before clearing the alert.");
        }

        var alert = await _db.DeliveryComplianceEvents
            .FirstOrDefaultAsync(e => e.Id == alertId, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Compliance alert not found.");

        if (alert.DaReviewedAtUtc is not null)
        {
            throw new ClientPortalException("ALREADY_REVIEWED", "This alert was already reviewed.");
        }

        alert.DaReviewedAtUtc = DateTime.UtcNow;
        alert.DaReviewedByUserId = user.Id;
        alert.DaReviewNotes = notes;
        await _db.SaveChangesAsync(cancellationToken);

        var mapped = await MapComplianceAlertsAsync(new[] { alert }, cancellationToken);
        return mapped[0];
    }

    public async Task<DaDeliveryComplianceHoldDto> ClearComplianceHoldAsync(
        Guid containerUuid,
        DaClearDeliveryComplianceHoldRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.AssignedDriver).ThenInclude(u => u!.Profile)
            .Include(c => c.AssignedOperatorVehicle)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (!DeliveryComplianceHoldHelper.HasActiveHold(container))
        {
            throw new ClientPortalException("NO_ACTIVE_HOLD", "This delivery has no active compliance hold.");
        }

        DeliveryComplianceHoldHelper.ClearHold(container, user.Id, request.ClearanceNotes);
        await _db.SaveChangesAsync(cancellationToken);

        return new DaDeliveryComplianceHoldDto(
            container.Uuid,
            container.ContainerNumber,
            container.Entry.ReferenceNo,
            container.DeliveryComplianceHoldReason ?? "cleared",
            container.DeliveryComplianceHoldAt ?? DateTime.UtcNow,
            FormatDriverName(container.AssignedDriver),
            container.AssignedOperatorVehicle?.PlateNumber);
    }

    public async Task<DaContainerActualRouteDto> GetActualRouteAsync(Guid containerUuid, CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var container = await _db.Containers
            .AsNoTracking()
            .Include(c => c.Entry).ThenInclude(e => e.FinalWarehouseFacility)
            .Include(c => c.AssignedDriver).ThenInclude(u => u!.Profile)
            .Include(c => c.AssignedOperatorVehicle)
            .Include(c => c.Locations)
            .Include(c => c.InspectionPhotos)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        var pickup = ContainerDeliveryPointHelper.ResolvePickup(container, container.Entry);
        var destination = await ResolveDestinationAsync(container, cancellationToken);
        var trail = BuildTrail(container.Locations, int.MaxValue);
        var distanceKm = ComputeTrailDistanceKm(trail);

        var driverName = FormatDriverName(container.AssignedDriver);

        var proofPhotos = BuildDeliveryProofPhotos(container);

        return new DaContainerActualRouteDto(
            container.Uuid,
            container.ContainerNumber,
            container.Entry.ReferenceNo,
            container.Status.ToString(),
            driverName,
            container.AssignedOperatorVehicle?.PlateNumber,
            new DaDeliveryStopDto(pickup.Label, pickup.Latitude, pickup.Longitude),
            destination,
            container.DepartureTime,
            container.ArrivalTime,
            container.WarehouseHandoffEligibleAt,
            distanceKm,
            trail.Count,
            trail,
            proofPhotos);
    }

    public async Task<StoredFileDownload> DownloadDeliveryProofPhotoAsync(
        Guid photoUuid,
        CancellationToken cancellationToken = default)
    {
        await DaContextHelper.RequireDaLeadershipAsync(_db, _currentUser, cancellationToken);

        var photo = await _db.ContainerInspectionPhotos
            .AsNoTracking()
            .Include(p => p.Container)
            .FirstOrDefaultAsync(p => p.Uuid == photoUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Photo not found.");

        if (!DeliveryProofPhotoTypes.Contains(photo.PhotoType))
        {
            throw new ClientPortalException("NOT_FOUND", "Photo not found.");
        }

        var folder = ResolveDeliveryProofStorageFolder(photo.Container.Uuid, photo.PhotoType);
        var physicalPath = _fileStorage.FindStoredFile(folder, photo.StoredFileName)
            ?? _fileStorage.GetPhysicalPath(folder, photo.StoredFileName);
        if (!File.Exists(physicalPath))
        {
            throw new ClientPortalException("NOT_FOUND", "Photo file not found.");
        }

        return new StoredFileDownload(
            physicalPath,
            photo.ContentType,
            photo.OriginalFileName);
    }

    private async Task<IReadOnlyList<DaDeliveryComplianceHoldDto>> LoadActiveComplianceHoldsAsync(
        CancellationToken cancellationToken)
    {
        var holdContainers = await _db.Containers
            .AsNoTracking()
            .Include(c => c.Entry)
            .Include(c => c.AssignedDriver).ThenInclude(u => u!.Profile)
            .Include(c => c.AssignedOperatorVehicle)
            .Where(c => c.DeliveryComplianceHoldReason != null && c.DeliveryComplianceClearedAt == null)
            .OrderByDescending(c => c.DeliveryComplianceHoldAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return holdContainers
            .Where(c => c.DeliveryComplianceHoldAt is not null && c.DeliveryComplianceHoldReason is not null)
            .Select(c =>
            {
                var driverName = FormatDriverName(c.AssignedDriver);
                return new DaDeliveryComplianceHoldDto(
                    c.Uuid,
                    c.ContainerNumber,
                    c.Entry.ReferenceNo,
                    c.DeliveryComplianceHoldReason!,
                    c.DeliveryComplianceHoldAt!.Value,
                    driverName,
                    c.AssignedOperatorVehicle?.PlateNumber);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<DaDeliveryComplianceAlertDto>> MapComplianceAlertsAsync(
        IReadOnlyList<DeliveryComplianceEvent> alerts,
        CancellationToken cancellationToken)
    {
        if (alerts.Count == 0)
        {
            return Array.Empty<DaDeliveryComplianceAlertDto>();
        }

        var containerIds = alerts.Select(a => a.ContainerId).Distinct().ToList();
        var containers = await _db.Containers
            .AsNoTracking()
            .Include(c => c.Entry)
            .Include(c => c.AssignedDriver).ThenInclude(u => u!.Profile)
            .Where(c => containerIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        var containerMap = containers.ToDictionary(c => c.Id);

        var driverUserIds = containers
            .Where(c => c.AssignedDriverUserId is not null)
            .Select(c => c.AssignedDriverUserId!.Value)
            .Distinct()
            .ToList();
        var phoneByUserId = await _db.DriverProfiles
            .AsNoTracking()
            .Where(p => driverUserIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.PhoneNumber })
            .ToDictionaryAsync(p => p.UserId, p => p.PhoneNumber, cancellationToken);

        return alerts
            .Where(a => containerMap.ContainsKey(a.ContainerId))
            .Select(a =>
            {
                var c = containerMap[a.ContainerId];
                var driverName = FormatDriverName(c.AssignedDriver);
                var phone = c.AssignedDriverUserId is long uid
                    ? phoneByUserId.GetValueOrDefault(uid)
                    : null;
                return new DaDeliveryComplianceAlertDto(
                    a.Id,
                    c.Uuid,
                    c.ContainerNumber,
                    c.Entry.ReferenceNo,
                    a.EventType,
                    a.Details,
                    a.RecordedAt,
                    driverName,
                    phone,
                    a.DaReviewedAtUtc is null,
                    a.DaReviewedAtUtc,
                    a.DaReviewNotes);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<DaLiveDeliveryDto>> LoadDeliveriesAsync(
        ContainerStatus[] statuses,
        int recentTrailLimit,
        bool liveMapTrailOnly,
        CancellationToken cancellationToken)
    {
        var ids = await _db.Containers
            .AsNoTracking()
            .Where(c => statuses.Contains(c.Status) && c.AssignedDriverUserId != null)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => c.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        return await LoadDeliveriesByIdsAsync(ids, recentTrailLimit, liveMapTrailOnly, cancellationToken);
    }

    private async Task<IReadOnlyList<DaLiveDeliveryDto>> LoadDeliveriesByIdsAsync(
        IReadOnlyList<long> ids,
        int recentTrailLimit,
        bool liveMapTrailOnly,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<DaLiveDeliveryDto>();
        }

        var containers = await _db.Containers
            .AsNoTracking()
            .Include(c => c.Entry).ThenInclude(e => e.FinalWarehouseFacility)
            .Include(c => c.AssignedDriver).ThenInclude(u => u!.Profile)
            .Include(c => c.AssignedOperatorVehicle)
            .Include(c => c.Locations)
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

        var order = ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        containers.Sort((a, b) => order[a.Id].CompareTo(order[b.Id]));

        var results = new List<DaLiveDeliveryDto>(containers.Count);
        foreach (var container in containers)
        {
            var pickup = ContainerDeliveryPointHelper.ResolvePickup(container, container.Entry);
            var destination = await ResolveDestinationAsync(container, cancellationToken);
            var trail = BuildTrail(container.Locations, recentTrailLimit);
            if (liveMapTrailOnly)
            {
                trail = FilterTrailForLiveNavigation(trail, container.DepartureTime);
            }

            var last = trail.LastOrDefault();
            var driverName = FormatDriverName(container.AssignedDriver);
            var (gpsOffline, offlineMinutes) = ResolveDriverGpsOffline(container.Status, last?.RecordedAt);

            results.Add(new DaLiveDeliveryDto(
                container.Uuid,
                container.ContainerNumber,
                container.Entry.ReferenceNo,
                container.Status.ToString(),
                driverName,
                container.AssignedOperatorVehicle?.PlateNumber,
                new DaDeliveryStopDto(pickup.Label, pickup.Latitude, pickup.Longitude),
                destination,
                last?.Latitude,
                last?.Longitude,
                last?.RecordedAt,
                trail,
                gpsOffline,
                offlineMinutes,
                container.DepartureTime));
        }

        return results;
    }

    private static (bool Offline, int? OfflineMinutes) ResolveDriverGpsOffline(
        ContainerStatus status,
        DateTime? lastLocationAtUtc)
    {
        if (status is ContainerStatus.Released
            or ContainerStatus.Pending
            or ContainerStatus.AtWarehouse
            or ContainerStatus.ReadyForTransport
            or ContainerStatus.AwaitingConfirmation)
        {
            return (false, null);
        }

        if (lastLocationAtUtc is null)
        {
            return (true, null);
        }

        var recorded = lastLocationAtUtc.Value.Kind switch
        {
            DateTimeKind.Utc => lastLocationAtUtc.Value,
            DateTimeKind.Local => lastLocationAtUtc.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(lastLocationAtUtc.Value, DateTimeKind.Utc)
        };

        var age = DateTime.UtcNow - recorded;
        if (age.TotalMinutes < DriverGpsOfflineThresholdMinutes)
        {
            return (false, null);
        }

        return (true, (int)Math.Floor(age.TotalMinutes));
    }

    private static string? FormatDriverName(User? driver)
    {
        if (driver is null)
        {
            return null;
        }

        var profile = driver.Profile;
        if (profile is not null)
        {
            var full = $"{profile.FirstName} {profile.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(full))
            {
                return full;
            }
        }

        return string.IsNullOrWhiteSpace(driver.Email) ? null : driver.Email;
    }

    private static IReadOnlyList<DaDeliveryProofPhotoDto> BuildDeliveryProofPhotos(Container container)
    {
        return container.InspectionPhotos
            .Where(p => DeliveryProofPhotoTypes.Contains(p.PhotoType))
            .OrderBy(p => p.PhotoType)
            .Select(p => new DaDeliveryProofPhotoDto(
                p.Uuid,
                p.PhotoType.ToString(),
                ResolveDeliveryProofLabel(p.PhotoType),
                p.CaptureLatitude,
                p.CaptureLongitude,
                p.CapturedAtUtc,
                p.OriginalFileName))
            .ToList();
    }

    private static string ResolveDeliveryProofLabel(ContainerInspectionPhotoType photoType) => photoType switch
    {
        ContainerInspectionPhotoType.DriverPickupGeotag => "Pickup arrival (geotag proof)",
        ContainerInspectionPhotoType.DriverWarehouseGeotag => "Warehouse arrival (geotag proof)",
        ContainerInspectionPhotoType.DriverIncidentGeotag => "Driver incident report (geotag proof 1)",
        ContainerInspectionPhotoType.DriverIncidentGeotag2 => "Driver incident report (geotag proof 2)",
        _ => photoType.ToString(),
    };

    private static string ResolveDeliveryProofStorageFolder(Guid containerUuid, ContainerInspectionPhotoType photoType) =>
        photoType is ContainerInspectionPhotoType.DriverIncidentGeotag
            or ContainerInspectionPhotoType.DriverIncidentGeotag2
            ? $"container-incident-geotag/{containerUuid}"
            : DeliveryProofPhotoTypes.Contains(photoType)
                ? $"container-delivery-geotag/{containerUuid}"
                : $"container-inspection/{containerUuid}";

    private static List<DaContainerTrailPointDto> FilterTrailForLiveNavigation(
        List<DaContainerTrailPointDto> trail,
        DateTime? departureTimeUtc)
    {
        if (trail.Count == 0)
        {
            return trail;
        }

        if (departureTimeUtc is null)
        {
            return trail.Count <= 1 ? trail : trail.Skip(trail.Count - 1).ToList();
        }

        var departure = departureTimeUtc.Value.Kind switch
        {
            DateTimeKind.Utc => departureTimeUtc.Value,
            DateTimeKind.Local => departureTimeUtc.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(departureTimeUtc.Value, DateTimeKind.Utc),
        };

        var filtered = trail.Where(p =>
        {
            var recorded = p.RecordedAt.Kind switch
            {
                DateTimeKind.Utc => p.RecordedAt,
                DateTimeKind.Local => p.RecordedAt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(p.RecordedAt, DateTimeKind.Utc),
            };
            return recorded >= departure.AddMinutes(-2);
        }).ToList();

        return filtered.Count > 0 ? filtered : trail.Skip(Math.Max(0, trail.Count - 1)).ToList();
    }

    private static List<DaContainerTrailPointDto> BuildTrail(IEnumerable<ContainerLocation> locations, int limit)
    {
        if (limit <= 0)
        {
            return new List<DaContainerTrailPointDto>();
        }

        var ordered = locations
            .Where(l => l.Latitude is not null && l.Longitude is not null)
            .OrderBy(l => l.RecordedAt)
            .ToList();

        if (limit != int.MaxValue && ordered.Count > limit)
        {
            ordered = ordered.Skip(ordered.Count - limit).ToList();
        }

        return ordered
            .Select(l => new DaContainerTrailPointDto(
                l.Latitude!.Value,
                l.Longitude!.Value,
                l.RecordedAt,
                ContainerLocationTrailHelper.ResolveCapturedOffline(l),
                l.CreatedAt,
                ContainerLocationTrailHelper.ResolveSyncDelaySeconds(l)))
            .ToList();
    }

    private static decimal? ComputeTrailDistanceKm(IReadOnlyList<DaContainerTrailPointDto> trail)
    {
        if (trail.Count < 2)
        {
            return null;
        }

        double meters = 0;
        for (var i = 1; i < trail.Count; i++)
        {
            var prev = trail[i - 1];
            var cur = trail[i];
            meters += GeoDistanceHelper.DistanceMeters(prev.Latitude, prev.Longitude, cur.Latitude, cur.Longitude);
        }

        return Math.Round((decimal)(meters / 1000.0), 2);
    }

    private async Task<DaDeliveryStopDto> ResolveDestinationAsync(Container container, CancellationToken cancellationToken)
    {
        var storedDestination = ContainerDeliveryPointHelper.TryGetStoredDestination(container);
        if (storedDestination is not null)
        {
            return new DaDeliveryStopDto(
                storedDestination.Value.Label,
                storedDestination.Value.Latitude,
                storedDestination.Value.Longitude);
        }

        var finalFacility = container.Entry.FinalWarehouseFacility;
        if (finalFacility?.Latitude is not null && finalFacility.Longitude is not null)
        {
            return new DaDeliveryStopDto(finalFacility.Name, finalFacility.Latitude.Value, finalFacility.Longitude.Value);
        }

        if (container.WarehouseFacilityId is not null)
        {
            var facility = await _db.WarehouseFacilities
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == container.WarehouseFacilityId, cancellationToken);
            if (facility?.Latitude is not null && facility.Longitude is not null)
            {
                return new DaDeliveryStopDto(facility.Name, facility.Latitude.Value, facility.Longitude.Value);
            }
        }

        return new DaDeliveryStopDto(
            "Warehouse (set final warehouse on entry)",
            DriverDeliveryTestReset.WarehouseLatitude,
            DriverDeliveryTestReset.WarehouseLongitude);
    }
}
