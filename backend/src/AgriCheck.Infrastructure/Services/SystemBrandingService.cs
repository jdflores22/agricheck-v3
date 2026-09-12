using AgriCheck.Application.Common;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class SystemBrandingService : ISystemBrandingService
{
    private readonly AgriCheckDbContext _db;

    public SystemBrandingService(AgriCheckDbContext db) => _db = db;

    public async Task<SystemBrandingDto> GetPublicBrandingAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.SystemSettings.AsNoTracking()
            .ToDictionaryAsync(x => x.SettingKey, x => x.SettingValue, StringComparer.OrdinalIgnoreCase, cancellationToken);

        string Get(string key, string fallback = "") =>
            settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

        return new SystemBrandingDto(
            Get("system_name", "AgriCheck System"),
            NormalizeAssetUrl(Get("system_logo_path")),
            NormalizeAssetUrl(Get("spinner_logo_path")),
            NormalizeAssetUrl(Get("favicon_path")),
            Get("spinner_color", "#166534"),
            Get("primary_color", "#166534"),
            Get("footer_text", "© 2025 Department of Agriculture. All rights reserved."));
    }

    internal static string? NormalizeAssetUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return path.StartsWith('/') ? path : $"/{path}";
    }
}
