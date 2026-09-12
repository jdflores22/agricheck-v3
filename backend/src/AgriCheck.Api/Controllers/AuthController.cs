using System.Security.Claims;
using AgriCheck.Application.Auth;
using AgriCheck.Application.Auth.Dtos;
using AgriCheck.Application.Common;
using AgriCheck.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriCheck.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _authService.LoginAsync(request, GetIpAddress(), cancellationToken));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _authService.RegisterAsync(request, GetIpAddress(), cancellationToken));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() => _authService.RefreshAsync(request, GetIpAddress(), cancellationToken));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { message = "Logged out." }));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { message = "If the email exists, a reset link has been sent." }));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync<object>(async () =>
        {
            await _authService.ResetPasswordAsync(request, cancellationToken);
            return new { message = "Password has been reset." };
        });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync<object>(async () =>
        {
            await _authService.VerifyEmailAsync(request, cancellationToken);
            return new { message = "Email verified successfully." };
        });
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResendVerification([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResendVerificationAsync(request.Email, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { message = "If the account exists and is unverified, a new verification email has been sent." }));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MeResponseDto>>> Me(CancellationToken cancellationToken)
    {
        var userUuid = GetUserUuid();
        if (userUuid is null)
        {
            return Unauthorized(ApiResponse<MeResponseDto>.Fail("UNAUTHORIZED", "Invalid token."));
        }

        var result = await _authService.GetCurrentUserAsync(userUuid.Value, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse<MeResponseDto>.Fail("USER_NOT_FOUND", "User not found."));
        }

        return Ok(ApiResponse<MeResponseDto>.Ok(result));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userUuid = GetUserUuid();
        if (userUuid is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "Invalid token."));
        }

        return await ExecuteAsync<object>(async () =>
        {
            await _authService.ChangePasswordAsync(userUuid.Value, request, cancellationToken);
            return new { message = "Password changed successfully." };
        });
    }

    private async Task<ActionResult<ApiResponse<T>>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return Ok(ApiResponse<T>.Ok(result));
        }
        catch (AuthException ex)
        {
            return BadRequest(ApiResponse<T>.Fail(ex.Code, ex.Message));
        }
        catch (OperationCanceledException)
        {
            return BadRequest(ApiResponse<T>.Fail("REQUEST_CANCELLED", "The request was cancelled."));
        }
    }

    private Guid? GetUserUuid()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var uuid) ? uuid : null;
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
