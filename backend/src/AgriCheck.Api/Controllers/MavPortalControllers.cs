using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Application.MavPortal;
using AgriCheck.Application.MavPortal.Dtos;
using AgriCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/mav/dashboard")]
[Authorize(Roles = "ROLE_MAV_IMPORTER,ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavDashboardController : MavPortalControllerBase
{
    private readonly IMavDashboardService _service;

    public MavDashboardController(IMavDashboardService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<MavImporterDashboardDto>>> GetImporter(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetImporterDashboardAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/mav/admin/dashboard")]
[Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavAdminDashboardController : MavPortalControllerBase
{
    private readonly IMavDashboardService _service;

    public MavAdminDashboardController(IMavDashboardService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<MavAdminDashboardDto>>> Get(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetAdminDashboardAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/mav/periods")]
[Authorize(Roles = "ROLE_MAV_IMPORTER,ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavPeriodsController : MavPortalControllerBase
{
    private readonly IMavApplicationPeriodService _service;

    public MavPeriodsController(IMavApplicationPeriodService service) => _service = service;

    [HttpGet("open")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavApplicationPeriodListItemDto>>>> ListOpen(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListOpenAsync(cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<MavApplicationPeriodDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<MavApplicationPeriodDetailDto>.Fail("NOT_FOUND", "Period not found."));
        return Ok(ApiResponse<MavApplicationPeriodDetailDto>.Ok(item));
    }
}

[ApiController]
[Route("api/v1/mav/admin/periods")]
[Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavAdminPeriodsController : MavPortalControllerBase
{
    private readonly IMavApplicationPeriodService _service;

    public MavAdminPeriodsController(IMavApplicationPeriodService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavApplicationPeriodListItemDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAdminAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavApplicationPeriodDetailDto>>> Create([FromBody] CreateMavApplicationPeriodRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateAsync(request, cancellationToken));

    [HttpPost("{uuid:guid}/open")]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavApplicationPeriodDetailDto>>> Open(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.OpenAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/close")]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavApplicationPeriodDetailDto>>> Close(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CloseAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/allocations")]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavCommodityAllocationDto>>> UpsertAllocation(Guid uuid, [FromBody] UpsertMavCommodityAllocationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpsertAllocationAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/mav/applications")]
[Authorize(Roles = "ROLE_MAV_IMPORTER,ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavApplicationsController : MavPortalControllerBase
{
    private readonly IMavApplicationService _service;

    public MavApplicationsController(IMavApplicationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavApplicationListItemDto>>>> ListMine(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListMineAsync(cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<MavApplicationDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<MavApplicationDetailDto>.Fail("NOT_FOUND", "Application not found."));
        return Ok(ApiResponse<MavApplicationDetailDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MavApplicationDetailDto>>> Create([FromBody] CreateMavApplicationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateAsync(request, cancellationToken));

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<MavApplicationDetailDto>>> Update(Guid uuid, [FromBody] UpdateMavApplicationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/submit")]
    public async Task<ActionResult<ApiResponse<MavApplicationDetailDto>>> Submit(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SubmitAsync(uuid, cancellationToken));
}

[ApiController]
[Route("api/v1/mav/admin/applications")]
[Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavAdminApplicationsController : MavPortalControllerBase
{
    private readonly IMavApplicationService _service;

    public MavAdminApplicationsController(IMavApplicationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavApplicationListItemDto>>>> List([FromQuery] string? status, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAdminAsync(status, cancellationToken));

    [HttpPost("{uuid:guid}/approve")]
    public async Task<ActionResult<ApiResponse<MavApplicationDetailDto>>> Approve(Guid uuid, [FromBody] ReviewMavApplicationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ApproveAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/reject")]
    public async Task<ActionResult<ApiResponse<MavApplicationDetailDto>>> Reject(Guid uuid, [FromBody] RejectMavApplicationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.RejectAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/mav/licenses")]
[Authorize(Roles = "ROLE_MAV_IMPORTER,ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavLicensesController : MavPortalControllerBase
{
    private readonly IMavLicenseService _service;
    private readonly IMavMicService _micService;

    public MavLicensesController(IMavLicenseService service, IMavMicService micService)
    {
        _service = service;
        _micService = micService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavLicenseListItemDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListMineAsync(cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<MavLicenseDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<MavLicenseDetailDto>.Fail("NOT_FOUND", "License not found."));
        return Ok(ApiResponse<MavLicenseDetailDto>.Ok(item));
    }

    [HttpPost("{uuid:guid}/mic")]
    public async Task<ActionResult<ApiResponse<MavMicListItemDto>>> IssueMic(Guid uuid, [FromBody] IssueMicRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _micService.IssueAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/mav/admin/licenses")]
[Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavAdminLicensesController : MavPortalControllerBase
{
    private readonly IMavLicenseService _service;

    public MavAdminLicensesController(IMavLicenseService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavLicenseListItemDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAdminAsync(cancellationToken));

    [HttpPost("{uuid:guid}/revoke")]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavLicenseListItemDto>>> Revoke(Guid uuid, [FromBody] RejectMavApplicationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.RevokeAsync(uuid, request.Reason, cancellationToken));
}

[ApiController]
[Route("api/v1/mav/mic")]
[Authorize(Roles = "ROLE_MAV_IMPORTER,ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavMicController : MavPortalControllerBase
{
    private readonly IMavMicService _service;

    public MavMicController(IMavMicService service) => _service = service;

    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavMicListItemDto>>>> ListAvailable(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAvailableAsync(cancellationToken));

    [HttpPost("{uuid:guid}/utilize")]
    public async Task<ActionResult<ApiResponse<MavMicListItemDto>>> Utilize(Guid uuid, [FromBody] UtilizeMicRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UtilizeAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/mav/admin/mic")]
[Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavAdminMicController : MavPortalControllerBase
{
    private readonly IMavMicService _service;

    public MavAdminMicController(IMavMicService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavMicListItemDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAdminAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/mav/compliance")]
[Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavComplianceController : MavPortalControllerBase
{
    private readonly IMavComplianceService _service;

    public MavComplianceController(IMavComplianceService service) => _service = service;

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<MavComplianceDashboardDto>>> Dashboard(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDashboardAsync(cancellationToken));

    [HttpGet("alerts")]
    public async Task<ActionResult<ApiResponse<MavComplianceAlertsDto>>> Alerts(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetAlertsAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/mav/reports")]
[Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavReportsController : MavPortalControllerBase
{
    private readonly IMavReportService _service;

    public MavReportsController(IMavReportService service) => _service = service;

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<MavReportSummaryDto>>> Summary([FromQuery] int? mavYear, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetSummaryAsync(mavYear, cancellationToken));

    [HttpGet("detail")]
    public async Task<ActionResult<ApiResponse<MavReportDetailDto>>> Detail([FromQuery] int? mavYear, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDetailAsync(mavYear, cancellationToken));

    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] int? mavYear, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _service.ExportCsvAsync(mavYear, cancellationToken);
            var fileName = $"mav-report-{mavYear ?? DateTime.UtcNow.Year}.csv";
            return File(bytes, "text/csv", fileName);
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpGet("export.pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] int? mavYear, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _service.ExportPdfAsync(mavYear, cancellationToken);
            var fileName = $"mav-report-{mavYear ?? DateTime.UtcNow.Year}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }
}

[ApiController]
[Route("api/v1/mav/admin/year-transition")]
[Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
public class MavYearTransitionController : MavPortalControllerBase
{
    private readonly IMavYearTransitionService _service;

    public MavYearTransitionController(IMavYearTransitionService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MavYearTransitionResultDto>>> Transition([FromBody] MavYearTransitionRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.TransitionAsync(request, cancellationToken));
}

public abstract class MavPortalControllerBase : ControllerBase
{
    protected async Task<ActionResult<ApiResponse<T>>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(ApiResponse<T>.Ok(await action()));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<T>.Fail(ex.Code, ex.Message));
        }
    }
}
