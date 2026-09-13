using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Application.Payments;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/client/dashboard")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class ClientDashboardController : ClientPortalControllerBase
{
    private readonly IClientDashboardService _service;

    public ClientDashboardController(IClientDashboardService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ClientDashboardDto>>> Get(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDashboardAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/commodities")]
[Authorize]
public class CommoditiesController : ClientPortalControllerBase
{
    private readonly ICommodityService _service;

    public CommoditiesController(ICommodityService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CommodityDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(cancellationToken));

    [HttpGet("agencies")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgencyOptionDto>>>> Agencies(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAgenciesAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/entries")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class EntriesController : ClientPortalControllerBase
{
    private readonly IEntryService _service;

    public EntriesController(IEntryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EntryListItemDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, status, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<EntryDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var entry = await _service.GetAsync(uuid, cancellationToken);
        if (entry is null) return NotFound(ApiResponse<EntryDto>.Fail("ENTRY_NOT_FOUND", "Entry not found."));
        return Ok(ApiResponse<EntryDto>.Ok(entry));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EntryDto>>> Create([FromBody] CreateEntryRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateAsync(request, cancellationToken));

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<EntryDto>>> Update(Guid uuid, [FromBody] UpdateEntryRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/submit")]
    public async Task<ActionResult<ApiResponse<EntryDto>>> Submit(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SubmitAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/files")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<EntryFileDto>>> Upload(Guid uuid, IFormFile file, [FromForm] string? documentType, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest(ApiResponse<EntryFileDto>.Fail("EMPTY_FILE", "File is empty."));
        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.UploadFileAsync(uuid, stream, file.FileName, file.ContentType, documentType, cancellationToken));
    }

    [HttpPost("{uuid:guid}/files/{fileUuid:guid}/compliance")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<EntryFileDto>>> UploadCompliance(Guid uuid, Guid fileUuid, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest(ApiResponse<EntryFileDto>.Fail("EMPTY_FILE", "File is empty."));
        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.UploadComplianceFileAsync(uuid, fileUuid, stream, file.FileName, file.ContentType, cancellationToken));
    }

    [HttpPost("{uuid:guid}/compliance/resubmit")]
    public async Task<ActionResult<ApiResponse<EntryDto>>> ResubmitCompliance(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ResubmitComplianceAsync(uuid, cancellationToken));

    [HttpGet("check-mav-no")]
    public async Task<ActionResult<ApiResponse<CheckMavNoResultDto>>> CheckMavNo(
        [FromQuery] string mavNo,
        [FromQuery] Guid? excludeUuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CheckMavNoAsync(mavNo, excludeUuid, cancellationToken));

    [HttpPut("{uuid:guid}/mav")]
    public async Task<ActionResult<ApiResponse<EntryDto>>> UpdateMav(Guid uuid, [FromBody] UpdateEntryMavRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateMavNoAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/mav/utilize")]
    public async Task<ActionResult<ApiResponse<EntryDto>>> UtilizeMic(Guid uuid, [FromBody] UtilizeEntryMicRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UtilizeMicAsync(uuid, request, cancellationToken));

    [HttpGet("{uuid:guid}/files/{fileUuid:guid}/download")]
    public async Task<IActionResult> Download(Guid uuid, Guid fileUuid, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _service.DownloadFileAsync(uuid, fileUuid, cancellationToken);
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
}

[ApiController]
[Route("api/v1/accreditation/submissions")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class AccreditationSubmissionsController : ClientPortalControllerBase
{
    private readonly IAccreditationService _service;

    public AccreditationSubmissionsController(IAccreditationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AccreditationListItemDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AccreditationSubmissionDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<AccreditationSubmissionDto>.Fail("NOT_FOUND", "Submission not found."));
        return Ok(ApiResponse<AccreditationSubmissionDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccreditationSubmissionDto>>> Create([FromBody] CreateAccreditationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateAsync(request, cancellationToken));

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AccreditationSubmissionDto>>> Update(Guid uuid, [FromBody] UpdateAccreditationRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/submit")]
    public async Task<ActionResult<ApiResponse<AccreditationSubmissionDto>>> Submit(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SubmitAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/files")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<SubmissionFileDto>>> UploadFile(Guid uuid, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest(ApiResponse<SubmissionFileDto>.Fail("EMPTY_FILE", "File is empty."));
        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.UploadFileAsync(uuid, stream, file.FileName, file.ContentType, cancellationToken));
    }

    [HttpPost("{uuid:guid}/files/{fileUuid:guid}/compliance")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<SubmissionFileDto>>> UploadComplianceFile(Guid uuid, Guid fileUuid, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest(ApiResponse<SubmissionFileDto>.Fail("EMPTY_FILE", "File is empty."));
        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.UploadComplianceFileAsync(uuid, fileUuid, stream, file.FileName, file.ContentType, cancellationToken));
    }

    [HttpPost("{uuid:guid}/compliance/resubmit")]
    public async Task<ActionResult<ApiResponse<AccreditationSubmissionDto>>> ResubmitCompliance(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ResubmitComplianceAsync(uuid, cancellationToken));

    [HttpGet("{uuid:guid}/files/{fileUuid:guid}/download")]
    public async Task<IActionResult> DownloadFile(Guid uuid, Guid fileUuid, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _service.DownloadFileAsync(uuid, fileUuid, cancellationToken);
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

    [HttpGet("{uuid:guid}/files/{fileUuid:guid}/versions/{versionNumber:int}/download")]
    public async Task<IActionResult> DownloadFileVersion(Guid uuid, Guid fileUuid, int versionNumber, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _service.DownloadFileVersionAsync(uuid, fileUuid, versionNumber, cancellationToken);
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
[Route("api/v1/certificates")]
public class CertificatesController : ClientPortalControllerBase
{
    private readonly ICertificateService _service;

    public CertificatesController(ICertificateService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<PagedResult<CertificateListItemDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, cancellationToken));

    [HttpGet("{uuid:guid}")]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<CertificateDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<CertificateDto>.Fail("NOT_FOUND", "Certificate not found."));
        return Ok(ApiResponse<CertificateDto>.Ok(item));
    }

    [HttpGet("{uuid:guid}/pdf")]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<IActionResult> DownloadPdf(Guid uuid, CancellationToken cancellationToken)
    {
        var download = await _service.GetPdfAsync(uuid, cancellationToken);
        if (download is null) return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Certificate PDF not found."));
        return PhysicalFile(download.PhysicalPath, download.ContentType, download.DownloadFileName);
    }

    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CertificateVerifyDto>>> Verify(string code, CancellationToken cancellationToken)
    {
        var item = await _service.VerifyAsync(code, cancellationToken);
        if (item is null) return NotFound(ApiResponse<CertificateVerifyDto>.Fail("NOT_FOUND", "Certificate not found."));
        return Ok(ApiResponse<CertificateVerifyDto>.Ok(item));
    }
}

[ApiController]
[Route("api/v1/warehouse")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class WarehouseBookingsController : ClientPortalControllerBase
{
    private readonly IWarehouseBookingService _service;

    public WarehouseBookingsController(IWarehouseBookingService service) => _service = service;

    [HttpGet("facilities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WarehouseFacilityDto>>>> Facilities(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListFacilitiesAsync(cancellationToken));

    [HttpGet("bookings")]
    public async Task<ActionResult<ApiResponse<PagedResult<WarehouseBookingListItemDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListBookingsAsync(page, pageSize, cancellationToken));

    [HttpGet("bookings/{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseBookingDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetBookingAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<WarehouseBookingDto>.Fail("NOT_FOUND", "Booking not found."));
        return Ok(ApiResponse<WarehouseBookingDto>.Ok(item));
    }

    [HttpPost("bookings")]
    public async Task<ActionResult<ApiResponse<WarehouseBookingDto>>> Create([FromBody] CreateWarehouseBookingRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateBookingAsync(request, cancellationToken));

    [HttpPost("bookings/{uuid:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync<object>(async () => { await _service.CancelBookingAsync(uuid, cancellationToken); return new { message = "Booking cancelled." }; });
}

[ApiController]
[Route("api/v1/client/bills")]
public class ClientBillsController : ClientPortalControllerBase
{
    private readonly IClientBillService _service;

    public ClientBillsController(IClientBillService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientBillSummaryDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListForUserAsync(cancellationToken));

    [HttpGet("{uuid:guid}")]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<ClientBillDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<ClientBillDto>.Fail("NOT_FOUND", "Bill not found."));
        return Ok(ApiResponse<ClientBillDto>.Ok(item));
    }

    [HttpGet("pay/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientBillDto>>> GetByToken(string token, CancellationToken cancellationToken)
    {
        var item = await _service.GetByPaymentTokenAsync(token, cancellationToken);
        if (item is null) return NotFound(ApiResponse<ClientBillDto>.Fail("NOT_FOUND", "Payment link not found."));
        return Ok(ApiResponse<ClientBillDto>.Ok(item));
    }

    [HttpPost("{uuid:guid}/pay")]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<ClientBillDto>>> Pay(Guid uuid, [FromBody] PayBillRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.PayAsync(uuid, request, cancellationToken));

    [HttpGet("{uuid:guid}/payment-options")]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<BillPaymentOptionsDto>>> PaymentOptions(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetPaymentOptionsAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/initiate")]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<InitiateBillPaymentResultDto>>> Initiate(Guid uuid, [FromBody] PayBillRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.InitiatePaymentAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/confirm-payment")]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<ClientBillDto>>> ConfirmPayment(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ConfirmPaymentAsync(uuid, cancellationToken));

    [HttpPost("pay/{token}/initiate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<InitiateBillPaymentResultDto>>> InitiateByToken(string token, [FromBody] PayBillRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.InitiatePaymentByTokenAsync(token, request, cancellationToken));

    [HttpPost("pay/{token}/confirm-payment")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientBillDto>>> ConfirmPaymentByToken(string token, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ConfirmPaymentByTokenAsync(token, cancellationToken));

    [HttpPost("pay/{token}/complete")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientBillDto>>> PayByToken(string token, [FromBody] PayBillRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.PayByTokenAsync(token, request, cancellationToken));

    [HttpGet("history/payments")]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientPaymentHistoryItemDto>>>> PaymentHistory(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListPaymentHistoryAsync(cancellationToken));

    [HttpGet("{uuid:guid}/receipt")]
    [Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
    public async Task<IActionResult> Receipt(Guid uuid, CancellationToken cancellationToken)
    {
        var pdf = await _service.GetReceiptPdfAsync(uuid, cancellationToken);
        if (pdf is null) return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Receipt not available."));
        return File(pdf, "application/pdf", $"receipt-{uuid}.pdf");
    }
}

[ApiController]
[Route("api/v1/client/forms")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class ClientFormsController : ClientPortalControllerBase
{
    private readonly IClientFormService _service;

    public ClientFormsController(IClientFormService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientFormListItemDto>>>> List([FromQuery] long? agencyId, [FromQuery] string formType = "ENTRY", CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListPublishedAsync(agencyId, formType, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<ClientFormDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetPublishedAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<ClientFormDetailDto>.Fail("NOT_FOUND", "Form template not found."));
        return Ok(ApiResponse<ClientFormDetailDto>.Ok(item));
    }
}

[ApiController]
[Route("api/v1/client/profile")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class ClientProfileController : ClientPortalControllerBase
{
    private readonly IClientProfileService _service;

    public ClientProfileController(IClientProfileService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ClientProfileDto>>> Get(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<ApiResponse<ClientProfileDto>>> Update([FromBody] UpdateClientProfileRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(request, cancellationToken));
}

[ApiController]
[Route("api/v1/client/containers")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class ClientContainersController : ClientPortalControllerBase
{
    private readonly IClientContainerService _service;

    public ClientContainersController(IClientContainerService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientContainerListItemDto>>>> List([FromQuery] string? status, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(status, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<ClientContainerDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<ClientContainerDetailDto>.Fail("NOT_FOUND", "Container not found."));
        return Ok(ApiResponse<ClientContainerDetailDto>.Ok(item));
    }
}

[ApiController]
[Route("api/v1/client/inspections")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class ClientInspectionsController : ClientPortalControllerBase
{
    private readonly IClientInspectionService _service;

    public ClientInspectionsController(IClientInspectionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientInspectionListItemDto>>>> List([FromQuery] string? status, [FromQuery] Guid? entryUuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(status, entryUuid, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<ClientInspectionDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<ClientInspectionDetailDto>.Fail("NOT_FOUND", "Inspection not found."));
        return Ok(ApiResponse<ClientInspectionDetailDto>.Ok(item));
    }
}

[ApiController]
[Route("api/v1/client/entries/{entryUuid:guid}/da-billings")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class ClientDaBillingsController : ClientPortalControllerBase
{
    private readonly IClientDaBillingService _service;

    public ClientDaBillingsController(IClientDaBillingService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientDaBillingDto>>>> List(Guid entryUuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListForEntryAsync(entryUuid, cancellationToken));

    [HttpPost("{billingUuid:guid}/payment-proof")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<ClientDaBillingDto>>> UploadPaymentProof(
        Guid entryUuid,
        Guid billingUuid,
        IFormFile file,
        [FromForm] string paymentReference,
        [FromForm] string? notes,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<ClientDaBillingDto>.Fail("FILE_REQUIRED", "Payment proof file is required."));
        }

        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.UploadPaymentProofAsync(
            billingUuid,
            stream,
            file.FileName,
            file.ContentType,
            new UploadDaBillingPaymentRequest(paymentReference, notes),
            cancellationToken));
    }
}

[ApiController]
[Route("api/v1/client/entries/{entryUuid:guid}/container-inspections")]
[Authorize(Roles = "ROLE_IMPORTER,ROLE_EXPORTER,ROLE_BROKER,ROLE_ADMIN")]
public class ClientContainerInspectionsController : ClientPortalControllerBase
{
    private readonly IContainerInspectionWorkflowService _service;

    public ClientContainerInspectionsController(IContainerInspectionWorkflowService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientContainerInspectionStatusDto>>>> List(
        Guid entryUuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListForEntryAsync(entryUuid, cancellationToken));

    [HttpPost("containers/{containerUuid:guid}/photos/{photoType}")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<ApiResponse<ClientContainerInspectionPhotoDto>>> UploadPhoto(
        Guid entryUuid,
        Guid containerUuid,
        string photoType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<ClientContainerInspectionPhotoDto>.Fail("FILE_REQUIRED", "Photo file is required."));
        }

        if (!Enum.TryParse<ContainerInspectionPhotoType>(photoType, true, out var parsed))
        {
            return BadRequest(ApiResponse<ClientContainerInspectionPhotoDto>.Fail("INVALID_PHOTO_TYPE", "Invalid photo type."));
        }

        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.UploadPhotoAsync(
            containerUuid, parsed, stream, file.FileName, file.ContentType, cancellationToken));
    }

    [HttpGet("photos/{photoUuid:guid}/download")]
    public async Task<IActionResult> DownloadPhoto(Guid entryUuid, Guid photoUuid, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _service.DownloadPhotoForClientAsync(entryUuid, photoUuid, cancellationToken);
            return PhysicalFile(download.PhysicalPath, download.ContentType, download.DownloadFileName);
        }
        catch (ClientPortalException ex) when (ex.Code == "NOT_FOUND")
        {
            return NotFound(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }
}

[ApiController]
[Route("api/v1/agency/workflow")]
[Authorize(Roles = "ROLE_EVALUATOR,ROLE_INSPECTOR,ROLE_DOCTOR,ROLE_ADMIN")]
public class AgencyWorkflowController : AgencyPortalControllerBase
{
    private readonly IContainerInspectionWorkflowService _inspectionService;
    private readonly ITransportTagService _transportTagService;

    public AgencyWorkflowController(
        IContainerInspectionWorkflowService inspectionService,
        ITransportTagService transportTagService)
    {
        _inspectionService = inspectionService;
        _transportTagService = transportTagService;
    }

    [HttpGet("container-inspections")]
    [Authorize(Roles = "ROLE_INSPECTOR,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<PagedResult<AgencyContainerInspectionQueueItemDto>>>> ListContainerInspectionQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string scope = "unclaimed",
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _inspectionService.ListInspectionQueueForAgencyAsync(page, pageSize, scope, cancellationToken));

    [HttpPost("containers/{containerUuid:guid}/claim")]
    [Authorize(Roles = "ROLE_INSPECTOR,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<AgencyContainerInspectionDetailDto>>> ClaimContainerInspection(
        Guid containerUuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _inspectionService.ClaimContainerInspectionAsync(containerUuid, cancellationToken));

    [HttpGet("entries/{entryUuid:guid}/container-inspections")]
    [Authorize(Roles = "ROLE_INSPECTOR,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClientContainerInspectionStatusDto>>>> ListEntryContainerInspections(
        Guid entryUuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _inspectionService.ListForAgencyEntryAsync(entryUuid, cancellationToken));

    [HttpGet("containers/{containerUuid:guid}")]
    [Authorize(Roles = "ROLE_INSPECTOR,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<AgencyContainerInspectionDetailDto>>> GetContainerInspectionDetail(
        Guid containerUuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _inspectionService.GetContainerInspectionDetailAsync(containerUuid, cancellationToken));

    [HttpGet("entries/{entryUuid:guid}/certificate/pdf")]
    public async Task<IActionResult> DownloadEntryCertificate(Guid entryUuid, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _inspectionService.GetEntryCertificatePdfForAgencyAsync(entryUuid, cancellationToken);
            if (download is null)
            {
                return NotFound(ApiResponse<object>.Fail("NOT_FOUND", "Entry certificate PDF not found."));
            }

            return PhysicalFile(download.PhysicalPath, download.ContentType, download.DownloadFileName);
        }
        catch (ClientPortalException ex) when (ex.Code == "NOT_FOUND")
        {
            return NotFound(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpPost("inspection-photos/{photoUuid:guid}/review")]
    [Authorize(Roles = "ROLE_INSPECTOR,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<ClientContainerInspectionPhotoDto>>> ReviewPhoto(
        Guid photoUuid,
        [FromBody] ReviewContainerInspectionPhotoRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _inspectionService.ReviewPhotoAsync(photoUuid, request, cancellationToken));

    [HttpPost("containers/{containerUuid:guid}/complete")]
    [Authorize(Roles = "ROLE_INSPECTOR,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<AgencyContainerInspectionDetailDto>>> CompleteContainerInspection(
        Guid containerUuid,
        [FromBody] CompleteContainerInspectionRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _inspectionService.CompleteContainerInspectionAsync(containerUuid, request, cancellationToken));

    [HttpGet("inspection-photos/{photoUuid:guid}/download")]
    public async Task<IActionResult> DownloadInspectionPhoto(Guid photoUuid, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _inspectionService.DownloadPhotoForAgencyAsync(photoUuid, cancellationToken);
            return PhysicalFile(download.PhysicalPath, download.ContentType, download.DownloadFileName);
        }
        catch (ClientPortalException ex) when (ex.Code == "NOT_FOUND")
        {
            return NotFound(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpGet("transport-tags/ready")]
    public async Task<ActionResult<ApiResponse<TransportTagQueuesDto>>> ListTransportTagQueues(
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _transportTagService.ListTransportTagQueuesAsync(cancellationToken));

    [HttpGet("transport-tags/containers/{containerUuid:guid}")]
    public async Task<ActionResult<ApiResponse<TransportTagContainerDetailDto>>> GetTransportTagContainerDetail(
        Guid containerUuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _transportTagService.GetTransportTagContainerDetailAsync(containerUuid, cancellationToken));

    [HttpPost("transport-tags")]
    public async Task<ActionResult<ApiResponse<AddTransportTagResultDto>>> AddTransportTag(
        [FromBody] AddTransportTagRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _transportTagService.AddTransportTagAsync(request, cancellationToken));

    [HttpGet("transport-tags/{tagUuid:guid}")]
    public async Task<ActionResult<ApiResponse<AddTransportTagResultDto>>> GetTransportTag(
        Guid tagUuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _transportTagService.GetTransportTagAsync(tagUuid, cancellationToken));
}

[ApiController]
[Route("api/v1/ops/operator/containers")]
[Authorize(Roles = "ROLE_OPERATOR,ROLE_ADMIN")]
public class OperatorContainersController : OpsPortalControllerBase
{
    private readonly IOperatorOpsService _service;

    public OperatorContainersController(IOperatorOpsService service) => _service = service;

    [HttpGet("claimable")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContainerListItemDto>>>> ListClaimable(
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListClaimableContainersAsync(cancellationToken));

    [HttpPost("{uuid:guid}/claim")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> Claim(
        Guid uuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ClaimContainerAsync(uuid, cancellationToken));

    [HttpPost("scan")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> ScanAndClaim(
        [FromBody] ScanTransportQrRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ClaimContainerByQrAsync(request, cancellationToken));

    [HttpPost("{uuid:guid}/assign-driver")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> AssignDriver(
        Guid uuid,
        [FromBody] AssignDriverRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.AssignDriverAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/ops/operator/invite-codes")]
[Authorize(Roles = "ROLE_OPERATOR,ROLE_ADMIN")]
public class OperatorInviteCodesController : OpsPortalControllerBase
{
    private readonly IOperatorOpsService _service;

    public OperatorInviteCodesController(IOperatorOpsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OperatorInviteCodeListItemDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListInviteCodesAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OperatorInviteCodeListItemDto>>> Create(
        [FromBody] CreateOperatorInviteCodeRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateInviteCodeAsync(request, cancellationToken));
}

[ApiController]
[Route("api/v1/ops/doctor/containers")]
[Authorize(Roles = "ROLE_DOCTOR,ROLE_INSPECTOR,ROLE_ADMIN")]
public class DoctorContainersController : OpsPortalControllerBase
{
    private readonly IDoctorInspectionService _service;

    public DoctorContainersController(IDoctorInspectionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContainerListItemDto>>>> List(
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListInspectableContainersAsync(cancellationToken));

    [HttpPost("{uuid:guid}/claim")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> Claim(
        Guid uuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ClaimContainerAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/complete")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> Complete(
        Guid uuid,
        [FromBody] CompleteDoctorInspectionRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CompleteInspectionAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/webhooks/paymongo")]
[AllowAnonymous]
public class PayMongoWebhookController : ControllerBase
{
    private readonly IClientBillService _billService;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly ILogger<PayMongoWebhookController> _logger;

    public PayMongoWebhookController(
        IClientBillService billService,
        IPaymentGatewayService paymentGateway,
        ILogger<PayMongoWebhookController> logger)
    {
        _billService = billService;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Paymongo-Signature"].FirstOrDefault()
            ?? Request.Headers["X-PayMongo-Signature"].FirstOrDefault()
            ?? string.Empty;

        if (!await _paymentGateway.VerifyWebhookSignatureAsync(payload, signature, cancellationToken))
        {
            _logger.LogWarning("PayMongo webhook signature verification failed");
            return Unauthorized();
        }

        var parsed = await _paymentGateway.ParseWebhookPayloadAsync(payload, cancellationToken);
        if (parsed is null)
        {
            _logger.LogInformation("PayMongo webhook ignored: {Payload}", payload);
            return Ok(new { received = true });
        }

        if (parsed.Value.Status is "succeeded" or "paid")
        {
            try
            {
                await _billService.CompletePaymentByReferenceAsync(parsed.Value.PaymentReference, null, cancellationToken);
            }
            catch (ClientPortalException ex) when (ex.Code is "NOT_FOUND" or "ALREADY_PAID")
            {
                _logger.LogInformation("PayMongo webhook skipped {Code}: {Message}", ex.Code, ex.Message);
            }
        }

        return Ok(new { received = true });
    }
}

public abstract class ClientPortalControllerBase : ControllerBase
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
