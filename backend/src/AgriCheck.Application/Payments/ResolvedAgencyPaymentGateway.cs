namespace AgriCheck.Application.Payments;

public record ResolvedAgencyPaymentGateway(
    bool PayMongoEnabled,
    bool CashPaymentEnabled,
    string Mode,
    string? PayMongoApiKey,
    string? PayMongoWebhookSecret,
    string? PayMongoPublicKey,
    bool UsesAgencyCredentials);
