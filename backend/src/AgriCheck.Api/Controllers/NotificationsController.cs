using AgriCheck.Application.Common;
using AgriCheck.Application.Notifications;
using AgriCheck.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> List([FromQuery] int limit = 20, [FromQuery] bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(await _service.ListAsync(limit, unreadOnly, cancellationToken)));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<NotificationDto>>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<object>>> UnreadCount(CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(ApiResponse<object>.Ok(new { count = await _service.GetUnreadCountAsync(cancellationToken) }));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpPost("{uuid:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(Guid uuid, CancellationToken cancellationToken = default)
    {
        try
        {
            await _service.MarkReadAsync(uuid, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { message = "Notification marked as read." }));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken cancellationToken = default)
    {
        try
        {
            await _service.MarkAllReadAsync(cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { message = "All notifications marked as read." }));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<ApiResponse<NotificationPreferenceDto>>> GetPreferences(CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(ApiResponse<NotificationPreferenceDto>.Ok(await _service.GetPreferencesAsync(cancellationToken)));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<NotificationPreferenceDto>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<NotificationPreferenceDto>>> UpdatePreferences([FromBody] UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(ApiResponse<NotificationPreferenceDto>.Ok(await _service.UpdatePreferencesAsync(request, cancellationToken)));
        }
        catch (ClientPortalException ex)
        {
            return BadRequest(ApiResponse<NotificationPreferenceDto>.Fail(ex.Code, ex.Message));
        }
    }
}
