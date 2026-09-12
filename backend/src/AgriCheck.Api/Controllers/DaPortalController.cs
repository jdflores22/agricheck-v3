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

    public DaPortalController(
        IDaDashboardService dashboardService,
        IDaAgencyOverviewService agencyOverviewService,
        IDaOversightReportService reportService,
        IDaWarehouseManagementService warehouseService)
    {
        _dashboardService = dashboardService;
        _agencyOverviewService = agencyOverviewService;
        _reportService = reportService;
        _warehouseService = warehouseService;
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
}
