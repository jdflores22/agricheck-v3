using AgriCheck.Application.AdminPortal;
using AgriCheck.Application.Payments;
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

    public static async Task<ResolvedAgencyPaymentGateway> ResolveForAgencyAsync(
        AgriCheckDbContext db,
        long agencyId,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var agencySettings = await db.AgencyPaymentSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.AgencyId == agencyId, cancellationToken);

        var global = await GetAllAsync(db, cancellationToken);
        var globalApiKey = global.GetValueOrDefault(PaymentSettingsDefaults.PayMongoApiKey);
        if (string.IsNullOrWhiteSpace(globalApiKey))
        {
            globalApiKey = configuration["PayMongo:ApiKey"];
        }

        var globalWebhook = global.GetValueOrDefault(PaymentSettingsDefaults.PayMongoWebhookSecret);
        if (string.IsNullOrWhiteSpace(globalWebhook))
        {
            globalWebhook = configuration["PayMongo:WebhookSecret"];
        }

        var globalPublicKey = global.GetValueOrDefault(PaymentSettingsDefaults.PayMongoPublicKey);
        var globalEnabled = global.GetValueOrDefault(PaymentSettingsDefaults.PayMongoEnabled) == "1";

        var cashEnabled = agencySettings?.CashPaymentEnabled ?? true;
        var usesAgencyCredentials = false;
        var payMongoEnabled = false;
        string? apiKey = null;
        string? webhookSecret = null;
        string? publicKey = null;

        if (agencySettings is not null)
        {
            if (agencySettings.PayMongoEnabled && !string.IsNullOrWhiteSpace(agencySettings.PayMongoApiKey))
            {
                usesAgencyCredentials = true;
                payMongoEnabled = true;
                apiKey = agencySettings.PayMongoApiKey;
                webhookSecret = agencySettings.PayMongoWebhookSecret;
                publicKey = agencySettings.PayMongoPublicKey;
            }
            else if (!agencySettings.PayMongoEnabled)
            {
                payMongoEnabled = false;
            }
            else
            {
                payMongoEnabled = globalEnabled && !string.IsNullOrWhiteSpace(globalApiKey);
                apiKey = globalApiKey;
                webhookSecret = globalWebhook;
                publicKey = globalPublicKey;
            }
        }
        else
        {
            payMongoEnabled = globalEnabled && !string.IsNullOrWhiteSpace(globalApiKey);
            apiKey = globalApiKey;
            webhookSecret = globalWebhook;
            publicKey = globalPublicKey;
        }

        var mode = payMongoEnabled ? "paymongo" : cashEnabled ? "cash_only" : "simulated";

        return new ResolvedAgencyPaymentGateway(
            payMongoEnabled,
            cashEnabled,
            mode,
            apiKey?.Trim(),
            webhookSecret?.Trim(),
            publicKey?.Trim(),
            usesAgencyCredentials);
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

    public static async Task<decimal> ResolveEntryProcessingFeeForAgencyAsync(
        AgriCheckDbContext db,
        long agencyId,
        EntryType entryType,
        CancellationToken cancellationToken = default)
    {
        var agencyFee = await db.ProcessingFeeConfigs.AsNoTracking()
            .Where(c => c.AgencyId == agencyId && c.EntryType == entryType && c.IsActive)
            .Select(c => (decimal?)c.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        if (agencyFee is > 0)
        {
            return agencyFee.Value;
        }

        return await ResolveEntryProcessingFeeAsync(db, entryType, cancellationToken);
    }

    public static async Task<IReadOnlyList<string>> GetWebhookSecretsAsync(
        AgriCheckDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var secrets = new List<string>();
        var global = await GetPayMongoWebhookSecretAsync(db, configuration, cancellationToken);
        if (!string.IsNullOrWhiteSpace(global))
        {
            secrets.Add(global);
        }

        var agencySecrets = await db.AgencyPaymentSettings.AsNoTracking()
            .Where(s => s.PayMongoEnabled && s.PayMongoWebhookSecret != null && s.PayMongoWebhookSecret != "")
            .Select(s => s.PayMongoWebhookSecret!)
            .ToListAsync(cancellationToken);

        foreach (var secret in agencySecrets)
        {
            if (!secrets.Contains(secret, StringComparer.Ordinal))
            {
                secrets.Add(secret);
            }
        }

        return secrets;
    }
}
