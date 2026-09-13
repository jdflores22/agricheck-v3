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
    string? FullName,
    DateTime? BirthDate,
    string? LicenseNumber,
    DateTime? LicenseExpiryDate,
    string? VehicleType,
    string? VehicleRegistration,
    string? PhoneNumber,
    string? EmergencyContact,
    string? EmergencyPhone,
    string? Address,
    long? RegionId,
    long? ProvinceId,
    long? CityId,
    long? BarangayId,
    string? ZipCode,
    string? StreetAddress,
    string? OperatorName,
    IReadOnlyList<DriverDocumentDto> Documents,
    int CompletionPercentage,
    bool FaceVerified,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt);

public record DriverDocumentDto(
    string DocumentType,
    string OriginalFileName,
    DateTime UploadedAt);

public record UpdateDriverProfileRequest(
    string? FullName,
    DateTime? BirthDate,
    string? LicenseNumber,
    DateTime? LicenseExpiryDate,
    string? VehicleType,
    string? VehicleRegistration,
    string? PhoneNumber,
    string? EmergencyContact,
    string? EmergencyPhone,
    string? Address,
    long? RegionId,
    long? ProvinceId,
    long? CityId,
    long? BarangayId,
    string? ZipCode,
    string? StreetAddress);

public record RegisterDriverRequest(
    string Email,
    string Password,
    string FullName,
    DateTime BirthDate,
    string InviteCode,
    long RegionId,
    long ProvinceId,
    long CityId,
    long BarangayId,
    string ZipCode,
    string StreetAddress,
    string LicenseNumber,
    DateTime LicenseExpiryDate,
    string PhoneNumber);

public record ValidateInviteCodeRequest(string InviteCode);

public record InviteCodeValidationDto(
    bool IsValid,
    string? OperatorName,
    string? Label);

public record DriverTransportQrPreviewDto(
    Guid ContainerUuid,
    string ContainerNumber,
    string EntryReference,
    string WarehouseName,
    string? WarehouseAddress,
    decimal? WarehouseLatitude,
    decimal? WarehouseLongitude,
    DateOnly? ScheduledWarehouseDate,
    string ContainerStatus,
    bool CanAccept,
    string? BlockReason);

public record DriverWarehouseCheckInRequest(decimal Latitude, decimal Longitude);

public record DriverWarehouseCheckInResultDto(
    bool WithinGeofence,
    double DistanceMeters,
    ContainerListItemDto Container);

public record SubmitFaceVerificationRequest(
    decimal? Confidence,
    string? Notes);

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

public record ScanTransportQrRequest([property: System.ComponentModel.DataAnnotations.MaxLength(8192)] string QrData);

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
