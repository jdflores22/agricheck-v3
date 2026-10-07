namespace AgriCheck.Application.Compliance;

public record AgencyLicenseApplicationListItemDto(
    Guid Uuid,
    string ApplicantName,
    string ApplicantEmail,
    string LicenseTypeCode,
    string LicenseTypeName,
    string Status,
    DateTime? SubmittedAt,
    DateTime CreatedAt);

public record AgencyLicenseApplicationDetailDto(
    Guid Uuid,
    string ApplicantName,
    string ApplicantEmail,
    string LicenseTypeCode,
    string LicenseTypeName,
    string Status,
    string? FormDataJson,
    DateTime? SubmittedAt,
    string? ReviewComments,
    DateTime CreatedAt);

public record ApproveAgencyLicenseApplicationRequest(
    string LicenseNumber,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    string? ReviewComments);

public record RejectAgencyLicenseApplicationRequest(string ReviewComments);
