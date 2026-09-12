using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

public static class SystemSettingsReader
{
    public static async Task<string?> GetAsync(AgriCheckDbContext db, string key, CancellationToken cancellationToken = default) =>
        await db.SystemSettings.AsNoTracking()
            .Where(s => s.SettingKey == key)
            .Select(s => s.SettingValue)
            .FirstOrDefaultAsync(cancellationToken);

    public static async Task<int> GetIntAsync(AgriCheckDbContext db, string key, int defaultValue, CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(db, key, cancellationToken);
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
