using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Compliance;
using AgriCheck.Application.DaPortal.Dtos;
using AgriCheck.Application.OpsPortal;

namespace AgriCheck.Application.DaPortal;

public interface IDaDashboardService
{
    Task<DaDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IDaAgencyOverviewService
{
    Task<IReadOnlyList<DaAgencySummaryDto>> ListAgenciesAsync(CancellationToken cancellationToken = default);
    Task<DaAgencyOversightDto> GetOversightAsync(long id, CancellationToken cancellationToken = default);
}

public interface IDaOversightReportService
{
    Task<DaOversightReportDto> GetReportAsync(CancellationToken cancellationToken = default);
    Task<DaMavNationalReportDto> GetMavNationalReportAsync(int? mavYear, CancellationToken cancellationToken = default);
    Task<DaGeoStockReportDto> GetGeoStockReportAsync(DaGeoStockQuery query, CancellationToken cancellationToken = default);
    Task<DaCommodityStockReportDto> GetCommodityStockReportAsync(DaCommodityStockQuery query, CancellationToken cancellationToken = default);
    Task<DaImportPipelineReportDto> GetImportPipelineReportAsync(DaImportPipelineQuery query, CancellationToken cancellationToken = default);

    Task<DaStorageTallySummaryDto> GetStorageTallySummaryAsync(int? year, CancellationToken cancellationToken = default);
}

public interface IDaWarehouseManagementService
{
    Task<IReadOnlyList<DaWarehouseListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<DaWarehouseDetailDto> GetDetailAsync(long id, CancellationToken cancellationToken = default);
    Task<DaWarehouseListItemDto> CreateAsync(SaveDaWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<DaWarehouseListItemDto> UpdateAsync(long id, SaveDaWarehouseRequest request, CancellationToken cancellationToken = default);

    Task<WarehouseOperatingLicenseDto?> GetOperatingLicenseAsync(long warehouseFacilityId, CancellationToken cancellationToken = default);

    Task<WarehouseOperatingLicenseDto> IssueOperatingLicenseAsync(
        long warehouseFacilityId,
        IssueWarehouseOperatingLicenseRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDaFacilityRegistrationService
{
    Task<FacilityRegistrationDaReviewDto?> GetPendingAsync(long warehouseFacilityId, CancellationToken cancellationToken = default);

    Task<FacilityRegistrationDaReviewDto> VerifyAsync(
        long warehouseFacilityId,
        VerifyFacilityRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<FacilityAccreditationSummaryDto> RegisterCswAsync(
        long warehouseFacilityId,
        RegisterFacilityCswRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDaImporterProfileService
{
    Task<PagedResult<DaImporterListItemDto>> ListAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<DaImporterProfileDto> GetProfileAsync(Guid uuid, CancellationToken cancellationToken = default);
    Task<PagedResult<DaImporterEntryListItemDto>> ListEntriesAsync(Guid uuid, int page, int pageSize, string? status, CancellationToken cancellationToken = default);
}

public interface IDaLogisticsLiveService
{
    Task<DaLiveLogisticsSnapshotDto> GetLiveSnapshotAsync(CancellationToken cancellationToken = default);
    Task<DaContainerActualRouteDto> GetActualRouteAsync(Guid containerUuid, CancellationToken cancellationToken = default);
    Task<DaDeliveryComplianceHoldDto> ClearComplianceHoldAsync(
        Guid containerUuid,
        DaClearDeliveryComplianceHoldRequest request,
        CancellationToken cancellationToken = default);
}
