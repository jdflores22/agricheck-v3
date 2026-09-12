using AgriCheck.Application.Common;

namespace AgriCheck.Application.OpsPortal.Dtos;

public record WarehouseDashboardDto(
    int StoredContainers,
    int PendingReleases,
    int ReleasedToday,
    int ActiveFacilities);

public record WarehouseInventoryListItemDto(
    Guid Uuid,
    string ContainerNumber,
    string EntryReference,
    string FacilityName,
    string? LocationCode,
    string Status,
    DateTime ReceivedAt);

public record ReceiveContainerRequest(
    Guid ContainerUuid,
    long WarehouseFacilityId,
    string? LocationCode);

public record ReleaseAuthorizationListItemDto(
    Guid Uuid,
    string EntryReference,
    string RecipientName,
    string RecipientIdNumber,
    DateTime AuthorizedAt,
    int ReleaseCount);

public record CreateReleaseAuthorizationRequest(
    Guid EntryUuid,
    string RecipientName,
    string RecipientIdNumber);

public record ExecuteReleaseRequest(
    Guid InventoryUuid,
    Guid AuthorizationUuid,
    string? RecipientSignaturePath);

public record ReleaseRecordListItemDto(
    Guid Uuid,
    string ContainerNumber,
    string RecipientName,
    DateTime ReleasedAt);

public record DriverDashboardDto(
    int AssignedContainers,
    int InTransitContainers,
    int ProfileCompletion,
    bool FaceVerified);

public record DriverProfileDto(
    string? LicenseNumber,
    DateTime? LicenseExpiryDate,
    string? VehicleType,
    string? VehicleRegistration,
    string? PhoneNumber,
    string? EmergencyContact,
    string? EmergencyPhone,
    string? Address,
    int CompletionPercentage,
    bool FaceVerified,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt);

public record UpdateDriverProfileRequest(
    string? LicenseNumber,
    DateTime? LicenseExpiryDate,
    string? VehicleType,
    string? VehicleRegistration,
    string? PhoneNumber,
    string? EmergencyContact,
    string? EmergencyPhone,
    string? Address);

public record ContainerListItemDto(
    Guid Uuid,
    string ContainerNumber,
    Guid EntryUuid,
    string EntryReference,
    string Status,
    DateTime? DepartureTime,
    DateTime? ArrivalTime,
    decimal? LastLatitude,
    decimal? LastLongitude,
    DateTime? LastLocationAt);

public record UpdateContainerStatusRequest(string Status);

public record AssignDriverRequest(Guid DriverUserUuid);

public record CompleteDoctorInspectionRequest(string Decision, string? Findings);

public record RecordContainerLocationRequest(decimal Latitude, decimal Longitude);

public record MobileSyncPushRequest(IReadOnlyList<MobileSyncItemRequest> Items);

public record MobileSyncItemRequest(
    string ClientId,
    string EntityType,
    string PayloadJson);

public record MobileSyncPushResultDto(
    int Accepted,
    int Processed,
    int Failed,
    IReadOnlyList<MobileSyncItemResultDto> Results);

public record MobileSyncItemResultDto(string ClientId, string Status, string? ErrorMessage);

public record MobileSyncPullResultDto(
    IReadOnlyList<ContainerListItemDto> Containers,
    DateTime ServerTime);

public record ContainerLocationPointDto(decimal Latitude, decimal Longitude, DateTime RecordedAt);

public record ContainerTrackDestinationDto(
    string Name,
    decimal Latitude,
    decimal Longitude);

public record ContainerTrackDto(
    Guid ContainerUuid,
    string ContainerNumber,
    string EntryReference,
    string Status,
    decimal? CurrentLatitude,
    decimal? CurrentLongitude,
    ContainerTrackDestinationDto Destination,
    IReadOnlyList<ContainerLocationPointDto> Trail);

public record RegisterMobilePushDeviceRequest(string ExpoPushToken, string Platform);
