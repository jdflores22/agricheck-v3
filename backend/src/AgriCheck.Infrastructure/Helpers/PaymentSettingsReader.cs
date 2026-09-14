using AgriCheck.Application.AdminPortal;
using AgriCheck.Application.Payments;
using AgriCheck.Domain.Entities;
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

    public static async Task<ResolvedAgencyPaymentGateway> ResolveForSystemAsync(
        AgriCheckDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetAllAsync(db, cancellationToken);
        var enabled = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoEnabled) == "1";
        var apiKey = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoApiKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = configuration["PayMongo:ApiKey"];
        }

        var webhookSecret = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoWebhookSecret);
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            webhookSecret = configuration["PayMongo:WebhookSecret"];
        }

        var publicKey = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoPublicKey);

        if (enabled && !string.IsNullOrWhiteSpace(apiKey))
        {
            return new ResolvedAgencyPaymentGateway(
                true,
                true,
                "paymongo",
                apiKey.Trim(),
                webhookSecret?.Trim(),
                string.IsNullOrWhiteSpace(publicKey) ? null : publicKey.Trim(),
                true);
        }

        return new ResolvedAgencyPaymentGateway(false, true, "simulated", null, null, null, false);
    }

    public static async Task<ResolvedAgencyPaymentGateway> ResolveForAgencyAsync(
        AgriCheckDbContext db,
        long agencyId,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var agencySettings = await TryGetAgencySettingsAsync(db, agencyId, cancellationToken);
        var cashEnabled = agencySettings?.CashPaymentEnabled ?? true;

        if (agencySettings?.PayMongoEnabled == true && !string.IsNullOrWhiteSpace(agencySettings.PayMongoApiKey))
        {
            return new ResolvedAgencyPaymentGateway(
                true,
                cashEnabled,
                "paymongo",
                agencySettings.PayMongoApiKey.Trim(),
                agencySettings.PayMongoWebhookSecret?.Trim(),
                agencySettings.PayMongoPublicKey?.Trim(),
                true);
        }

        var mode = cashEnabled ? "cash_only" : "simulated";
        return new ResolvedAgencyPaymentGateway(false, cashEnabled, mode, null, null, null, false);
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

        if (decimal.TryParse(settings.GetValueOrDefault(key), out var amount) && amount >= 0)
        {
            return amount;
        }

        var defaults = PaymentSettingsDefaults.Values;
        var fallbackKey = entryType == EntryType.Export
            ? PaymentSettingsDefaults.EntryProcessingFeeExport
            : PaymentSettingsDefaults.EntryProcessingFeeImport;

        return decimal.TryParse(defaults.GetValueOrDefault(fallbackKey), out var fallbackAmount)
            ? fallbackAmount
            : 2500m;
    }

    /// <summary>
    /// System-provider entry processing fee (configured in /admin/payment-config).
    /// Agency-specific <c>processing_fee_configs</c> rows are legacy and are not used for client entry bills.
    /// </summary>
    public static Task<decimal> ResolveEntryProcessingFeeForAgencyAsync(
        AgriCheckDbContext db,
        long agencyId,
        EntryType entryType,
        CancellationToken cancellationToken = default) =>
        ResolveEntryProcessingFeeAsync(db, entryType, cancellationToken);

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

        try
        {
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
        }
        catch
        {
            // Legacy production DBs may not have agency_payment_settings yet.
        }

        return secrets;
    }

    public static async Task<AgencyPaymentSettings?> TryGetAgencySettingsAsync(
        AgriCheckDbContext db,
        long agencyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.AgencyPaymentSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.AgencyId == agencyId, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
