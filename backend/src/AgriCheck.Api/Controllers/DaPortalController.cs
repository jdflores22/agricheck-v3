using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Application.DaPortal;
using AgriCheck.Application.DaPortal.Dtos;
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

    public DaPortalController(
        IDaDashboardService dashboardService,
        IDaAgencyOverviewService agencyOverviewService,
        IDaOversightReportService reportService,
        IDaWarehouseManagementService warehouseService,
        IDaImporterProfileService importerProfileService)
    {
        _dashboardService = dashboardService;
        _agencyOverviewService = agencyOverviewService;
        _reportService = reportService;
        _warehouseService = warehouseService;
        _importerProfileService = importerProfileService;
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
}
