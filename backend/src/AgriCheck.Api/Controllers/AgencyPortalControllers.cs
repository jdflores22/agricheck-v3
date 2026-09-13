using AgriCheck.Application.AgencyPortal;
using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/agency/dashboard")]
[Authorize(Roles = "ROLE_EVALUATOR,ROLE_BILLING_AGENT,ROLE_ACCREDITATION_OFFICER,ROLE_INSPECTOR,ROLE_AGENCY_ADMIN,ROLE_ADMIN")]
public class AgencyDashboardController : AgencyPortalControllerBase
{
    private readonly IAgencyDashboardService _service;

    public AgencyDashboardController(IAgencyDashboardService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AgencyDashboardDto>>> Get(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDashboardAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/agency/evaluator")]
[Authorize(Roles = "ROLE_EVALUATOR,ROLE_ADMIN")]
public class EvaluatorController : AgencyPortalControllerBase
{
    private readonly IEvaluatorService _service;

    public EvaluatorController(IEvaluatorService service) => _service = service;

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<PagedResult<AgencyEntryListItemDto>>>> Queue(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListQueueAsync(page, pageSize, cancellationToken));

    [HttpGet("assignments")]
    public async Task<ActionResult<ApiResponse<PagedResult<AgencyEntryListItemDto>>>> Assignments(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListMyAssignmentsAsync(page, pageSize, cancellationToken));

    [HttpPost("entries/{uuid:guid}/assign")]
    public async Task<ActionResult<ApiResponse<object>>> Assign(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.AssignToSelfAsync(uuid, cancellationToken);
            return (object)new { assigned = true };
        });

    [HttpGet("entries/{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AgencyEntryEvaluationDto>>> GetEntry(Guid uuid, CancellationToken cancellationToken)
    {
        var entry = await _service.GetEntryAsync(uuid, cancellationToken);
        if (entry is null) return NotFound(ApiResponse<AgencyEntryEvaluationDto>.Fail("ENTRY_NOT_FOUND", "Entry not found."));
        return Ok(ApiResponse<AgencyEntryEvaluationDto>.Ok(entry));
    }

    [HttpPost("files/{uuid:guid}/evaluate")]
    public async Task<ActionResult<ApiResponse<object>>> EvaluateFile(Guid uuid, [FromBody] EvaluateFileRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.EvaluateFileAsync(uuid, request, cancellationToken);
            return (object)new { saved = true };
        });

    [HttpPost("entries/{uuid:guid}/mav/evaluate")]
    public async Task<ActionResult<ApiResponse<object>>> EvaluateMav(Guid uuid, [FromBody] EvaluateEntryMavRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.EvaluateMavDocumentAsync(uuid, request, cancellationToken);
            return (object)new { saved = true };
        });

    [HttpPut("entries/{uuid:guid}/compliance")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCompliance(Guid uuid, [FromBody] UpdateComplianceRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.UpdateComplianceAsync(uuid, request, cancellationToken);
            return (object)new { saved = true };
        });

    [HttpPost("entries/{uuid:guid}/notes")]
    public async Task<ActionResult<ApiResponse<object>>> AddNote(Guid uuid, [FromBody] AddEvaluatorNoteRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.AddNoteAsync(uuid, request, cancellationToken);
            return (object)new { saved = true };
        });

    [HttpPost("entries/{uuid:guid}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> Complete(Guid uuid, [FromBody] CompleteEvaluationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.CompleteEvaluationAsync(uuid, request, cancellationToken);
            return (object)new { completed = true };
        });

    [HttpGet("entries/{entryUuid:guid}/files/{fileUuid:guid}/download")]
    public async Task<IActionResult> DownloadFile(Guid entryUuid, Guid fileUuid, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _service.DownloadFileAsync(entryUuid, fileUuid, cancellationToken);
            return PhysicalFile(download.PhysicalPath, download.ContentType, download.DownloadFileName);
        }
        catch (ClientPortalException ex) when (ex.Code == "FILE_NOT_FOUND")
        {
            return NotFound(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpGet("entries/{entryUuid:guid}/files/{fileUuid:guid}/versions/{versionNumber:int}/download")]
    public async Task<IActionResult> DownloadFileVersion(
        Guid entryUuid,
        Guid fileUuid,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var download = await _service.DownloadFileVersionAsync(entryUuid, fileUuid, versionNumber, cancellationToken);
            return PhysicalFile(download.PhysicalPath, download.ContentType, download.DownloadFileName);
        }
        catch (ClientPortalException ex) when (ex.Code is "FILE_NOT_FOUND" or "VERSION_NOT_FOUND")
        {
            return NotFound(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }
}

[ApiController]
[Route("api/v1/agency/entries")]
[Authorize(Roles = "ROLE_EVALUATOR,ROLE_INSPECTOR,ROLE_BILLING_AGENT,ROLE_ACCOUNTANT,ROLE_ADMIN")]
public class AgencyEntriesController : AgencyPortalControllerBase
{
    private readonly IEvaluatorService _service;

    public AgencyEntriesController(IEvaluatorService service) => _service = service;

    [HttpGet("approved")]
    public async Task<ActionResult<ApiResponse<PagedResult<AgencyEntryListItemDto>>>> Approved(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListApprovedEntriesAsync(page, pageSize, cancellationToken));
}

[ApiController]
[Route("api/v1/agency/inspections")]
[Authorize(Roles = "ROLE_INSPECTOR,ROLE_DOCTOR,ROLE_EVALUATOR,ROLE_ADMIN")]
public class InspectionsController : AgencyPortalControllerBase
{
    private readonly IInspectionService _service;

    public InspectionsController(IInspectionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<InspectionListItemDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, status, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<InspectionDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<InspectionDto>.Fail("NOT_FOUND", "Inspection not found."));
        return Ok(ApiResponse<InspectionDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InspectionDto>>> Create([FromBody] CreateInspectionRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateAsync(request, cancellationToken));

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<InspectionDto>>> Update(Guid uuid, [FromBody] UpdateInspectionRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/complete")]
    public async Task<ActionResult<ApiResponse<InspectionDto>>> Complete(Guid uuid, [FromBody] CompleteInspectionRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CompleteAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/photos")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<InspectionPhotoDto>>> UploadPhoto(Guid uuid, IFormFile file, [FromForm] string? caption, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest(ApiResponse<InspectionPhotoDto>.Fail("EMPTY_FILE", "File is empty."));
        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.UploadPhotoAsync(uuid, stream, file.FileName, file.ContentType, caption, cancellationToken));
    }
}

[ApiController]
[Route("api/v1/agency/billings")]
[Authorize(Roles = "ROLE_BILLING_AGENT,ROLE_ACCOUNTANT,ROLE_ADMIN")]
public class AgencyBillingsController : AgencyPortalControllerBase
{
    private readonly IAgencyBillingService _service;
    private readonly IEvaluatorService _evaluatorService;

    public AgencyBillingsController(IAgencyBillingService service, IEvaluatorService evaluatorService)
    {
        _service = service;
        _evaluatorService = evaluatorService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AgencyBillingListItemDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AgencyBillingDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetAsync(uuid, cancellationToken));

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AgencyBillingListItemDto>>> Update(
        Guid uuid,
        [FromBody] UpdateAgencyBillingRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(uuid, request, cancellationToken));

    [HttpGet("awaiting-entries")]
    public async Task<ActionResult<ApiResponse<PagedResult<AgencyEntryListItemDto>>>> AwaitingEntries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _evaluatorService.ListEntriesAwaitingBillingAsync(page, pageSize, cancellationToken));

    [HttpGet("awaiting-entries/{entryUuid:guid}")]
    public async Task<ActionResult<ApiResponse<AgencyBillingEntryContextDto>>> AwaitingEntryContext(
        Guid entryUuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetAwaitingEntryContextAsync(entryUuid, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AgencyBillingListItemDto>>> Create([FromBody] CreateAgencyBillingRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateAsync(request, cancellationToken));

    [HttpPost("{uuid:guid}/issue")]
    public async Task<ActionResult<ApiResponse<AgencyBillingListItemDto>>> Issue(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.IssueAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/pay")]
    public async Task<ActionResult<ApiResponse<AgencyBillingListItemDto>>> MarkPaid(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.MarkPaidAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/verify-payment")]
    public async Task<ActionResult<ApiResponse<AgencyBillingListItemDto>>> VerifyPayment(
        Guid uuid,
        [FromBody] VerifyDaBillingPaymentRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.VerifyPaymentAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/agency/accreditation/submissions")]
[Authorize(Roles = "ROLE_ACCREDITATION_OFFICER,ROLE_ADMIN")]
public class AgencyAccreditationController : AgencyPortalControllerBase
{
    private readonly IAccreditationReviewService _service;

    public AgencyAccreditationController(IAccreditationReviewService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AgencyAccreditationListItemDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, filter, cancellationToken));

    [HttpPost("{uuid:guid}/claim")]
    public async Task<ActionResult<ApiResponse<object>>> Claim(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.ClaimAsync(uuid, cancellationToken);
            return (object)new { claimed = true };
        });

    [HttpPost("{uuid:guid}/release")]
    public async Task<ActionResult<ApiResponse<object>>> Release(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.ReleaseAsync(uuid, cancellationToken);
            return (object)new { released = true };
        });

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AgencyAccreditationDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<AgencyAccreditationDetailDto>.Fail("NOT_FOUND", "Submission not found."));
        return Ok(ApiResponse<AgencyAccreditationDetailDto>.Ok(item));
    }

    [HttpPost("files/{uuid:guid}/review")]
    public async Task<ActionResult<ApiResponse<object>>> ReviewFile(Guid uuid, [FromBody] ReviewSubmissionFileRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.ReviewFileAsync(uuid, request, cancellationToken);
            return (object)new { saved = true };
        });

    [HttpPost("{uuid:guid}/complete")]
    public async Task<ActionResult<ApiResponse<CompleteAccreditationReviewResponse>>> Complete(Guid uuid, [FromBody] CompleteAccreditationReviewRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
            await _service.CompleteReviewAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/agency/reports")]
public class AgencyReportsController : AgencyPortalControllerBase
{
    private readonly ISecretaryReportService _secretaryService;
    private readonly IAgencyBillingReportService _billingReportService;

    public AgencyReportsController(
        ISecretaryReportService secretaryService,
        IAgencyBillingReportService billingReportService)
    {
        _secretaryService = secretaryService;
        _billingReportService = billingReportService;
    }

    [HttpGet("summary")]
    [Authorize(Roles = "ROLE_EVALUATOR,ROLE_BILLING_AGENT,ROLE_SECRETARY,ROLE_UNDERSECRETARY,ROLE_ACCREDITATION_OFFICER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<SecretaryReportDto>>> Summary(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _secretaryService.GetReportAsync(cancellationToken));

    [HttpGet("billing-revenue")]
    [Authorize(Roles = "ROLE_BILLING_AGENT,ROLE_ACCOUNTANT,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<AgencyBillingRevenueReportDto>>> BillingRevenue(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _billingReportService.GetRevenueReportAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/agency/payment-config")]
[Authorize(Roles = "ROLE_AGENCY_ADMIN,ROLE_BILLING_AGENT,ROLE_ACCOUNTANT,ROLE_ADMIN")]
public class AgencyPaymentConfigController : AgencyPortalControllerBase
{
    private readonly IAgencyPaymentConfigService _service;

    public AgencyPaymentConfigController(IAgencyPaymentConfigService service) => _service = service;

    [HttpGet("settings")]
    [Authorize(Roles = "ROLE_AGENCY_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<AgencyPaymentSettingsDto>>> GetSettings(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetSettingsAsync(cancellationToken));

    [HttpPut("settings")]
    [Authorize(Roles = "ROLE_AGENCY_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<AgencyPaymentSettingsDto>>> UpdateSettings(
        [FromBody] UpdateAgencyPaymentSettingsRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateSettingsAsync(request, cancellationToken));

    [HttpGet("pending-cash")]
    [Authorize(Roles = "ROLE_BILLING_AGENT,ROLE_ACCOUNTANT,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgencyPendingCashPaymentDto>>>> ListPendingCash(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListPendingCashPaymentsAsync(cancellationToken));

    [HttpPost("pending-cash/{billUuid:guid}/verify")]
    [Authorize(Roles = "ROLE_BILLING_AGENT,ROLE_ACCOUNTANT,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyCash(
        Guid billUuid,
        [FromBody] VerifyAgencyCashPaymentRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.VerifyCashPaymentAsync(billUuid, request, cancellationToken);
            return (object)new { verified = true };
        });
}

public abstract class AgencyPortalControllerBase : ControllerBase
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
