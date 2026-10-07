using AgriCheck.Application.Compliance;
using AgriCheck.Application.DaPortal.Dtos;

namespace AgriCheck.Application.OpsPortal;

public record WarehouseComplianceFacilityListItemDto(
    long WarehouseFacilityId,
    string WarehouseCode,
    string WarehouseName,
    string? WarehouseLocation,
    bool HasActiveWarehouseLto,
    string? WarehouseLtoNumber,
    DateTime? WarehouseLtoExpiresAt,
    string LtoApplicationStatus,
    string RsasStatus,
    string? CswStatus,
    bool DestinationReady);

public record WarehouseComplianceDetailDto(
    long WarehouseFacilityId,
    string WarehouseCode,
    string WarehouseName,
    string? WarehouseLocation,
    bool HasActiveWarehouseLto,
    WarehouseOperatingLicenseDto? ActiveLicense,
    WarehouseLtoApplicationDto? PendingLtoApplication,
    WarehouseLtoApplicationDto? LatestLtoApplication,
    ClientFacilityRegistrationDto? RsasRegistration,
    string CswStatus,
    bool DestinationReady);

public record WarehouseLtoApplicationDto(
    Guid Uuid,
    long WarehouseFacilityId,
    string WarehouseCode,
    string WarehouseName,
    string OperatorLegalName,
    string? ContactEmail,
    string? ContactPhone,
    string? Notes,
    string Status,
    DateTime SubmittedAt,
    string? ReviewComments,
    DateTime? ReviewedAt);

public record SubmitWarehouseLtoApplicationRequest(
    long WarehouseFacilityId,
    string OperatorLegalName,
    string? ContactEmail,
    string? ContactPhone,
    string? Notes);

public record DaWarehouseLtoApplicationListItemDto(
    Guid Uuid,
    long WarehouseFacilityId,
    string WarehouseCode,
    string WarehouseName,
    string OperatorLegalName,
    string Status,
    DateTime SubmittedAt,
    string? SubmittedByEmail);

public record ResolveWarehouseLtoApplicationRequest(
    bool Approve,
    string? ReviewComments,
    IssueWarehouseOperatingLicenseRequest? IssueLicense);
