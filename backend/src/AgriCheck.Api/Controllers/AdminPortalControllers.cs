using System.Text;
using System.Text.Json;
using AgriCheck.Application.AdminPortal;
using AgriCheck.Application.AdminPortal.Dtos;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminDashboardController : AdminPortalControllerBase
{
    private readonly IAdminDashboardService _service;

    public AdminDashboardController(IAdminDashboardService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> Get(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDashboardAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminUsersController : AdminPortalControllerBase
{
    private readonly IAdminUserService _service;

    public AdminUsersController(IAdminUserService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminUserListItemDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<AdminUserDetailDto>.Fail("NOT_FOUND", "User not found."));
        return Ok(ApiResponse<AdminUserDetailDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminUserListItemDto>>> Create([FromBody] CreateAdminUserRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateAsync(request, cancellationToken));

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AdminUserListItemDto>>> Update(Guid uuid, [FromBody] UpdateAdminUserRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/admin/agencies")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminAgenciesController : AdminPortalControllerBase
{
    private readonly IAdminAgencyService _service;

    public AdminAgenciesController(IAdminAgencyService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminAgencyDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<AdminAgencyDetailDto>>> Get(long id, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            var agency = await _service.GetAsync(id, cancellationToken);
            return agency is null
                ? throw new ClientPortalException("NOT_FOUND", "Agency not found.")
                : agency;
        });

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminAgencyDto>>> Create([FromBody] CreateAgencyRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateAsync(request, cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<AdminAgencyDto>>> Update(long id, [FromBody] UpdateAgencyRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.DeleteAsync(id, cancellationToken);
            return (object)new { deleted = true };
        });

    [HttpPost("{id:long}/logo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<AdminAgencyDetailDto>>> UploadLogo(long id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<AdminAgencyDetailDto>.Fail("INVALID_FILE", "No file uploaded."));
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            return BadRequest(ApiResponse<AdminAgencyDetailDto>.Fail("INVALID_FILE", "File size exceeds 2MB limit."));
        }

        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.UploadLogoAsync(id, stream, file.FileName, file.ContentType, cancellationToken));
    }

    [HttpPut("{id:long}/leadership/secretary")]
    public async Task<ActionResult<ApiResponse<AdminAgencyDetailDto>>> AssignSecretary(
        long id,
        [FromBody] AssignAgencySecretaryRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.AssignSecretaryAsync(id, request, cancellationToken));

    [HttpPut("{id:long}/leadership/undersecretaries")]
    public async Task<ActionResult<ApiResponse<AdminAgencyDetailDto>>> AssignUndersecretaries(
        long id,
        [FromBody] AssignAgencyUndersecretariesRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.AssignUndersecretariesAsync(id, request, cancellationToken));
}

[ApiController]
[Route("api/v1/admin/commodities")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminCommoditiesController : AdminPortalControllerBase
{
    private readonly IAdminCommodityService _service;

    public AdminCommoditiesController(IAdminCommodityService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminCommodityDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminCommodityDto>>> Create([FromBody] CreateCommodityRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateAsync(request, cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<AdminCommodityDto>>> Update(long id, [FromBody] UpdateCommodityRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(id, request, cancellationToken));
}

[ApiController]
[Route("api/v1/admin/payment-configs")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminPaymentConfigsController : AdminPortalControllerBase
{
    private readonly IAdminPaymentConfigService _service;

    public AdminPaymentConfigsController(IAdminPaymentConfigService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProcessingFeeConfigDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProcessingFeeConfigDto>>> Upsert([FromBody] UpsertProcessingFeeRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpsertAsync(request, cancellationToken));

    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<AdminPaymentSettingsDto>>> GetSettings(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetSettingsAsync(cancellationToken));

    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<AdminPaymentSettingsDto>>> UpdateSettings([FromBody] UpdateAdminPaymentSettingsRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateSettingsAsync(request, cancellationToken));
}

[ApiController]
[Route("api/v1/admin/entry-payments")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminEntryPaymentsController : AdminPortalControllerBase
{
    private readonly IAdminEntryPaymentService _service;

    public AdminEntryPaymentsController(IAdminEntryPaymentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminEntryPaymentListItemDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, status, cancellationToken));

    [HttpGet("revenue")]
    public async Task<ActionResult<ApiResponse<AdminRevenueSummaryDto>>> Revenue(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetRevenueSummaryAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/admin/forms")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminFormsController : AdminPortalControllerBase
{
    private readonly IFormBuilderService _service;

    public AdminFormsController(IFormBuilderService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FormTemplateListItemDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<FormTemplateDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<FormTemplateDetailDto>.Fail("NOT_FOUND", "Form template not found."));
        return Ok(ApiResponse<FormTemplateDetailDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FormTemplateDetailDto>>> Create([FromBody] SaveFormTemplateRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SaveAsync(null, request, cancellationToken));

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<FormTemplateDetailDto>>> Update(Guid uuid, [FromBody] SaveFormTemplateRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SaveAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/clone")]
    public async Task<ActionResult<ApiResponse<FormTemplateDetailDto>>> Clone(Guid uuid, [FromBody] CloneFormTemplateRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CloneAsync(uuid, request, cancellationToken));

    [HttpDelete("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            await _service.DeleteAsync(uuid, cancellationToken);
            return (object)new { deleted = true };
        });

    [HttpGet("{uuid:guid}/export")]
    public async Task<IActionResult> Export(Guid uuid, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _service.ExportAsync(uuid, cancellationToken);
            var fileName = $"form-template-{uuid}.json";
            return File(Encoding.UTF8.GetBytes(json), "application/json", fileName);
        }
        catch (ClientPortalException ex) when (ex.Code == "NOT_FOUND")
        {
            return NotFound(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse<FormTemplateDetailDto>>> Import([FromBody] JsonElement payload, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ImportAsync(payload, cancellationToken));
}

[ApiController]
[Route("api/v1/admin/certificate-templates")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminCertificateTemplatesController : AdminPortalControllerBase
{
    private readonly ICertificateTemplateService _service;

    public AdminCertificateTemplatesController(ICertificateTemplateService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CertificateTemplateListItemDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<CertificateTemplateDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<CertificateTemplateDetailDto>.Fail("NOT_FOUND", "Template not found."));
        return Ok(ApiResponse<CertificateTemplateDetailDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CertificateTemplateDetailDto>>> Create([FromBody] SaveCertificateTemplateRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SaveAsync(null, request, cancellationToken));

    [HttpPut("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<CertificateTemplateDetailDto>>> Update(Guid uuid, [FromBody] SaveCertificateTemplateRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SaveAsync(uuid, request, cancellationToken));

    [HttpGet("{uuid:guid}/preview")]
    public async Task<IActionResult> Preview(Guid uuid, CancellationToken cancellationToken)
    {
        var pdf = await _service.PreviewAsync(uuid, cancellationToken);
        return File(pdf, "application/pdf", "certificate-preview.pdf");
    }

    [HttpPost("{uuid:guid}/upload-image")]
    public async Task<ActionResult<ApiResponse<object>>> UploadImage(Guid uuid, IFormFile image, CancellationToken cancellationToken)
    {
        if (image is null || image.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_FILE", "Image file is required."));
        }

        await using var stream = image.OpenReadStream();
        var path = await _service.UploadImageAsync(uuid, stream, image.FileName, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { imagePath = path }));
    }
}

[ApiController]
[Route("api/v1/admin/certificates")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminCertificatesController : AdminPortalControllerBase
{
    private readonly ICertificateIssuanceService _service;

    public AdminCertificatesController(ICertificateIssuanceService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminCertificateListItemDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, cancellationToken));

    [HttpGet("approved-entries")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminApprovedEntryDto>>>> ApprovedEntries([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListApprovedEntriesAsync(page, pageSize, cancellationToken));

    [HttpPost("issue")]
    public async Task<ActionResult<ApiResponse<AdminCertificateListItemDto>>> Issue([FromBody] IssueCertificateRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.IssueAsync(request, cancellationToken));

    [HttpPost("{uuid:guid}/revoke")]
    public async Task<ActionResult<ApiResponse<AdminCertificateListItemDto>>> Revoke(Guid uuid, [FromBody] RevokeCertificateRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.RevokeAsync(uuid, request, cancellationToken));

    [HttpGet("{uuid:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid uuid, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _service.DownloadPdfAsync(uuid, cancellationToken);
            if (bytes is null) return NotFound();
            return File(bytes, "application/pdf", $"certificate-{uuid}.pdf");
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }
}

[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminAuditLogsController : AdminPortalControllerBase
{
    private readonly IAdminAuditService _service;

    public AdminAuditLogsController(IAdminAuditService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogListItemDto>>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListAsync(page, pageSize, cancellationToken));
}

[ApiController]
[Route("api/v1/admin/roles")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminRolesController : AdminPortalControllerBase
{
    private readonly IAdminRoleService _service;

    public AdminRolesController(IAdminRoleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminRoleDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/admin/settings")]
[Authorize(Roles = "ROLE_ADMIN")]
public class AdminSettingsController : AdminPortalControllerBase
{
    private readonly IAdminSettingsService _service;

    public AdminSettingsController(IAdminSettingsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminSettingsDto>>> Get(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<ApiResponse<AdminSettingsDto>>> Update([FromBody] UpdateAdminSettingsRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateAsync(request, cancellationToken));

    [HttpPost("branding/{assetType}")]
    public async Task<ActionResult<ApiResponse<AdminSettingsDto>>> UploadBranding(string assetType, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(ApiResponse<AdminSettingsDto>.Fail("INVALID_FILE", "No file uploaded."));
        }

        await using var stream = file.OpenReadStream();
        return await ExecuteAsync(() => _service.UploadBrandingAssetAsync(assetType, stream, file.FileName, file.ContentType, cancellationToken));
    }
}

public abstract class AdminPortalControllerBase : ControllerBase
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
