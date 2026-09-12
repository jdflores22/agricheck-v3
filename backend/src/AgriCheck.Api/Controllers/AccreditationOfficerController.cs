using AgriCheck.Application.AgencyPortal;
using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/accreditation-officer")]
[Authorize(Roles = "ROLE_ACCREDITATION_OFFICER,ROLE_ADMIN")]
public class AccreditationOfficerController : AgencyPortalControllerBase
{
    private readonly IAccreditationReviewService _service;

    public AccreditationOfficerController(IAccreditationReviewService service) => _service = service;

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AccreditationOfficerDashboardDto>>> Dashboard(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDashboardAsync(cancellationToken));

    [HttpGet("submissions")]
    public async Task<ActionResult<ApiResponse<PagedResult<AgencyAccreditationListItemDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? filter = null,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, filter, cancellationToken));

    [HttpGet("submissions/{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AgencyAccreditationDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null)
        {
            return NotFound(ApiResponse<AgencyAccreditationDetailDto>.Fail("NOT_FOUND", "Submission not found."));
        }

        return Ok(ApiResponse<AgencyAccreditationDetailDto>.Ok(item));
    }

    [HttpPost("submissions/{uuid:guid}/claim")]
    public async Task<ActionResult<ApiResponse<object>>> Claim(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.ClaimAsync(uuid, cancellationToken);
            return (object)new { claimed = true };
        });

    [HttpPost("submissions/{uuid:guid}/release")]
    public async Task<ActionResult<ApiResponse<object>>> Release(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.ReleaseAsync(uuid, cancellationToken);
            return (object)new { released = true };
        });

    [HttpPost("submissions/files/{uuid:guid}/review")]
    public async Task<ActionResult<ApiResponse<object>>> ReviewFile(Guid uuid, [FromBody] ReviewSubmissionFileRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.ReviewFileAsync(uuid, request, cancellationToken);
            return (object)new { saved = true };
        });

    [HttpGet("submissions/{submissionUuid:guid}/files/{fileUuid:guid}/download")]
    public async Task<IActionResult> DownloadFile(Guid submissionUuid, Guid fileUuid, CancellationToken cancellationToken)
    {
        try
        {
            var download = await _service.DownloadFileAsync(submissionUuid, fileUuid, cancellationToken);
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

    [HttpGet("submissions/{submissionUuid:guid}/files/{fileUuid:guid}/versions/{versionNumber:int}/download")]
    public async Task<IActionResult> DownloadFileVersion(
        Guid submissionUuid,
        Guid fileUuid,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var download = await _service.DownloadFileVersionAsync(submissionUuid, fileUuid, versionNumber, cancellationToken);
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

    [HttpPost("submissions/{uuid:guid}/complete")]
    public async Task<ActionResult<ApiResponse<CompleteAccreditationReviewResponse>>> Complete(Guid uuid, [FromBody] CompleteAccreditationReviewRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
            await _service.CompleteReviewAsync(uuid, request, cancellationToken));
}
