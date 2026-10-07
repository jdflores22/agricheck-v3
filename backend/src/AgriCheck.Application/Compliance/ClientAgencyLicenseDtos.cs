namespace AgriCheck.Application.Compliance;

public record AgencyLicenseTypeOptionDto(
    long Id,
    long AgencyId,
    string AgencyCode,
    string AgencyName,
    string Code,
    string Name,
    string Kind,
    bool IsRequiredForSpsic,
    string? Description);

public record ClientAgencyLicenseListItemDto(
    Guid Uuid,
    long AgencyId,
    string AgencyCode,
    string AgencyName,
    string LicenseTypeCode,
    string LicenseTypeName,
    string LicenseNumber,
    string Status,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    bool IsActive);

public record ClientAgencyLicenseApplicationListItemDto(
    Guid Uuid,
    long AgencyId,
    string AgencyCode,
    string AgencyName,
    string LicenseTypeCode,
    string LicenseTypeName,
    string Status,
    DateTime? SubmittedAt,
    DateTime CreatedAt);

public record ClientAgencyLicenseOverviewDto(
    IReadOnlyList<ClientAgencyLicenseListItemDto> Licenses,
    IReadOnlyList<ClientAgencyLicenseApplicationListItemDto> Applications);

public record ClientAgencyLicenseApplicationDto(
    Guid Uuid,
    long AgencyId,
    string AgencyCode,
    string AgencyName,
    long AgencyLicenseTypeId,
    string LicenseTypeCode,
    string LicenseTypeName,
    string Status,
    string? FormDataJson,
    DateTime? SubmittedAt,
    string? ReviewComments,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateAgencyLicenseApplicationRequest(long AgencyId, long AgencyLicenseTypeId, string? FormDataJson);

public record UpdateAgencyLicenseApplicationRequest(string? FormDataJson);

public record RegisterClientAgencyLicenseRequest(
    long AgencyId,
    long AgencyLicenseTypeId,
    string LicenseNumber,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    string? Notes);
