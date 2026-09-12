using AgriCheck.Application.MavPortal.Dtos;

namespace AgriCheck.Application.MavPortal;

public interface IMavDashboardService
{
    Task<MavImporterDashboardDto> GetImporterDashboardAsync(CancellationToken cancellationToken = default);
    Task<MavAdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IMavApplicationPeriodService
{
    Task<IReadOnlyList<MavApplicationPeriodListItemDto>> ListOpenAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MavApplicationPeriodListItemDto>> ListAdminAsync(CancellationToken cancellationToken = default);
    Task<MavApplicationPeriodDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<MavApplicationPeriodDetailDto> CreateAsync(CreateMavApplicationPeriodRequest request, CancellationToken cancellationToken = default);
    Task<MavApplicationPeriodDetailDto> OpenAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<MavApplicationPeriodDetailDto> CloseAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<MavCommodityAllocationDto> UpsertAllocationAsync(Guid periodUuid, UpsertMavCommodityAllocationRequest request, CancellationToken cancellationToken = default);
}

public interface IMavApplicationService
{
    Task<IReadOnlyList<MavApplicationListItemDto>> ListMineAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MavApplicationListItemDto>> ListAdminAsync(string? status, CancellationToken cancellationToken = default);
    Task<MavApplicationDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<MavApplicationDetailDto> CreateAsync(CreateMavApplicationRequest request, CancellationToken cancellationToken = default);
    Task<MavApplicationDetailDto> UpdateAsync(Guid uuid, UpdateMavApplicationRequest request, CancellationToken cancellationToken = default);
    Task<MavApplicationDetailDto> SubmitAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<MavApplicationDetailDto> ApproveAsync(Guid uuid, ReviewMavApplicationRequest request, CancellationToken cancellationToken = default);
    Task<MavApplicationDetailDto> RejectAsync(Guid uuid, RejectMavApplicationRequest request, CancellationToken cancellationToken = default);
}

public interface IMavLicenseService
{
    Task<IReadOnlyList<MavLicenseListItemDto>> ListMineAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MavLicenseListItemDto>> ListAdminAsync(CancellationToken cancellationToken = default);
    Task<MavLicenseDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<MavLicenseListItemDto> RevokeAsync(Guid uuid, string reason, CancellationToken cancellationToken = default);
}

public interface IMavMicService
{
    Task<MavMicListItemDto> IssueAsync(Guid licenseUuid, IssueMicRequest request, CancellationToken cancellationToken = default);
    Task<MavMicListItemDto> UtilizeAsync(Guid micUuid, UtilizeMicRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MavMicListItemDto>> ListAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MavMicListItemDto>> ListAdminAsync(CancellationToken cancellationToken = default);
}

public interface IMavComplianceService
{
    Task<MavComplianceDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<MavComplianceAlertsDto> GetAlertsAsync(CancellationToken cancellationToken = default);
}

public interface IMavReportService
{
    Task<MavReportSummaryDto> GetSummaryAsync(int? mavYear, CancellationToken cancellationToken = default);
    Task<MavReportDetailDto> GetDetailAsync(int? mavYear, CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(int? mavYear, CancellationToken cancellationToken = default);
    Task<byte[]> ExportPdfAsync(int? mavYear, CancellationToken cancellationToken = default);
}

public interface IMavHsLibraryService
{
    Task<IReadOnlyList<MavHsCategoryListItemDto>> ListCategoriesAsync(long? agencyId, CancellationToken cancellationToken = default);
    Task<MavHsCategoryDetailDto?> GetCategoryAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<MavHsCategoryDetailDto> CreateCategoryAsync(CreateMavHsCategoryRequest request, CancellationToken cancellationToken = default);
    Task<MavHsCategoryDetailDto> UpdateCategoryAsync(Guid uuid, UpdateMavHsCategoryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MavHsHeadingListItemDto>> ListHeadingsAsync(Guid categoryUuid, CancellationToken cancellationToken = default);
    Task<MavHsHeadingListItemDto> CreateHeadingAsync(Guid categoryUuid, CreateMavHsHeadingRequest request, CancellationToken cancellationToken = default);
    Task<MavHsHeadingListItemDto> UpdateHeadingAsync(Guid uuid, UpdateMavHsHeadingRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MavHsDetailListItemDto>> ListDetailsAsync(Guid headingUuid, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MavHsDetailListItemDto>> ListPickerDetailsAsync(long? agencyId, Guid? categoryUuid, CancellationToken cancellationToken = default);
    Task<MavHsDetailListItemDto> CreateDetailAsync(Guid headingUuid, CreateMavHsDetailRequest request, CancellationToken cancellationToken = default);
    Task<MavHsDetailListItemDto> UpdateDetailAsync(Guid uuid, UpdateMavHsDetailRequest request, CancellationToken cancellationToken = default);
}

public interface IMavYearTransitionService
{
    Task<MavYearTransitionResultDto> TransitionAsync(MavYearTransitionRequest request, CancellationToken cancellationToken = default);
}
