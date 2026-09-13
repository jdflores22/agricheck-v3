using AgriCheck.Application.Auth;
using AgriCheck.Application.Auth.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Common;
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
        if (profile is null) return Ok(ApiResponse<DriverProfileDto>.Ok(new DriverProfileDto(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, Array.Empty<DriverDocumentDto>(), 0, false, null, null)));
        return Ok(ApiResponse<DriverProfileDto>.Ok(profile));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<DriverProfileDto>>> Update(
        [FromBody] UpdateDriverProfileRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateProfileAsync(request, cancellationToken));
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

    [HttpPost("{uuid:guid}/location")]
    public async Task<ActionResult<ApiResponse<object>>> RecordLocation(
        Guid uuid,
        [FromBody] RecordContainerLocationRequest request,
        CancellationToken cancellationToken)
    {
        await _service.RecordContainerLocationAsync(uuid, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { recorded = true }));
    }

    [HttpGet("{uuid:guid}/track")]
    public async Task<ActionResult<ApiResponse<ContainerTrackDto>>> Track(
        Guid uuid,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.GetContainerTrackAsync(uuid, cancellationToken));

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
    public async Task<ActionResult<ApiResponse<DriverWarehouseCheckInResultDto>>> CheckInAtWarehouse(
        Guid uuid,
        [FromBody] DriverWarehouseCheckInRequest request,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CheckInAtWarehouseAsync(uuid, request, cancellationToken));
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
