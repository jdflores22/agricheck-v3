namespace AgriCheck.Application.AdminPortal;

public static class PaymentSettingsDefaults
{
    public const string PayMongoEnabled = "paymongo_enabled";
    public const string PayMongoApiKey = "paymongo_api_key";
    public const string PayMongoWebhookSecret = "paymongo_webhook_secret";
    public const string PayMongoPublicKey = "paymongo_public_key";
    public const string EntryProcessingFeeImport = "entry_processing_fee_import";
    public const string EntryProcessingFeeExport = "entry_processing_fee_export";
    public const string EntryProcessingFeeCurrency = "entry_processing_fee_currency";

    public static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [PayMongoEnabled] = "0",
        [PayMongoApiKey] = "",
        [PayMongoWebhookSecret] = "",
        [PayMongoPublicKey] = "",
        [EntryProcessingFeeImport] = "2500",
        [EntryProcessingFeeExport] = "2500",
        [EntryProcessingFeeCurrency] = "PHP",
    };
}
