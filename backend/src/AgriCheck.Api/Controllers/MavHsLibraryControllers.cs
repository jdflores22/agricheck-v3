using AgriCheck.Application.Common;
using AgriCheck.Application.MavPortal;
using AgriCheck.Application.MavPortal.Dtos;
using AgriCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/mav/hs-categories")]
[Authorize]
public class MavHsCategoriesController : MavPortalControllerBase
{
    private readonly IMavHsLibraryService _service;

    public MavHsCategoriesController(IMavHsLibraryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavHsCategoryListItemDto>>>> List([FromQuery] long? agencyId, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListCategoriesAsync(agencyId, cancellationToken));

    [HttpGet("{uuid:guid}")]
    public async Task<ActionResult<ApiResponse<MavHsCategoryDetailDto>>> Get(Guid uuid, CancellationToken cancellationToken)
    {
        var item = await _service.GetCategoryAsync(uuid, cancellationToken);
        if (item is null) return NotFound(ApiResponse<MavHsCategoryDetailDto>.Fail("NOT_FOUND", "HS category not found."));
        return Ok(ApiResponse<MavHsCategoryDetailDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavHsCategoryDetailDto>>> Create([FromBody] CreateMavHsCategoryRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateCategoryAsync(request, cancellationToken));

    [HttpPut("{uuid:guid}")]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavHsCategoryDetailDto>>> Update(Guid uuid, [FromBody] UpdateMavHsCategoryRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateCategoryAsync(uuid, request, cancellationToken));

    [HttpGet("{uuid:guid}/headings")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavHsHeadingListItemDto>>>> ListHeadings(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListHeadingsAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/headings")]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavHsHeadingListItemDto>>> CreateHeading(Guid uuid, [FromBody] CreateMavHsHeadingRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateHeadingAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/mav/hs-headings")]
[Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_MAV_EVALUATOR,ROLE_MAV_SECRETARY,ROLE_ADMIN")]
public class MavHsHeadingsController : MavPortalControllerBase
{
    private readonly IMavHsLibraryService _service;

    public MavHsHeadingsController(IMavHsLibraryService service) => _service = service;

    [HttpPut("{uuid:guid}")]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavHsHeadingListItemDto>>> Update(Guid uuid, [FromBody] UpdateMavHsHeadingRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateHeadingAsync(uuid, request, cancellationToken));

    [HttpGet("{uuid:guid}/details")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavHsDetailListItemDto>>>> ListDetails(Guid uuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListDetailsAsync(uuid, cancellationToken));

    [HttpPost("{uuid:guid}/details")]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavHsDetailListItemDto>>> CreateDetail(Guid uuid, [FromBody] CreateMavHsDetailRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.CreateDetailAsync(uuid, request, cancellationToken));
}

[ApiController]
[Route("api/v1/mav/hs-details")]
[Authorize]
public class MavHsDetailsController : MavPortalControllerBase
{
    private readonly IMavHsLibraryService _service;

    public MavHsDetailsController(IMavHsLibraryService service) => _service = service;

    [HttpGet("picker")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MavHsDetailListItemDto>>>> Picker([FromQuery] long? agencyId, [FromQuery] Guid? categoryUuid, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.ListPickerDetailsAsync(agencyId, categoryUuid, cancellationToken));

    [HttpPut("{uuid:guid}")]
    [Authorize(Roles = "ROLE_MAV_ADMIN,ROLE_ADMIN")]
    public async Task<ActionResult<ApiResponse<MavHsDetailListItemDto>>> Update(Guid uuid, [FromBody] UpdateMavHsDetailRequest request, CancellationToken cancellationToken) =>
        await ExecuteAsync(() => _service.UpdateDetailAsync(uuid, request, cancellationToken));
}
