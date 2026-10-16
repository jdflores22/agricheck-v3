using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Application.Compliance;
using AgriCheck.Application.DaPortal;
using AgriCheck.Application.DaPortal.Dtos;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/da")]
[Authorize(Roles = "ROLE_DA_SECRETARY,ROLE_DA_UNDERSECRETARY,ROLE_ADMIN")]
public class DaPortalController : AgencyPortalControllerBase
{
    private readonly IDaDashboardService _dashboardService;
    private readonly IDaAgencyOverviewService _agencyOverviewService;
    private readonly IDaOversightReportService _reportService;
    private readonly IDaWarehouseManagementService _warehouseService;
    private readonly IDaImporterProfileService _importerProfileService;
    private readonly IDaFacilityRegistrationService _facilityRegistrationService;
    private readonly IDaWarehouseLtoApplicationService _ltoApplicationService;
    private readonly IDaLogisticsLiveService _logisticsLiveService;

    public DaPortalController(
        IDaDashboardService dashboardService,
        IDaAgencyOverviewService agencyOverviewService,
        IDaOversightReportService reportService,
        IDaWarehouseManagementService warehouseService,
        IDaImporterProfileService importerProfileService,
        IDaFacilityRegistrationService facilityRegistrationService,
        IDaWarehouseLtoApplicationService ltoApplicationService,
        IDaLogisticsLiveService logisticsLiveService)
    {
        _dashboardService = dashboardService;
        _agencyOverviewService = agencyOverviewService;
        _reportService = reportService;
        _warehouseService = warehouseService;
        _importerProfileService = importerProfileService;
        _facilityRegistrationService = facilityRegistrationService;
        _ltoApplicationService = ltoApplicationService;
        _logisticsLiveService = logisticsLiveService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<DaDashboardDto>>> Dashboard(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _dashboardService.GetDashboardAsync(cancellationToken));

    [HttpGet("agencies")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DaAgencySummaryDto>>>> Agencies(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _agencyOverviewService.ListAgenciesAsync(cancellationToken));

    [HttpGet("agencies/{id:long}")]
    public async Task<ActionResult<ApiResponse<DaAgencyOversightDto>>> AgencyOversight(long id, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _agencyOverviewService.GetOversightAsync(id, cancellationToken));

    [HttpGet("reports/summary")]
    public async Task<ActionResult<ApiResponse<DaOversightReportDto>>> ReportSummary(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _reportService.GetReportAsync(cancellationToken));

    [HttpGet("reports/mav")]
    public async Task<ActionResult<ApiResponse<DaMavNationalReportDto>>> MavNationalReport([FromQuery] int? mavYear, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _reportService.GetMavNationalReportAsync(mavYear, cancellationToken));

    [HttpGet("reports/commodity-stock")]
    public async Task<ActionResult<ApiResponse<DaCommodityStockReportDto>>> CommodityStockReport(
        [FromQuery] string? hsCode,
        [FromQuery] string? commodityName,
        [FromQuery] string? agencyCode,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _reportService.GetCommodityStockReportAsync(
            new DaCommodityStockQuery(hsCode, commodityName, agencyCode),
            cancellationToken));

    [HttpGet("reports/geo-stock")]
    public async Task<ActionResult<ApiResponse<DaGeoStockReportDto>>> GeoStockReport(
        [FromQuery] long? regionId,
        [FromQuery] long? provinceId,
        [FromQuery] long? cityId,
        [FromQuery] long? barangayId,
        [FromQuery] string? hsCode,
        [FromQuery] string? commodityName,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _reportService.GetGeoStockReportAsync(
            new DaGeoStockQuery(regionId, provinceId, cityId, barangayId, hsCode, commodityName),
            cancellationToken));

    [HttpGet("reports/storage-tally")]
    public async Task<ActionResult<ApiResponse<DaStorageTallySummaryDto>>> StorageTallyReport(
        [FromQuery] int? year,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _reportService.GetStorageTallySummaryAsync(year, cancellationToken));

    [HttpGet("reports/import-pipeline")]
    public async Task<ActionResult<ApiResponse<DaImportPipelineReportDto>>> ImportPipelineReport(
        [FromQuery] string? hsCode,
        [FromQuery] string? commodityName,
        [FromQuery] string? agencyCode,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _reportService.GetImportPipelineReportAsync(
            new DaImportPipelineQuery(hsCode, commodityName, agencyCode),
            cancellationToken));

    [HttpGet("warehouses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DaWarehouseListItemDto>>>> Warehouses(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _warehouseService.ListAsync(cancellationToken));

    [HttpGet("warehouses/{id:long}")]
    public async Task<ActionResult<ApiResponse<DaWarehouseDetailDto>>> WarehouseDetail(long id, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _warehouseService.GetDetailAsync(id, cancellationToken));

    [HttpPost("warehouses")]
    public async Task<ActionResult<ApiResponse<DaWarehouseListItemDto>>> CreateWarehouse(
        [FromBody] SaveDaWarehouseRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _warehouseService.CreateAsync(request, cancellationToken));

    [HttpPut("warehouses/{id:long}")]
    public async Task<ActionResult<ApiResponse<DaWarehouseListItemDto>>> UpdateWarehouse(
        long id,
        [FromBody] SaveDaWarehouseRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _warehouseService.UpdateAsync(id, request, cancellationToken));

    [HttpGet("warehouses/{id:long}/operating-license")]
    public async Task<ActionResult<ApiResponse<WarehouseOperatingLicenseDto>>> GetWarehouseOperatingLicense(
        long id,
        CancellationToken cancellationToken)
    {
        var license = await _warehouseService.GetOperatingLicenseAsync(id, cancellationToken);
        if (license is null)
        {
            return NotFound(ApiResponse<WarehouseOperatingLicenseDto>.Fail("NOT_FOUND", "No warehouse LTO on record."));
        }

        return Ok(ApiResponse<WarehouseOperatingLicenseDto>.Ok(license));
    }

    [HttpPost("warehouses/{id:long}/operating-license")]
    public async Task<ActionResult<ApiResponse<WarehouseOperatingLicenseDto>>> IssueWarehouseOperatingLicense(
        long id,
        [FromBody] IssueWarehouseOperatingLicenseRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _warehouseService.IssueOperatingLicenseAsync(id, request, cancellationToken));

    [HttpGet("warehouses/{id:long}/facility-registration")]
    public async Task<ActionResult<ApiResponse<FacilityRegistrationDaReviewDto>>> PendingFacilityRegistration(
        long id,
        CancellationToken cancellationToken)
    {
        var item = await _facilityRegistrationService.GetPendingAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<FacilityRegistrationDaReviewDto>.Fail("NOT_FOUND", "No submitted RSAS registration."));
        }

        return Ok(ApiResponse<FacilityRegistrationDaReviewDto>.Ok(item));
    }

    [HttpPost("warehouses/{id:long}/facility-registration/verify")]
    public async Task<ActionResult<ApiResponse<FacilityRegistrationDaReviewDto>>> VerifyFacilityRegistration(
        long id,
        [FromBody] VerifyFacilityRegistrationRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _facilityRegistrationService.VerifyAsync(id, request, cancellationToken));

    [HttpPost("warehouses/{id:long}/csw-accreditation")]
    public async Task<ActionResult<ApiResponse<FacilityAccreditationSummaryDto>>> RegisterWarehouseCsw(
        long id,
        [FromBody] RegisterFacilityCswRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _facilityRegistrationService.RegisterCswAsync(id, request, cancellationToken));

    [HttpGet("warehouse-lto-applications")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DaWarehouseLtoApplicationListItemDto>>>> PendingWarehouseLtoApplications(
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _ltoApplicationService.ListPendingAsync(cancellationToken));

    [HttpGet("warehouse-lto-applications/recent")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DaWarehouseLtoApplicationListItemDto>>>> RecentWarehouseLtoApplications(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _ltoApplicationService.ListRecentAsync(limit, cancellationToken));

    [HttpPost("warehouse-lto-applications/{uuid:guid}/resolve")]
    public async Task<ActionResult<ApiResponse<WarehouseLtoApplicationDto>>> ResolveWarehouseLtoApplication(
        Guid uuid,
        [FromBody] ResolveWarehouseLtoApplicationRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _ltoApplicationService.ResolveAsync(uuid, request, cancellationToken));

    [HttpGet("importers")]
    public async Task<ActionResult<ApiResponse<PagedResult<DaImporterListItemDto>>>> Importers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _importerProfileService.ListAsync(page, pageSize, search, cancellationToken));

    [HttpGet("importers/{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<DaImporterProfileDto>>> ImporterProfile(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _importerProfileService.GetProfileAsync(uuid, cancellationToken));

    [HttpGet("importers/{uuid:guid}/entries")]
    public async Task<ActionResult<ApiResponse<PagedResult<DaImporterEntryListItemDto>>>> ImporterEntries(
        Guid uuid,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _importerProfileService.ListEntriesAsync(uuid, page, pageSize, status, cancellationToken));

    [HttpGet("logistics/live")]
    public async Task<ActionResult<ApiResponse<DaLiveLogisticsSnapshotDto>>> LogisticsLive(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _logisticsLiveService.GetLiveSnapshotAsync(cancellationToken));

    [HttpGet("logistics/containers/{uuid:guid}/actual-route")]
    public async Task<ActionResult<ApiResponse<DaContainerActualRouteDto>>> ContainerActualRoute(
        Guid uuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _logisticsLiveService.GetActualRouteAsync(uuid, cancellationToken));

    [HttpGet("logistics/delivery-proof-photos/{photoUuid:guid}/download")]
    public async Task<IActionResult> DownloadDeliveryProofPhoto(Guid photoUuid, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _logisticsLiveService.DownloadDeliveryProofPhotoAsync(photoUuid, cancellationToken);
            return PhysicalFile(download.PhysicalPath, download.ContentType, download.DownloadFileName);
        }
        catch (ClientPortalException ex) when (ex.Code == "NOT_FOUND")
        {
            return NotFound(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpPost("logistics/containers/{uuid:guid}/clear-compliance-hold")]
    public async Task<ActionResult<ApiResponse<DaDeliveryComplianceHoldDto>>> ClearComplianceHold(
        Guid uuid,
        [FromBody] DaClearDeliveryComplianceHoldRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _logisticsLiveService.ClearComplianceHoldAsync(uuid, request, cancellationToken));
}
