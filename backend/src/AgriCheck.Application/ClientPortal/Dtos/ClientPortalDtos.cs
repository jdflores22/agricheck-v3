namespace AgriCheck.Application.ClientPortal.Dtos;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public record CommodityDto(long Id, string Code, string Name, string CategoryName);
public record AgencyOptionDto(long Id, string Code, string Name);

public record EntryListItemDto(
    Guid Uuid, string ReferenceNo, string EntryType, string Status, string AgencyCode,
    string? CommodityName, DateTime CreatedAt, DateTime UpdatedAt, DateTime? SubmittedAt, string PaymentStatus,
    decimal? PaymentAmount, DateTime? ComplianceDeadlineAt);

public record EntryDetailDto(
    long? CommodityId, string? CommodityName, string? Description, decimal Quantity, string Unit,
    string? OriginCountry, string? DestinationCountry, string? PortOfEntry);

public record EntryContainerDto(
    Guid Uuid,
    int SequenceNumber,
    string ContainerNumber,
    string? ContainerType,
    string? FormDataJson);

public record EntryDto(
    Guid Uuid, string ReferenceNo, string EntryType, string Status, long AgencyId, string AgencyCode, string AgencyName,
    string? Notes, string? FormDataJson, DateTime? SubmittedAt, string PaymentStatus, decimal? PaymentAmount, DateTime? ComplianceDeadlineAt,
    EntryDetailDto? Detail, IReadOnlyList<EntryFileDto> Files, IReadOnlyList<EntryContainerDto> Containers,
    IReadOnlyList<StatusHistoryDto> StatusHistory,
    IReadOnlyList<TimelineEventDto> Timeline, IReadOnlyList<ClientBillSummaryDto> Bills,
    EntryMavInfoDto? Mav);

public record EntryMicUtilizationDto(
    Guid MicUuid,
    string CertificateNumber,
    decimal Volume,
    DateTime UtilizedAt,
    string HsCode,
    string CommodityName);

public record EntryMavInfoDto(
    string? MavNo,
    string ImportTrack,
    string MavDocumentStatus,
    string? MavRemarks,
    Guid? MavCertificateFileUuid,
    string? MavCertificateFileName,
    IReadOnlyList<EntryMicUtilizationDto> MicUtilizations,
    decimal TotalUtilizedVolume,
    decimal? RequiredVolume);

public record CheckMavNoResultDto(bool IsAvailable, string? Message);

public record UpdateEntryMavRequest(string MavNo);

public record UtilizeEntryMicRequest(Guid MicUuid, decimal Volume);

public record EntryFileVersionDto(int VersionNumber, string OriginalFileName, long FileSizeBytes, DateTime CreatedAt, bool IsCurrent);

public record EntryFileDto(
    Guid Uuid,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string? DocumentType,
    DateTime CreatedAt,
    string? EvaluationDecision,
    string? EvaluationComment,
    IReadOnlyList<EntryFileVersionDto> Versions);
public record StatusHistoryDto(string FromStatus, string ToStatus, string? Comment, DateTime CreatedAt);
public record TimelineEventDto(string EventType, string Title, string? Description, DateTime CreatedAt);

public record CreateEntryRequest(long AgencyId, string EntryType, EntryDetailInputDto Detail, string? Notes, string? FormDataJson, int? NumContainers, string? ContainersJson, string? MavNo = null, string? ImportTrack = null);
public record UpdateEntryRequest(EntryDetailInputDto Detail, string? Notes, string? FormDataJson, int? NumContainers, string? ContainersJson, string? MavNo = null, string? ImportTrack = null);
public record EntryDetailInputDto(long? CommodityId, string? CommodityName, string? Description, decimal Quantity, string Unit, string? OriginCountry, string? DestinationCountry, string? PortOfEntry);

public record AccreditationListItemDto(Guid Uuid, string CompanyName, string SubmissionType, string Status, string DisplayStatus, DateTime? SubmittedAt, DateTime CreatedAt);
public record AccreditationSubmissionDto(
    Guid Uuid,
    string CompanyName,
    string SubmissionType,
    string Status,
    string DisplayStatus,
    string? FormDataJson,
    string? ReviewComments,
    string? AccreditationNumber,
    DateTime? SubmittedAt,
    Guid? CertificateUuid,
    string? CertificateNumber,
    IReadOnlyList<SubmissionFileDto> Files,
    IReadOnlyList<AccreditationHistoryDto> History);
public record SubmissionFileDto(
    Guid Uuid,
    string OriginalFileName,
    long FileSizeBytes,
    DateTime CreatedAt,
    string? ReviewDecision,
    string? ReviewComment,
    IReadOnlyList<SubmissionFileVersionDto> Versions);

public record SubmissionFileVersionDto(int VersionNumber, string OriginalFileName, long FileSizeBytes, DateTime CreatedAt, bool IsCurrent);
public record AccreditationHistoryDto(string Status, string? Comment, DateTime CreatedAt, string? ActorName = null);
public record CreateAccreditationRequest(string CompanyName, string SubmissionType, string? FormDataJson);
public record UpdateAccreditationRequest(string CompanyName, string? FormDataJson);

public record CertificateListItemDto(Guid Uuid, string CertificateNumber, string Title, string Status, DateTime IssuedAt, DateTime? ExpiresAt, string? EntryReferenceNo);
public record CertificateDto(Guid Uuid, string CertificateNumber, string VerificationCode, string Title, string Status, DateTime IssuedAt, DateTime? ExpiresAt, string? EntryReferenceNo, string? QrCodeData, string? SummaryJson);
public record CertificateVerifyDto(
    string CertificateNumber,
    string Title,
    string Status,
    string EffectiveStatus,
    DateTime IssuedAt,
    DateTime? ExpiresAt,
    bool IsValid,
    string? HolderName,
    string? ProcessType,
    string? CompanyName,
    string? CompanyType,
    string? AccreditationNumber,
    string? QrCodeData,
    DateTime? RevokedAt,
    string? RevocationReason);

public record WarehouseFacilityDto(
    long Id,
    string Code,
    string Name,
    string? Location,
    int Capacity,
    string FormattedAddress,
    decimal? Latitude,
    decimal? Longitude,
    string? RegionName,
    string? ProvinceName,
    string? CityName,
    string? BarangayName,
    string? StreetAddress,
    string? ZipCode);
public record WarehouseBookingListItemDto(Guid Uuid, string BookingNumber, string WarehouseName, string ContainerReference, string Status, DateTime ScheduledDate, decimal? Amount);
public record WarehouseBookingDto(Guid Uuid, string BookingNumber, string WarehouseName, string ContainerReference, string Status, DateTime ScheduledDate, string? Notes, decimal? Amount);
public record CreateWarehouseBookingRequest(
    long WarehouseFacilityId,
    string ContainerReference,
    DateTime ScheduledDate,
    string? Notes,
    Guid? ContainerUuid = null);

public record ClientBillSummaryDto(Guid Uuid, string BillNumber, string Description, decimal Amount, string Status, DateTime? DueDate, DateTime? PaidAt);
public record ClientBillDto(Guid Uuid, string BillNumber, string Description, decimal Amount, string Status, DateTime? DueDate, string? PaymentLinkToken, Guid? EntryUuid, string? EntryReferenceNo, IReadOnlyList<ClientBillPaymentDto> Payments);
public record ClientBillPaymentDto(decimal Amount, string PaymentMethod, string Status, DateTime CreatedAt);
public record PayBillRequest(string PaymentMethod, string? ReturnBaseUrl = null, string? PaymentReference = null);

public record BillPaymentOptionsDto(
    bool PayMongoEnabled,
    bool CashPaymentEnabled,
    string GatewayMode,
    string? CashPaymentInstructions);

public record InitiateBillPaymentResultDto(
    string Mode,
    string PaymentReference,
    string? PaymentUrl,
    ClientBillDto Bill);

public record ClientDashboardProfileDto(string FirstName, string LastName, string Email);

public record ClientDashboardAccreditationDto(
    string? Status,
    string? DisplayStatus,
    string? SubmissionType,
    string? AccreditationNumber,
    string? CompanyName,
    string? ReviewComments,
    bool IsAccredited);

public record ClientDashboardEntryStatsDto(int Total, int Pending, int Approved, int ForCompliance);

public record ClientDashboardWorkflowStatsDto(
    int DaIssueBilling,
    int ForInspection,
    int ReadyForTransport,
    int AwaitingTransport,
    int InTransit,
    int DaBillingPaymentPending);

public record ClientDashboardLogisticsStatsDto(
    int PendingPayments,
    int UnpaidBills,
    int OverdueBills,
    decimal TotalAmountDue,
    int ApprovedContainers,
    int AssignedContainers,
    int PendingInspections);

public record ClientDashboardAgencyDto(long Id, string Code, string Name, string? LogoUrl, bool HasEntryForm);

public record ClientDashboardRecentBillDto(
    Guid Uuid,
    Guid? EntryUuid,
    string BillNumber,
    string? EntryReferenceNo,
    string? AgencyName,
    string Description,
    decimal Amount,
    string Status,
    DateTime? DueDate,
    bool IsOverdue,
    string? PaymentLinkToken);

public record ClientDashboardRecentEntryDto(
    Guid Uuid,
    string ReferenceNo,
    string Status,
    string AgencyCode,
    DateTime CreatedAt);

public record ClientDashboardRecentCertificateDto(
    Guid Uuid,
    string CertificateNumber,
    string Title,
    string Status,
    DateTime IssuedAt);

public record ClientDashboardDto(
    ClientDashboardProfileDto Profile,
    ClientDashboardAccreditationDto Accreditation,
    ClientDashboardEntryStatsDto Entries,
    ClientDashboardWorkflowStatsDto Workflow,
    ClientDashboardLogisticsStatsDto Logistics,
    IReadOnlyList<ClientDashboardAgencyDto> Agencies,
    IReadOnlyList<ClientDashboardRecentBillDto> RecentBills,
    IReadOnlyList<ClientDashboardRecentEntryDto> RecentEntries,
    IReadOnlyList<ClientDashboardRecentCertificateDto> RecentCertificates);

public record ClientFormListItemDto(Guid Uuid, string Name, string FormType, int VersionNumber);

public record ClientFormDetailDto(Guid Uuid, string Name, string FormType, int VersionNumber, string SchemaJson);

public record ClientProfileDto(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? CompanyName,
    string? Address);

public record UpdateClientProfileRequest(
    string FirstName,
    string LastName,
    string? Phone,
    string? CompanyName,
    string? Address);

public record ClientContainerListItemDto(
    Guid Uuid,
    string ContainerNumber,
    string? ContainerType,
    string Status,
    string EntryReferenceNo,
    string AgencyCode,
    DateTime UpdatedAt);

public record ClientContainerProcessStepDto(
    string Key,
    string Label,
    string Description,
    string State,
    DateTime? CompletedAt);

public record ClientContainerBookingSummaryDto(
    Guid Uuid,
    string BookingNumber,
    string WarehouseName,
    string Status,
    DateTime ScheduledDate);

public record ClientContainerWarehouseInfoDto(
    string? FacilityName,
    string? LocationCode,
    DateTime? ReceivedAt,
    string? Status);

public record ClientContainerTransportTagDto(
    Guid TagUuid,
    DateOnly? ScheduledWarehouseDate,
    DateTime TaggedAt,
    string? QrCodeData);

public record ClientContainerDetailDto(
    Guid Uuid,
    int SequenceNumber,
    string ContainerNumber,
    string? ContainerType,
    string? FormDataJson,
    string Status,
    Guid EntryUuid,
    string EntryReferenceNo,
    string EntryStatus,
    string AgencyCode,
    string AgencyName,
    DateTime? DepartureTime,
    DateTime? ArrivalTime,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool CanBookWarehouse,
    string? WarehouseBookingBlockedReason,
    IReadOnlyList<ClientContainerProcessStepDto> ProcessSteps,
    ClientContainerWarehouseInfoDto? WarehouseInfo,
    IReadOnlyList<ClientContainerBookingSummaryDto> Bookings,
    ClientContainerTransportTagDto? TransportTag);

public record ClientInspectionListItemDto(
    Guid Uuid,
    Guid EntryUuid,
    string EntryReferenceNo,
    string AgencyCode,
    string Status,
    DateTime? ScheduledAt,
    DateTime? CompletedAt);

public record ClientInspectionDetailDto(
    Guid Uuid,
    Guid EntryUuid,
    string EntryReferenceNo,
    string AgencyCode,
    string AgencyName,
    string Status,
    DateTime? ScheduledAt,
    DateTime? CompletedAt,
    string? Findings,
    string? InspectorName);

public record StoredFileDownload(string PhysicalPath, string ContentType, string DownloadFileName);

public record ClientPaymentHistoryItemDto(
    Guid BillUuid,
    string BillNumber,
    string? EntryReferenceNo,
    decimal Amount,
    string PaymentMethod,
    string Status,
    string? ExternalReference,
    DateTime PaidAt);

public record ClientDaBillingChargeDto(
    string Description,
    decimal Amount,
    int SortOrder);

public record ClientDaBillingDto(
    Guid Uuid,
    Guid EntryUuid,
    string EntryReferenceNo,
    string BillNumber,
    string Description,
    decimal Amount,
    string Status,
    string DisplayStatus,
    DateTime? IssuedAt,
    DateTime? PaidAt,
    string? PaymentReference,
    string? PaymentProofOriginalFileName,
    DateTime? PaymentUploadedAt,
    string? VerificationNotes,
    IReadOnlyList<ClientDaBillingChargeDto> Charges);

public record UploadDaBillingPaymentRequest(string PaymentReference, string? Notes);

public record ClientContainerInspectionPhotoDto(
    Guid Uuid,
    Guid ContainerUuid,
    string PhotoType,
    string OriginalFileName,
    string ReviewDecision,
    string? ReviewComment,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ReviewedByName);

public record ClientContainerInspectionStatusDto(
    Guid ContainerUuid,
    string ContainerNumber,
    IReadOnlyList<ClientContainerInspectionPhotoDto> Photos,
    bool IsComplete,
    bool IsApproved);
