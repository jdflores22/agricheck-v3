using AgriCheck.Application.Common;

namespace AgriCheck.Application.OpsPortal.Dtos;

public record WarehouseDashboardDto(
    int StoredContainers,
    int PendingReleases,
    int ReleasedToday,
    int ActiveFacilities);

public record WarehouseInventoryListItemDto(
    Guid Uuid,
    Guid EntryUuid,
    string ContainerNumber,
    string EntryReference,
    string FacilityName,
    string? LocationCode,
    string Status,
    DateTime ReceivedAt,
    bool MavLibraryLinked,
    string? MavHsCode,
    string? MavCommodityName,
    string? MavHsLabel,
    string? MavCategoryName,
    decimal? VolumeKg);

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
    int ReleaseCount,
    bool MavLibraryLinked,
    string? MavHsCode,
    string? MavCommodityName,
    string? MavHsLabel);

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
    DateTime? ApprovedAt,
    string? ProfilePhotoUrl = null);

public record DriverDocumentFileDto(
    Stream Content,
    string ContentType,
    string DownloadName);

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

public record OperatorInviteCodeListItemDto(
    string Code,
    string? Label,
    int MaxUses,
    int UsedCount,
    DateTime? ExpiresAt,
    bool IsActive,
    DateTime CreatedAt);

public record CreateOperatorInviteCodeRequest(string? Label);

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

public record DriverDeliveryHandoffQrDto(
    Guid ContainerUuid,
    string ContainerNumber,
    string EntryReference,
    string QrPayload,
    string? QrCodeData,
    string HandoffInstruction,
    DateTime ExpiresAtUtc,
    int RefreshAfterSeconds);

public record ReportDeliveryComplianceRequest(
    string EventType,
    decimal Latitude,
    decimal Longitude,
    string? Details = null,
    double? OffRouteMeters = null);

public record SubmitFaceVerificationRequest(
    decimal? Confidence,
    string? Notes);

public record ContainerDeliveryStopDto(
    string Name,
    decimal Latitude,
    decimal Longitude);

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
    DateTime? LastLocationAt,
    Guid? AssignedDriverUuid = null,
    string? AssignedDriverName = null,
    string? AssignedVehiclePlate = null,
    bool MavLibraryLinked = false,
    string? MavHsCode = null,
    string? MavCommodityName = null,
    string? MavHsLabel = null,
    string? MavCategoryName = null,
    decimal? VolumeKg = null,
    ContainerDeliveryStopDto? Pickup = null,
    ContainerDeliveryStopDto? Destination = null,
    bool HandoffQrAvailable = false,
    bool DeliveryComplianceHoldActive = false,
    string? DeliveryComplianceHoldReason = null);

public record UpdateContainerStatusRequest(string Status, decimal? Latitude = null, decimal? Longitude = null);

public record AssignDriverRequest(Guid DriverUserUuid, Guid? VehicleUuid = null);

public record BookOperatorDeliveryStopsRequest(
    string PickupLabel,
    decimal PickupLatitude,
    decimal PickupLongitude,
    string DestinationLabel,
    decimal DestinationLatitude,
    decimal DestinationLongitude);

public record OperatorDeliveryStopsDto(
    Guid ContainerUuid,
    string ContainerNumber,
    ContainerDeliveryStopDto Pickup,
    ContainerDeliveryStopDto Destination);

public record OperatorDriverListItemDto(
    Guid UserUuid,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? VehicleType,
    string? VehicleRegistration,
    bool ProfileComplete);

public record OperatorVehicleListItemDto(
    Guid Uuid,
    string PlateNumber,
    string VehicleType,
    string? Description,
    Guid? DefaultDriverUuid,
    string? DefaultDriverName,
    bool IsActive);

public record CreateOperatorVehicleRequest(
    string PlateNumber,
    string VehicleType,
    string? Description,
    Guid? DefaultDriverUuid);

public record ScanTransportQrRequest([property: System.ComponentModel.DataAnnotations.MaxLength(8192)] string QrData);

public record CompleteDoctorInspectionRequest(string Decision, string? Findings);

public record RecordContainerLocationRequest(decimal Latitude, decimal Longitude, DateTime? RecordedAt = null);

public record RecordContainerLocationPointRequest(decimal Latitude, decimal Longitude, DateTime RecordedAt, bool CapturedOffline = false);

public record RecordContainerLocationsBatchRequest(IReadOnlyList<RecordContainerLocationPointRequest> Points);

public record ConfirmContainerPickupRequest(decimal Latitude, decimal Longitude, string? Label = null);

public record ResetDriverDeliveryTestRequest(decimal? Latitude = null, decimal? Longitude = null);

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
    ContainerTrackDestinationDto Pickup,
    ContainerTrackDestinationDto Destination,
    IReadOnlyList<ContainerLocationPointDto> Trail,
    bool HandoffQrAvailable = false,
    bool DeliveryComplianceHoldActive = false,
    string? DeliveryComplianceHoldReason = null);

public record RegisterMobilePushDeviceRequest(string ExpoPushToken, string Platform);
