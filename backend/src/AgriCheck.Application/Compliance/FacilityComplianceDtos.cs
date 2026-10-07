namespace AgriCheck.Application.Compliance;

public record FacilityDestinationStatusDto(
    bool HasRsasRegistration,
    bool HasActiveCswAccreditation,
    string RsasStatus,
    string? RsasExternalRef,
    string CswStatus,
    string? CswCertificateNumber);

public record ClientFacilityRegistrationListItemDto(
    long WarehouseFacilityId,
    string WarehouseCode,
    string WarehouseName,
    string? WarehouseLocation,
    Guid? RegistrationUuid,
    string RegistrationStatus,
    string? RsasExternalRef,
    string? CswStatus,
    DateTime? RegisteredAt,
    DateTime? ExpiresAt,
    bool IsBmbeExempt,
    bool RenewalWindowOpen,
    bool RenewalDue,
    string? CswLeadAgencyCode = null,
    IReadOnlyList<string>? FacilityAllowedCommodityClasses = null,
    string? FacilityScopeHint = null);

public record ClientFacilityRegistrationDto(
    Guid Uuid,
    long WarehouseFacilityId,
    string WarehouseCode,
    string WarehouseName,
    string Status,
    string OwnerName,
    decimal? MaxCapacityKg,
    string? RsasExternalRef,
    string? ProductsJson,
    DateTime? RegisteredAt,
    DateTime? ExpiresAt,
    string? ReviewComments,
    bool IsBmbeExempt,
    bool RenewalWindowOpen,
    bool RenewalDue,
    FacilityAccreditationSummaryDto? CswAccreditation);

public record FacilityAccreditationSummaryDto(
    Guid Uuid,
    string Status,
    string? CertificateNumber,
    DateTime? AccreditedAt,
    DateTime? ExpiresAt,
    string? LeadAgencyCode = null);

public record FacilityRegistrationDaReviewDto(
    Guid RegistrationUuid,
    long WarehouseFacilityId,
    string WarehouseCode,
    string WarehouseName,
    string Status,
    string OwnerName,
    string? RsasExternalRef,
    DateTime? SubmittedAt,
    string? ReviewComments);

public record UpsertFacilityRegistrationRequest(
    long WarehouseFacilityId,
    string OwnerName,
    decimal? MaxCapacityKg,
    string? RsasExternalRef,
    string? ProductsJson,
    bool IsBmbeExempt = false);

public record RegisterFacilityCswRequest(
    long WarehouseFacilityId,
    string CertificateNumber,
    DateTime AccreditedAt,
    DateTime ExpiresAt,
    long? LeadAgencyId);
