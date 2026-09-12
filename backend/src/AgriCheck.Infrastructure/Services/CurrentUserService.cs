using System.Security.Claims;
using AgriCheck.Application.ClientPortal;
using Microsoft.AspNetCore.Http;

namespace AgriCheck.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserUuid
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null) return null;
            var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var uuid) ? uuid : null;
        }
    }

    public long? UserId =>
        _httpContextAccessor.HttpContext?.Items["CurrentUserId"] as long?;

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
}
