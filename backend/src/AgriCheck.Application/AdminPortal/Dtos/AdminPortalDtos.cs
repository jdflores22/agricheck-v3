using AgriCheck.Application.ClientPortal.Dtos;

namespace AgriCheck.Application.AdminPortal.Dtos;

public record AdminDashboardDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalAgencies,
    int TotalEntries,
    int TotalCertificates,
    int ActiveCertificates,
    int FormTemplates,
    int CertificateTemplates,
    int RecentAuditLogs);

public record AdminUserListItemDto(
    Guid Uuid,
    string Email,
    string Status,
    string FullName,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record AdminUserDetailDto(
    Guid Uuid,
    string Email,
    string Status,
    string FirstName,
    string LastName,
    string? Phone,
    string? CompanyName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<long> AgencyIds,
    IReadOnlyDictionary<string, IReadOnlyList<long>> RoleAgencies,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record CreateAdminUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    IReadOnlyList<string> RoleCodes,
    long? AgencyId,
    IReadOnlyDictionary<string, IReadOnlyList<long>>? RoleAgencies);

public record UpdateAdminUserRequest(
    string? FirstName,
    string? LastName,
    string? Status,
    IReadOnlyList<string>? RoleCodes,
    long? AgencyId,
    IReadOnlyDictionary<string, IReadOnlyList<long>>? RoleAgencies);

public record AdminRoleDto(string Code, string Name, string? Description, bool RequiresAgency);

public record AdminSettingsDto(IReadOnlyDictionary<string, string> Settings);

public record UpdateAdminSettingsRequest(IReadOnlyDictionary<string, string> Settings);

public record AdminAgencyDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    long? ParentId,
    string? ParentName,
    bool IsActive,
    int MemberCount);

public record AdminAgencyUserDto(
    Guid UserUuid,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    DateTime AssignedAt);

public record AdminAgencyLeadershipUserDto(
    Guid UserUuid,
    string FullName,
    string Email,
    DateTime AssignedAt);

public record AdminAgencyDetailDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    string? Address,
    string? ContactNumber,
    string? Email,
    string? LogoUrl,
    long? ParentId,
    string? ParentName,
    bool IsActive,
    int TotalUsers,
    int TotalEntries,
    AdminAgencyLeadershipUserDto? Secretary,
    IReadOnlyList<AdminAgencyLeadershipUserDto> Undersecretaries,
    IReadOnlyList<AdminAgencyUserDto> Users);

public record AssignAgencySecretaryRequest(Guid? UserUuid);

public record AssignAgencyUndersecretariesRequest(IReadOnlyList<Guid> UserUuids);

public record CreateAgencyRequest(
    string Code,
    string Name,
    long? ParentId,
    string? Description,
    string? Address,
    string? ContactNumber,
    string? Email);

public record UpdateAgencyRequest(
    string Code,
    string Name,
    long? ParentId,
    string? Description,
    string? Address,
    string? ContactNumber,
    string? Email,
    bool IsActive);

public record AdminCommodityDto(long Id, string Code, string Name, string CategoryName, bool IsActive);

public record CreateCommodityRequest(long CategoryId, string Code, string Name);

public record UpdateCommodityRequest(string Name, bool IsActive);

public record ProcessingFeeConfigDto(long Id, long AgencyId, string AgencyCode, string EntryType, decimal Amount, string Currency, bool IsActive);

public record UpsertProcessingFeeRequest(long AgencyId, string EntryType, decimal Amount, string Currency, bool IsActive);

public record PaymentGatewaySettingsDto(
    bool Enabled,
    string Mode,
    bool HasApiKey,
    string ApiKeyMasked,
    bool HasWebhookSecret,
    string? PublicKey);

public record GlobalEntryProcessingFeeDto(string EntryType, decimal Amount, string Currency);

public record AdminPaymentSettingsDto(IReadOnlyList<GlobalEntryProcessingFeeDto> ProcessingFees);

public record UpdateAdminPaymentSettingsRequest(
    decimal ImportFeeAmount,
    decimal ExportFeeAmount,
    string Currency);

public record AdminEntryPaymentListItemDto(
    long Id,
    Guid BillUuid,
    string BillNumber,
    string? EntryReferenceNo,
    string? AgencyCode,
    string ClientName,
    decimal Amount,
    string PaymentMethod,
    string Status,
    string? ExternalReference,
    string? GatewayTransactionId,
    DateTime CreatedAt,
    DateTime? PaidAt);

public record AdminPendingEntryCashPaymentDto(
    Guid BillUuid,
    string BillNumber,
    string? EntryReferenceNo,
    string? AgencyCode,
    string ClientName,
    decimal Amount,
    string? ExternalReference,
    DateTime SubmittedAt);

public record VerifyAdminEntryCashPaymentRequest(bool Approved, string? Notes);

public record AdminRevenueAgencyBreakdownDto(string AgencyCode, string AgencyName, decimal Amount, int PaymentCount);

public record AdminRevenueMonthlyBreakdownDto(string Month, decimal Amount, int PaymentCount);

public record AdminRevenueSummaryDto(
    decimal TotalCollected,
    decimal PendingAmount,
    int PaidCount,
    int PendingCount,
    int FailedCount,
    IReadOnlyList<AdminRevenueAgencyBreakdownDto> ByAgency,
    IReadOnlyList<AdminRevenueMonthlyBreakdownDto> ByMonth);

public record FormTemplateListItemDto(
    Guid Uuid,
    string Name,
    string FormType,
    string Status,
    bool IsActive,
    int LatestVersion,
    bool HasPublishedVersion,
    IReadOnlyList<long> AgencyIds,
    int FieldCount,
    DateTime CreatedAt,
    int SubmissionCount);

public record FormTemplateVersionSummaryDto(int VersionNumber, bool IsPublished, DateTime CreatedAt);

public record FormTemplateDetailDto(
    Guid Uuid,
    string Name,
    string FormType,
    string Status,
    bool IsActive,
    string SchemaJson,
    int VersionNumber,
    IReadOnlyList<long> AgencyIds,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<FormTemplateVersionSummaryDto> Versions);

public record SaveFormTemplateRequest(string Name, string FormType, string SchemaJson, IReadOnlyList<long> AgencyIds, bool Publish);

public record SetFormActiveRequest(bool IsActive);

public record CloneFormTemplateRequest(string Name);

public record CertificateTemplateListItemDto(
    Guid Uuid,
    string Name,
    string? Description,
    string? AgencyCode,
    bool IsActive,
    int LatestVersion,
    bool HasPublishedVersion,
    int ElementCount,
    IReadOnlyList<string> ProcessTypes,
    DateTime CreatedAt);

public record CertificateTemplateVersionSummaryDto(int VersionNumber, bool IsPublished, DateTime CreatedAt);

public record CertificateTemplateDetailDto(
    Guid Uuid,
    string Name,
    string? Description,
    long? AgencyId,
    bool IsActive,
    int VersionNumber,
    bool IsPublished,
    string? LayoutJson,
    IReadOnlyList<string> ProcessTypes,
    IReadOnlyList<CertificateElementDto> Elements,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<CertificateTemplateVersionSummaryDto> Versions);

public record CertificateElementDto(long Id, string ElementType, string Label, string? ConfigJson, int SortOrder);

public record SaveCertificateTemplateRequest(
    string Name,
    string? Description,
    long? AgencyId,
    IReadOnlyList<CertificateElementInput> Elements,
    IReadOnlyList<string>? ProcessTypes,
    bool Publish,
    bool? IsActive,
    string? LayoutJson);

public record CertificateElementInput(string ElementType, string Label, string? ConfigJson, int SortOrder);

public record AdminCertificateListItemDto(
    Guid Uuid,
    string CertificateNumber,
    string Title,
    string Status,
    string HolderName,
    string? EntryReferenceNo,
    DateTime IssuedAt,
    DateTime? ExpiresAt,
    DateTime? RevokedAt);

public record IssueCertificateRequest(Guid EntryUuid, long? TemplateId, DateTime? ExpiresAt);

public record RevokeCertificateRequest(string Reason);

public record AdminApprovedEntryDto(Guid Uuid, string ReferenceNo, string ApplicantName, string AgencyCode, bool HasActiveCertificate);

public record AuditLogListItemDto(
    long Id,
    string Action,
    string EntityType,
    string? EntityId,
    string? ActorName,
    DateTime CreatedAt);

public record PagedAuditLogs(PagedResult<AuditLogListItemDto> Logs);
