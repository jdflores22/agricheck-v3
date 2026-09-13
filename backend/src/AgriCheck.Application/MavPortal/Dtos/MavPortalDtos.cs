using AgriCheck.Application.ClientPortal.Dtos;

namespace AgriCheck.Application.MavPortal.Dtos;

public record MavImporterDashboardDto(
    int OpenPeriods,
    int MyApplications,
    int PendingApplications,
    int ActiveLicenses,
    int ActiveMics,
    decimal TotalAvailableVolume);

public record MavAdminDashboardDto(
    int OpenPeriods,
    int PendingApplications,
    int ActiveLicenses,
    int ActiveMics,
    int TotalApplications,
    int ComplianceAlerts);

public record MavApplicationPeriodListItemDto(
    Guid Uuid,
    int MavYear,
    string PoolType,
    string Status,
    DateTime OpeningDate,
    DateTime ClosingDate,
    int ApplicationCount,
    int AllocationCount,
    long? AgencyId,
    string? AgencyCode);

public record MavApplicationPeriodDetailDto(
    Guid Uuid,
    int MavYear,
    string PoolType,
    string Status,
    DateTime OpeningDate,
    DateTime ClosingDate,
    IReadOnlyList<MavCommodityAllocationDto> Allocations,
    long? AgencyId,
    string? AgencyCode);

public record MavAgencyContextDto(
    long AgencyId,
    bool IsActive,
    int OpenPeriodCount,
    int ActiveLicenseCount,
    int AvailableMicCount,
    string? AgencyCode,
    bool IsProgramAvailable,
    bool ImporterHasMavAccess);

public record MavCommodityAllocationDto(
    long Id,
    string HsCode,
    string CommodityName,
    decimal TotalVolume,
    decimal AllocatedVolume,
    decimal AvailableVolume,
    decimal MinimumImportVolume);

public record CreateMavApplicationPeriodRequest(int MavYear, string PoolType, DateTime OpeningDate, DateTime ClosingDate, long? AgencyId);

public record UpsertMavCommodityAllocationRequest(long CommodityId, string HsCode, string CommodityName, decimal TotalVolume, decimal MinimumImportVolume, long? AgencyId);

public record MavApplicationListItemDto(
    Guid Uuid,
    string ReferenceNumber,
    string Status,
    int MavYear,
    string PoolType,
    string HsCode,
    string CommodityName,
    decimal RequestedVolume,
    decimal? AllocatedVolume,
    string ApplicantName,
    DateTime? SubmittedAt);

public record MavApplicationDetailDto(
    Guid Uuid,
    string ReferenceNumber,
    string Status,
    Guid PeriodUuid,
    int MavYear,
    string PoolType,
    string HsCode,
    string CommodityName,
    decimal RequestedVolume,
    decimal? AllocatedVolume,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    string? RejectionReason,
    Guid? LicenseUuid,
    long? AgencyId,
    string? AgencyCode);

public record CreateMavApplicationRequest(Guid PeriodUuid, string HsCode, string CommodityName, decimal RequestedVolume, long? AccreditationSubmissionId, Guid? HsDetailUuid = null);

public record UpdateMavApplicationRequest(string HsCode, string CommodityName, decimal RequestedVolume, long? AccreditationSubmissionId, Guid? HsDetailUuid = null);

public record ReviewMavApplicationRequest(decimal AllocatedVolume);

public record RejectMavApplicationRequest(string Reason);

public record MavLicenseListItemDto(
    Guid Uuid,
    string LicenseNumber,
    string Status,
    int MavYear,
    string PoolType,
    string HsCode,
    string CommodityName,
    decimal AwardedVolume,
    decimal AvailableVolume,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    string HolderName);

public record MavLicenseDetailDto(
    Guid Uuid,
    string LicenseNumber,
    string Status,
    int MavYear,
    string PoolType,
    string HsCode,
    string CommodityName,
    decimal AwardedVolume,
    decimal UtilizedVolume,
    decimal AvailableVolume,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    IReadOnlyList<MavMicListItemDto> ImportCertificates,
    IReadOnlyList<MavAccountTransactionDto> RecentTransactions);

public record MavMicListItemDto(
    Guid Uuid,
    string CertificateNumber,
    string Status,
    string HsCode,
    string CommodityName,
    decimal AuthorizedVolume,
    decimal UtilizedVolume,
    decimal AvailableVolume,
    DateTime IssuedAt,
    DateTime ExpiresAt);

public record MavAccountTransactionDto(
    long Id,
    string TransactionType,
    decimal Volume,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Reference,
    DateTime CreatedAt);

public record IssueMicRequest(decimal Volume);

public record UtilizeMicRequest(Guid EntryUuid, decimal Volume);

public record MavComplianceDashboardDto(
    int ExpiredLicenses,
    int ExpiredMics,
    int ExpiringLicenses,
    int ExpiringMics,
    int LowUtilizationAccounts,
    int OverUtilizedAccounts,
    int PendingApplications);

public record MavComplianceAlertsDto(
    MavComplianceDashboardDto Summary,
    IReadOnlyList<MavExpiringLicenseAlertDto> ExpiringLicenses,
    IReadOnlyList<MavExpiringMicAlertDto> ExpiringMics,
    IReadOnlyList<MavLowUtilizationAlertDto> LowUtilizationAccounts,
    IReadOnlyList<MavOverUtilizedAlertDto> OverUtilizedAccounts,
    IReadOnlyList<MavExpiredLicenseAlertDto> ExpiredLicenses,
    IReadOnlyList<MavExpiredMicAlertDto> ExpiredMics);

public record MavExpiringLicenseAlertDto(
    Guid Uuid,
    string LicenseNumber,
    string HolderName,
    string HsCode,
    string CommodityName,
    decimal AvailableVolume,
    DateTime ExpiresAt,
    int DaysRemaining);

public record MavExpiredLicenseAlertDto(
    Guid Uuid,
    string LicenseNumber,
    string HolderName,
    string HsCode,
    string CommodityName,
    DateTime ExpiresAt);

public record MavExpiringMicAlertDto(
    Guid Uuid,
    string CertificateNumber,
    string HolderName,
    string HsCode,
    string CommodityName,
    decimal AvailableVolume,
    DateTime ExpiresAt,
    int DaysRemaining);

public record MavExpiredMicAlertDto(
    Guid Uuid,
    string CertificateNumber,
    string HolderName,
    string HsCode,
    string CommodityName,
    DateTime ExpiresAt);

public record MavLowUtilizationAlertDto(
    Guid LicenseUuid,
    string LicenseNumber,
    string HolderName,
    string HsCode,
    string CommodityName,
    decimal AwardedVolume,
    decimal UtilizedVolume,
    decimal UtilizationPercent);

public record MavOverUtilizedAlertDto(
    Guid LicenseUuid,
    string LicenseNumber,
    string HolderName,
    string HsCode,
    string CommodityName,
    decimal AwardedVolume,
    decimal UtilizedVolume);

public record MavReportSummaryDto(
    int TotalApplications,
    int ApprovedApplications,
    int RejectedApplications,
    int ActiveLicenses,
    int IssuedMics,
    decimal TotalAwardedVolume,
    decimal TotalUtilizedVolume);

public record MavReportDetailDto(
    MavReportSummaryDto Summary,
    IReadOnlyList<MavReportCommodityRowDto> ByCommodity,
    IReadOnlyList<MavReportPoolRowDto> ByPoolType);

public record MavReportCommodityRowDto(
    string HsCode,
    string CommodityName,
    int ApplicationCount,
    int ActiveLicenses,
    decimal AwardedVolume,
    decimal UtilizedVolume);

public record MavReportPoolRowDto(
    string PoolType,
    int ApplicationCount,
    int ActiveLicenses,
    decimal AwardedVolume,
    decimal UtilizedVolume);

public record MavHsCategoryListItemDto(
    Guid Uuid,
    string HsCode,
    string Description,
    string? AgencyCode,
    bool IsActive,
    int HeadingCount);

public record MavHsCategoryDetailDto(
    Guid Uuid,
    string HsCode,
    string Description,
    string? Notes,
    long? AgencyId,
    string? AgencyCode,
    bool IsActive,
    IReadOnlyList<MavHsHeadingWithDetailsDto> Headings);

public record MavHsHeadingListItemDto(
    Guid Uuid,
    string HeadingNumber,
    string Description,
    bool IsActive,
    int DetailCount);

public record MavHsHeadingWithDetailsDto(
    Guid Uuid,
    string HeadingNumber,
    string Description,
    bool IsActive,
    IReadOnlyList<MavHsDetailSummaryDto> Details);

public record MavHsDetailSummaryDto(
    Guid Uuid,
    string Description,
    string DisplayLabel,
    bool IsActive);

public record MavHsDetailListItemDto(
    Guid Uuid,
    string Description,
    string DisplayLabel,
    string HsCode,
    string HeadingNumber,
    bool IsActive);

public record CreateMavHsCategoryRequest(string HsCode, string Description, string? Notes, long? AgencyId);
public record UpdateMavHsCategoryRequest(string HsCode, string Description, string? Notes, long? AgencyId, bool IsActive);
public record CreateMavHsHeadingRequest(string HeadingNumber, string Description, string? Notes);
public record UpdateMavHsHeadingRequest(string HeadingNumber, string Description, string? Notes, bool IsActive);
public record CreateMavHsDetailRequest(string Description, string? Notes);
public record UpdateMavHsDetailRequest(string Description, string? Notes, bool IsActive);

public record MavYearTransitionRequest(int FromYear, int ToYear, string PoolType);

public record MavYearTransitionResultDto(int PeriodsCreated, int AllocationsCopied);
