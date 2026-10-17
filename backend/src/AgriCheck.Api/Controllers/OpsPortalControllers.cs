using AgriCheck.Application.Auth;
using AgriCheck.Application.Auth.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Application.Compliance;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/ops/warehouse/dashboard")]
[Authorize(Roles = "ROLE_WAREHOUSE_STAFF,ROLE_ADMIN")]
public class WarehouseDashboardController : OpsPortalControllerBase
{
    private readonly IWarehouseOpsService _service;

    public WarehouseDashboardController(IWarehouseOpsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<WarehouseDashboardDto>>> Get(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDashboardAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/ops/warehouse/facilities")]
[Authorize(Roles = "ROLE_WAREHOUSE_STAFF,ROLE_ADMIN")]
public class WarehouseFacilitiesController : OpsPortalControllerBase
{
    private readonly IWarehouseOpsService _service;

    public WarehouseFacilitiesController(IWarehouseOpsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WarehouseFacilityDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListFacilitiesAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/ops/warehouse/inventory")]
[Authorize(Roles = "ROLE_WAREHOUSE_STAFF,ROLE_ADMIN")]
public class WarehouseInventoryController : OpsPortalControllerBase
{
    private readonly IWarehouseOpsService _service;

    public WarehouseInventoryController(IWarehouseOpsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<WarehouseInventoryListItemDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(() => _service.ListInventoryAsync(page, pageSize, cancellationToken));

    [HttpGet("receivable")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContainerListItemDto>>>> ListReceivable(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListReceivableContainersAsync(cancellationToken));

    [HttpPost("receive")]
    public async Task<ActionResult<ApiResponse<WarehouseInventoryListItemDto>>> Receive(
        [FromBody] ReceiveContainerRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ReceiveContainerAsync(request, cancellationToken));
}

[ApiController]
[Route("api/v1/ops/warehouse/release-authorizations")]
[Authorize(Roles = "ROLE_WAREHOUSE_STAFF,ROLE_ADMIN")]
public class ReleaseAuthorizationController : OpsPortalControllerBase
{
    private readonly IWarehouseOpsService _service;

    public ReleaseAuthorizationController(IWarehouseOpsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReleaseAuthorizationListItemDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListReleaseAuthorizationsAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReleaseAuthorizationListItemDto>>> Create(
        [FromBody] CreateReleaseAuthorizationRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateReleaseAuthorizationAsync(request, cancellationToken));
}

[ApiController]
[Route("api/v1/ops/warehouse/compliance")]
[Authorize(Roles = "ROLE_WAREHOUSE_STAFF,ROLE_ADMIN")]
public class WarehouseComplianceController : OpsPortalControllerBase
{
    private readonly IWarehouseComplianceService _service;

    public WarehouseComplianceController(IWarehouseComplianceService service) => _service = service;

    [HttpGet("facilities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WarehouseComplianceFacilityListItemDto>>>> ListFacilities(
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListFacilitiesAsync(cancellationToken));

    [HttpGet("facilities/{warehouseFacilityId:long}")]
    public async Task<ActionResult<ApiResponse<WarehouseComplianceDetailDto>>> GetDetail(
        long warehouseFacilityId,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDetailAsync(warehouseFacilityId, cancellationToken));

    [HttpPost("lto-applications")]
    public async Task<ActionResult<ApiResponse<WarehouseLtoApplicationDto>>> SubmitLtoApplication(
        [FromBody] SubmitWarehouseLtoApplicationRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SubmitLtoApplicationAsync(request, cancellationToken));

    [HttpPut("rsas")]
    public async Task<ActionResult<ApiResponse<ClientFacilityRegistrationDto>>> UpsertRsas(
        [FromBody] UpsertFacilityRegistrationRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpsertRsasAsync(request, cancellationToken));

    [HttpPost("rsas/{uuid:guid}/submit")]
    public async Task<ActionResult<ApiResponse<ClientFacilityRegistrationDto>>> SubmitRsas(
        Guid uuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SubmitRsasAsync(uuid, cancellationToken));
}

[ApiController]
[Route("api/v1/ops/warehouse/importer-contracts")]
[Authorize(Roles = "ROLE_WAREHOUSE_STAFF,ROLE_ADMIN")]
public class WarehouseImporterContractsController : OpsPortalControllerBase
{
    private readonly IWarehouseImporterContractService _service;

    public WarehouseImporterContractsController(IWarehouseImporterContractService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WarehouseImporterContractListItemDto>>>> List(
        [FromQuery] long? warehouseFacilityId,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAsync(warehouseFacilityId, cancellationToken));

    [HttpPut]
    public async Task<ActionResult<ApiResponse<WarehouseImporterContractListItemDto>>> Upsert(
        [FromBody] UpsertWarehouseImporterContractRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpsertAsync(request, cancellationToken));
}

[ApiController]
[Route("api/v1/ops/warehouse/releases")]
[Authorize(Roles = "ROLE_WAREHOUSE_STAFF,ROLE_ADMIN")]
public class WarehouseReleaseController : OpsPortalControllerBase
{
    private readonly IWarehouseOpsService _service;

    public WarehouseReleaseController(IWarehouseOpsService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReleaseRecordListItemDto>>> Execute(
        [FromBody] ExecuteReleaseRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ExecuteReleaseAsync(request, cancellationToken));
}

[ApiController]
[Route("api/v1/ops/driver/dashboard")]
[Authorize(Roles = "ROLE_DRIVER,ROLE_OPERATOR,ROLE_ADMIN")]
public class DriverDashboardController : OpsPortalControllerBase
{
    private readonly IDriverOpsService _service;

    public DriverDashboardController(IDriverOpsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DriverDashboardDto>>> Get(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDashboardAsync(cancellationToken));
}

[ApiController]
[Route("api/v1/ops/driver/profile")]
[Authorize(Roles = "ROLE_DRIVER,ROLE_OPERATOR,ROLE_ADMIN")]
public class DriverProfileController : OpsPortalControllerBase
{
    private readonly IDriverOpsService _service;

    public DriverProfileController(IDriverOpsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DriverProfileDto>>> Get(CancellationToken cancellationToken)
    {
        var profile = await _service.GetProfileAsync(cancellationToken);
        if (profile is null) return Ok(ApiResponse<DriverProfileDto>.Ok(new DriverProfileDto(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, Array.Empty<DriverDocumentDto>(), 0, false, null, null, null)));
        return Ok(ApiResponse<DriverProfileDto>.Ok(profile));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<DriverProfileDto>>> Update(
        [FromBody] UpdateDriverProfileRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateProfileAsync(request, cancellationToken));

    /// <summary>Face-verification selfie captured during driver registration (document type Selfie).</summary>
    [HttpGet("photo")]
    public async Task<IActionResult> GetProfilePhoto(CancellationToken cancellationToken)
    {
        try
        {
            var file = await _service.DownloadDocumentAsync("Selfie", cancellationToken);
            return File(file.Content, file.ContentType, file.DownloadName);
        }
        catch (ClientPortalException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }
}

[ApiController]
[Route("api/v1/ops/driver/containers")]
[Authorize(Roles = "ROLE_DRIVER,ROLE_OPERATOR,ROLE_ADMIN")]
public class DriverContainersController : OpsPortalControllerBase
{
    private readonly IDriverOpsService _service;

    public DriverContainersController(IDriverOpsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContainerListItemDto>>>> List(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAssignedContainersAsync(cancellationToken));

    [HttpPut("{uuid:guid}/status")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> UpdateStatus(
        Guid uuid,
        [FromBody] UpdateContainerStatusRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateContainerStatusAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/location")]
    public async Task<ActionResult<ApiResponse<object>>> RecordLocation(
        Guid uuid,
        [FromBody] RecordContainerLocationRequest request,
        CancellationToken cancellationToken)
    {
        await _service.RecordContainerLocationAsync(uuid, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { recorded = true }));
    }

    [HttpPost("{uuid:guid}/locations/batch")]
    public async Task<ActionResult<ApiResponse<object>>> RecordLocationsBatch(
        Guid uuid,
        [FromBody] RecordContainerLocationsBatchRequest request,
        CancellationToken cancellationToken)
    {
        var count = await _service.RecordContainerLocationsBatchAsync(uuid, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { recorded = count }));
    }
}

[ApiController]
[Route("api/mobile/auth")]
public class MobileAuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public MobileAuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ip, cancellationToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result));
    }

    [HttpPost("register-driver")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterDriver(
        [FromBody] RegisterDriverRequest request,
        [FromServices] IMobileDriverAuthService driverAuth,
        CancellationToken cancellationToken)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await driverAuth.RegisterDriverAsync(request, ip, cancellationToken);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpPost("validate-invite")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<InviteCodeValidationDto>>> ValidateInvite(
        [FromBody] ValidateInviteCodeRequest request,
        [FromServices] IMobileDriverAuthService driverAuth,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await driverAuth.ValidateInviteCodeAsync(request, cancellationToken);
            return Ok(ApiResponse<InviteCodeValidationDto>.Ok(result));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<InviteCodeValidationDto>.Fail(ex.Code, ex.Message));
        }
    }
}

[ApiController]
[Route("api/mobile/containers")]
[Authorize]
public class MobileContainersController : OpsPortalControllerBase
{
    private readonly IDriverOpsService _service;

    public MobileContainersController(IDriverOpsService service) => _service = service;

    [HttpGet("assigned")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContainerListItemDto>>>> Assigned(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListAssignedContainersAsync(cancellationToken));

    [HttpPut("{uuid:guid}/status")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> UpdateStatus(
        Guid uuid,
        [FromBody] UpdateContainerStatusRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateContainerStatusAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/pickup")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    [Consumes("application/json")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> ConfirmPickup(
        Guid uuid,
        [FromBody] ConfirmContainerPickupRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ConfirmContainerPickupAsync(uuid, request, cancellationToken: cancellationToken));

    [HttpPost("{uuid:guid}/pickup")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> ConfirmPickupWithGeotag(
        Guid uuid,
        [FromForm] decimal latitude,
        [FromForm] decimal longitude,
        [FromForm] string? label,
        [FromForm] string? capturedAt,
        IFormFile photo,
        CancellationToken cancellationToken)
    {
        if (photo is null || photo.Length == 0)
        {
            return BadRequest(ApiResponse<ContainerListItemDto>.Fail("FILE_REQUIRED", "Geotagged photo is required."));
        }

        var capturedUtc = ParseCapturedAtUtc(capturedAt);
        await using var stream = photo.OpenReadStream();
        var request = new ConfirmContainerPickupRequest(latitude, longitude, label);
        return await ExecuteAsync(() =>
            _service.ConfirmContainerPickupAsync(
                uuid,
                request,
                stream,
                photo.FileName,
                capturedUtc,
                cancellationToken));
    }

    [HttpPost("{uuid:guid}/location")]
    public async Task<ActionResult<ApiResponse<object>>> RecordLocation(
        Guid uuid,
        [FromBody] RecordContainerLocationRequest request,
        CancellationToken cancellationToken)
    {
        await _service.RecordContainerLocationAsync(uuid, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { recorded = true }));
    }

    [HttpPost("{uuid:guid}/locations/batch")]
    public async Task<ActionResult<ApiResponse<object>>> RecordLocationsBatch(
        Guid uuid,
        [FromBody] RecordContainerLocationsBatchRequest request,
        CancellationToken cancellationToken)
    {
        var count = await _service.RecordContainerLocationsBatchAsync(uuid, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { recorded = count }));
    }

    [HttpPost("{uuid:guid}/compliance-events")]
    public async Task<ActionResult<ApiResponse<object>>> ReportCompliance(
        Guid uuid,
        [FromBody] ReportDeliveryComplianceRequest request,
        CancellationToken cancellationToken)
    {
        await _service.ReportDeliveryComplianceAsync(uuid, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { recorded = true }));
    }

    [HttpPost("{uuid:guid}/driver-incident-report")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(24 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<DriverIncidentReportResultDto>>> SubmitDriverIncidentReport(
        Guid uuid,
        [FromForm] string category,
        [FromForm] string? notes,
        [FromForm] decimal latitude,
        [FromForm] decimal longitude,
        [FromForm] string? capturedAt,
        IFormFile photo1,
        IFormFile photo2,
        CancellationToken cancellationToken)
    {
        if (photo1 is null || photo1.Length == 0 || photo2 is null || photo2.Length == 0)
        {
            return BadRequest(ApiResponse<DriverIncidentReportResultDto>.Fail(
                "FILES_REQUIRED",
                "Two geotagged photos are required for an incident report."));
        }

        var capturedUtc = ParseCapturedAtUtc(capturedAt) ?? DateTime.UtcNow;
        await using var upload1 = photo1.OpenReadStream();
        await using var upload2 = photo2.OpenReadStream();
        using var stream1 = new MemoryStream();
        using var stream2 = new MemoryStream();
        await upload1.CopyToAsync(stream1, cancellationToken);
        await upload2.CopyToAsync(stream2, cancellationToken);
        var photo1Bytes = stream1.ToArray();
        var photo2Bytes = stream2.ToArray();
        return await ExecuteAsync(() =>
            _service.SubmitDriverIncidentReportAsync(
                uuid,
                category,
                notes,
                latitude,
                longitude,
                capturedUtc,
                new MemoryStream(photo1Bytes),
                new MemoryStream(photo2Bytes),
                photo1.FileName,
                photo2.FileName,
                cancellationToken));
    }

    [HttpGet("{uuid:guid}/track")]
    public async Task<ActionResult<ApiResponse<ContainerTrackDto>>> Track(
        Guid uuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetContainerTrackAsync(uuid, cancellationToken));

    [HttpGet("{uuid:guid}/delivery-qr")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<DriverDeliveryHandoffQrDto>>> DeliveryQr(
        Guid uuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDeliveryHandoffQrAsync(uuid, cancellationToken));

    [HttpGet("{uuid:guid}/geotag-photos/{photoUuid:guid}")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    public async Task<IActionResult> GeotagPhoto(
        Guid uuid,
        Guid photoUuid,
        CancellationToken cancellationToken)
    {
        try
        {
            var file = await _service.GetDriverGeotagPhotoAsync(uuid, photoUuid, cancellationToken);
            return File(file.Stream, file.ContentType, file.FileName);
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpPost("scan/preview")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<DriverTransportQrPreviewDto>>> PreviewTransportQr(
        [FromBody] ScanTransportQrRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.PreviewTransportQrAsync(request, cancellationToken));

    [HttpPost("scan/accept")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> AcceptTransportQr(
        [FromBody] ScanTransportQrRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.AcceptDeliveryFromQrAsync(request, cancellationToken));

    [HttpPost("{uuid:guid}/check-in")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    [Consumes("application/json")]
    public async Task<ActionResult<ApiResponse<DriverWarehouseCheckInResultDto>>> CheckInAtWarehouse(
        Guid uuid,
        [FromBody] DriverWarehouseCheckInRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CheckInAtWarehouseAsync(uuid, request, cancellationToken: cancellationToken));

    [HttpPost("{uuid:guid}/check-in")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<DriverWarehouseCheckInResultDto>>> CheckInAtWarehouseWithGeotag(
        Guid uuid,
        [FromForm] decimal latitude,
        [FromForm] decimal longitude,
        [FromForm] string? capturedAt,
        IFormFile photo,
        CancellationToken cancellationToken)
    {
        if (photo is null || photo.Length == 0)
        {
            return BadRequest(ApiResponse<DriverWarehouseCheckInResultDto>.Fail("FILE_REQUIRED", "Geotagged photo is required."));
        }

        var capturedUtc = ParseCapturedAtUtc(capturedAt);
        await using var stream = photo.OpenReadStream();
        var request = new DriverWarehouseCheckInRequest(latitude, longitude);
        return await ExecuteAsync(() =>
            _service.CheckInAtWarehouseAsync(
                uuid,
                request,
                stream,
                photo.FileName,
                capturedUtc,
                cancellationToken));
    }

    [HttpPost("{uuid:guid}/reset-delivery-test")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> ResetDeliveryTest(
        Guid uuid,
        [FromBody] ResetDriverDeliveryTestRequest? request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ResetDeliveryTestAsync(uuid, request, cancellationToken));

    [HttpPost("{uuid:guid}/empty-container-geotag")]
    [Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> SubmitEmptyContainerGeotag(
        Guid uuid,
        [FromForm] decimal latitude,
        [FromForm] decimal longitude,
        [FromForm] string? capturedAt,
        IFormFile photo,
        CancellationToken cancellationToken)
    {
        if (photo is null || photo.Length == 0)
        {
            return BadRequest(ApiResponse<ContainerListItemDto>.Fail("FILE_REQUIRED", "Geotagged photo is required."));
        }

        var capturedUtc = ParseCapturedAtUtc(capturedAt);
        await using var stream = photo.OpenReadStream();
        var request = new ConfirmContainerPickupRequest(latitude, longitude);
        return await ExecuteAsync(() =>
            _service.SubmitEmptyContainerGeotagAsync(
                uuid,
                request,
                stream,
                photo.FileName,
                capturedUtc,
                cancellationToken));
    }
}

[ApiController]
[Route("api/mobile/driver")]
[Authorize(Roles = "ROLE_DRIVER,ROLE_ADMIN")]
public class MobileDriverController : OpsPortalControllerBase
{
    private readonly IDriverOpsService _service;

    public MobileDriverController(IDriverOpsService service) => _service = service;

    [HttpPost("documents/{documentType}")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<DriverDocumentDto>>> UploadDocument(
        string documentType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<DriverDocumentDto>.Fail("FILE_REQUIRED", "Document file is required."));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _service.UploadDocumentAsync(documentType, stream, file.FileName, cancellationToken);
            return Ok(ApiResponse<DriverDocumentDto>.Ok(result));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<DriverDocumentDto>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpGet("documents/{documentType}/download")]
    public async Task<IActionResult> DownloadDocument(string documentType, CancellationToken cancellationToken)
    {
        try
        {
            var file = await _service.DownloadDocumentAsync(documentType, cancellationToken);
            return File(file.Content, file.ContentType, file.DownloadName);
        }
        catch (ClientPortalException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpPost("face-verification")]
    public async Task<ActionResult<ApiResponse<DriverProfileDto>>> SubmitFaceVerification(
        [FromBody] SubmitFaceVerificationRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.SubmitFaceVerificationAsync(request, cancellationToken));
}

[ApiController]
[Route("api/mobile/push")]
[Authorize]
public class MobilePushController : OpsPortalControllerBase
{
    private readonly IAgriTrackPushService _service;

    public MobilePushController(IAgriTrackPushService service) => _service = service;

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register(
        [FromBody] RegisterMobilePushDeviceRequest request,
        CancellationToken cancellationToken)
    {
        await _service.RegisterCurrentUserDeviceAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { registered = true }));
    }

    [HttpDelete("register")]
    public async Task<ActionResult<ApiResponse<object>>> Unregister(
        [FromQuery] string expoPushToken,
        CancellationToken cancellationToken)
    {
        await _service.RemoveCurrentUserDeviceAsync(expoPushToken, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { removed = true }));
    }
}

[ApiController]
[Route("api/mobile/news")]
[Authorize]
public class MobileNewsController : OpsPortalControllerBase
{
    private readonly IAgencyNewsService _service;

    public MobileNewsController(IAgencyNewsService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgencyNewsListItemDto>>>> List(
        [FromQuery] string? category,
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListPublishedAsync(category, search, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<AgencyNewsDetailDto>>> Get(
        Guid uuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(async () =>
        {
            var item = await _service.GetPublishedAsync(uuid, cancellationToken);
            if (item is null)
            {
                throw new ClientPortalException("NOT_FOUND", "News post not found.");
            }

            return item;
        });
}

[ApiController]
[Route("api/mobile/sync")]
[Authorize]
public class MobileSyncController : OpsPortalControllerBase
{
    private readonly IMobileSyncService _service;

    public MobileSyncController(IMobileSyncService service) => _service = service;

    [HttpPost("push")]
    public async Task<ActionResult<ApiResponse<MobileSyncPushResultDto>>> Push(
        [FromBody] MobileSyncPushRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.PushAsync(request, cancellationToken));

    [HttpGet("pull")]
    public async Task<ActionResult<ApiResponse<MobileSyncPullResultDto>>> Pull(CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.PullAsync(cancellationToken));
}

[ApiController]
[Route("api/mobile/operator/containers")]
[Authorize(Roles = "ROLE_OPERATOR,ROLE_ADMIN")]
public class MobileOperatorContainersController : OpsPortalControllerBase
{
    private readonly IOperatorOpsService _service;

    public MobileOperatorContainersController(IOperatorOpsService service) => _service = service;

    [HttpGet("claimable")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContainerListItemDto>>>> ListClaimable(
        [FromQuery] string? search,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListClaimableContainersAsync(search, cancellationToken));

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

    [HttpPost("route-test-provision")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> ProvisionRouteTest(
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ProvisionRouteTestContainerAsync(cancellationToken));

    [HttpPost("{uuid:guid}/assign-driver")]
    public async Task<ActionResult<ApiResponse<ContainerListItemDto>>> AssignDriver(
        Guid uuid,
        [FromBody] AssignDriverRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.AssignDriverAsync(uuid, request, cancellationToken));

    [HttpGet("claimed")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ContainerListItemDto>>>> ListClaimed(
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListClaimedContainersAsync(cancellationToken));

    [HttpGet("{uuid:guid}/delivery-stops")]
    public async Task<ActionResult<ApiResponse<OperatorDeliveryStopsDto>>> GetDeliveryStops(
        Guid uuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetDeliveryStopsAsync(uuid, cancellationToken));

    [HttpPut("{uuid:guid}/delivery-stops")]
    public async Task<ActionResult<ApiResponse<OperatorDeliveryStopsDto>>> BookDeliveryStops(
        Guid uuid,
        [FromBody] BookOperatorDeliveryStopsRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.BookDeliveryStopsAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/mobile")]
public class MobileHealthController : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> Health() =>
        Ok(ApiResponse<object>.Ok(new { status = "ok", module = "mobile" }));
}

public abstract class OpsPortalControllerBase : ControllerBase
{
    protected static DateTime? ParseCapturedAtUtc(string? capturedAt)
    {
        if (string.IsNullOrWhiteSpace(capturedAt))
        {
            return null;
        }

        return DateTime.TryParse(
            capturedAt,
            provider: null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

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
