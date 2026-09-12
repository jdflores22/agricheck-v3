using AgriCheck.Application.AdminPortal;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgriCheck.Infrastructure.Helpers;

public static class PaymentSettingsReader
{
    public static async Task<Dictionary<string, string>> GetAllAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        var stored = await db.SystemSettings.AsNoTracking()
            .Where(s => PaymentSettingsDefaults.Values.Keys.Contains(s.SettingKey))
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var merged = new Dictionary<string, string>(PaymentSettingsDefaults.Values, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in stored)
        {
            merged[key] = value;
        }

        return merged;
    }

    public static async Task<bool> IsPayMongoEnabledAsync(AgriCheckDbContext db, CancellationToken cancellationToken = default)
    {
        var settings = await GetAllAsync(db, cancellationToken);
        return settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoEnabled) == "1"
            && !string.IsNullOrWhiteSpace(settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoApiKey));
    }

    public static async Task<string?> GetPayMongoApiKeyAsync(AgriCheckDbContext db, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var settings = await GetAllAsync(db, cancellationToken);
        var apiKey = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoApiKey);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return apiKey.Trim();
        }

        return configuration["PayMongo:ApiKey"];
    }

    public static async Task<string?> GetPayMongoWebhookSecretAsync(AgriCheckDbContext db, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var settings = await GetAllAsync(db, cancellationToken);
        var secret = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoWebhookSecret);
        if (!string.IsNullOrWhiteSpace(secret))
        {
            return secret.Trim();
        }

        return configuration["PayMongo:WebhookSecret"];
    }

    public static async Task<decimal> ResolveEntryProcessingFeeAsync(
        AgriCheckDbContext db,
        EntryType entryType,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetAllAsync(db, cancellationToken);
        var key = entryType == EntryType.Export
            ? PaymentSettingsDefaults.EntryProcessingFeeExport
            : PaymentSettingsDefaults.EntryProcessingFeeImport;

        if (decimal.TryParse(settings.GetValueOrDefault(key), out var amount) && amount > 0)
        {
            return amount;
        }

        return 1500m;
    }
}
