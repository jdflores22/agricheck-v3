using AgriCheck.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
[AllowAnonymous]
public class SystemBrandingController : ControllerBase
{
    private readonly ISystemBrandingService _service;

    public SystemBrandingController(ISystemBrandingService service) => _service = service;

    [HttpGet("branding")]
    public async Task<ActionResult<ApiResponse<SystemBrandingDto>>> GetBranding(CancellationToken cancellationToken) =>
        Ok(ApiResponse<SystemBrandingDto>.Ok(await _service.GetPublicBrandingAsync(cancellationToken)));
}
