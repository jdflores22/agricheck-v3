using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgriCheck.Application.AdminPortal;
using AgriCheck.Application.Payments;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Services;

public class PayMongoGatewayService : IPaymentGatewayService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AgriCheckDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayMongoGatewayService> _logger;

    public PayMongoGatewayService(
        IHttpClientFactory httpClientFactory,
        AgriCheckDbContext db,
        IConfiguration configuration,
        ILogger<PayMongoGatewayService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount,
        string description,
        string paymentReference,
        string paymentMethod,
        string successUrl,
        string cancelUrl,
        long? agencyId = null,
        CancellationToken cancellationToken = default)
    {
        var gateway = agencyId is long resolvedAgencyId
            ? await PaymentSettingsReader.ResolveForAgencyAsync(_db, resolvedAgencyId, _configuration, cancellationToken)
            : await ResolveGlobalGatewayAsync(cancellationToken);

        if (!gateway.PayMongoEnabled || string.IsNullOrWhiteSpace(gateway.PayMongoApiKey))
        {
            return new PaymentIntentResult(paymentReference, null, null, "simulated");
        }

        var apiKey = gateway.PayMongoApiKey;

        try
        {
            var client = CreateAuthorizedClient(apiKey);
            var amountCentavos = (int)(amount * 100);

            var methodSets = ResolvePaymentMethodAttempts(paymentMethod);
            string? lastErrorBody = null;

            foreach (var methodTypes in methodSets)
            {
                var checkoutPayload = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            line_items = new[]
                            {
                                new
                                {
                                    currency = "PHP",
                                    amount = amountCentavos,
                                    name = description,
                                    quantity = 1,
                                },
                            },
                            payment_method_types = methodTypes,
                            success_url = successUrl,
                            cancel_url = cancelUrl,
                            reference_number = paymentReference,
                            description,
                            send_email_receipt = true,
                            metadata = new
                            {
                                payment_reference = paymentReference,
                                payment_method = paymentMethod,
                            },
                        },
                    },
                };

                using var checkoutResponse = await client.PostAsync(
                    "https://api.paymongo.com/v1/checkout_sessions",
                    new StringContent(JsonSerializer.Serialize(checkoutPayload), Encoding.UTF8, "application/json"),
                    cancellationToken);

                var checkoutBody = await checkoutResponse.Content.ReadAsStringAsync(cancellationToken);
                if (checkoutResponse.IsSuccessStatusCode)
                {
                    var checkoutUrl = ExtractCheckoutSessionUrl(checkoutBody);
                    var sessionId = ExtractCheckoutSessionId(checkoutBody);
                    if (!string.IsNullOrWhiteSpace(checkoutUrl))
                    {
                        return new PaymentIntentResult(paymentReference, sessionId, checkoutUrl, "paymongo");
                    }
                }

                lastErrorBody = checkoutBody;
                _logger.LogWarning(
                    "PayMongo checkout_sessions failed: {Status} {Body}",
                    checkoutResponse.StatusCode,
                    checkoutBody);

                if (!IsPaymentMethodNotAllowed(checkoutBody))
                {
                    break;
                }
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(lastErrorBody)
                    ? "PayMongo checkout session could not be created."
                    : "PayMongo checkout session could not be created.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "PayMongo checkout session error");
            throw new InvalidOperationException("PayMongo checkout session could not be created.", ex);
        }
    }

    public async Task<bool> IsCheckoutSessionPaidAsync(string checkoutSessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(checkoutSessionId))
        {
            return false;
        }

        var payMongoEnabled = await PaymentSettingsReader.IsPayMongoEnabledAsync(_db, cancellationToken);
        var apiKey = await PaymentSettingsReader.GetPayMongoApiKeyAsync(_db, _configuration, cancellationToken);
        if (!payMongoEnabled || string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        try
        {
            var client = CreateAuthorizedClient(apiKey);

            using var sessionResponse = await client.GetAsync(
                $"https://api.paymongo.com/v1/checkout_sessions/{checkoutSessionId}",
                cancellationToken);

            if (!sessionResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "PayMongo checkout session lookup failed: {Status} {SessionId}",
                    sessionResponse.StatusCode,
                    checkoutSessionId);
                return false;
            }

            var sessionBody = await sessionResponse.Content.ReadAsStringAsync(cancellationToken);
            return HasPaidCheckoutPayment(sessionBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to verify PayMongo checkout session {SessionId}", checkoutSessionId);
            return false;
        }
    }

    public async Task<bool> IsGatewayPaymentPaidAsync(string gatewayTransactionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gatewayTransactionId))
        {
            return false;
        }

        if (gatewayTransactionId.StartsWith("cs_", StringComparison.OrdinalIgnoreCase))
        {
            return await IsCheckoutSessionPaidAsync(gatewayTransactionId, cancellationToken);
        }

        if (gatewayTransactionId.StartsWith("link_", StringComparison.OrdinalIgnoreCase))
        {
            return await IsLegacyLinkPaidAsync(gatewayTransactionId, cancellationToken);
        }

        return false;
    }

    private async Task<bool> IsLegacyLinkPaidAsync(string linkId, CancellationToken cancellationToken)
    {
        var payMongoEnabled = await PaymentSettingsReader.IsPayMongoEnabledAsync(_db, cancellationToken);
        var apiKey = await PaymentSettingsReader.GetPayMongoApiKeyAsync(_db, _configuration, cancellationToken);
        if (!payMongoEnabled || string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        try
        {
            var client = CreateAuthorizedClient(apiKey);

            using var linkResponse = await client.GetAsync($"https://api.paymongo.com/v1/links/{linkId}", cancellationToken);
            if (linkResponse.IsSuccessStatusCode)
            {
                var linkBody = await linkResponse.Content.ReadAsStringAsync(cancellationToken);
                if (IsLegacyLinkMarkedPaid(linkBody))
                {
                    return true;
                }
            }

            using var paymentLinkResponse = await client.GetAsync(
                $"https://api.paymongo.com/v1/payment_links/{linkId}/payments",
                cancellationToken);

            if (!paymentLinkResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var paymentsBody = await paymentLinkResponse.Content.ReadAsStringAsync(cancellationToken);
            return HasPaidPaymentRecords(paymentsBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to verify PayMongo link {LinkId}", linkId);
            return false;
        }
    }

    public async Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        var secrets = await PaymentSettingsReader.GetWebhookSecretsAsync(_db, _configuration, cancellationToken);
        if (secrets.Count == 0) return true;
        if (string.IsNullOrWhiteSpace(signature)) return false;

        foreach (var secret in secrets)
        {
            if (VerifyWebhookSignatureWithSecret(payload, signature, secret))
            {
                return true;
            }
        }

        return false;
    }

    private static bool VerifyWebhookSignatureWithSecret(string payload, string signature, string secret)
    {
        var timestamp = ReadSignaturePart(signature, "t");
        var testSignature = ReadSignaturePart(signature, "te");
        var liveSignature = ReadSignaturePart(signature, "li");

        if (string.IsNullOrWhiteSpace(timestamp))
        {
            return SecureEquals(ComputeHexHmac(secret, payload), signature);
        }

        if (long.TryParse(timestamp, out var unixSeconds))
        {
            var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            if (age.Duration() > TimeSpan.FromMinutes(10))
            {
                return false;
            }
        }

        var computed = ComputeHexHmac(secret, $"{timestamp}.{payload}");
        return SecureEquals(computed, liveSignature) || SecureEquals(computed, testSignature);
    }

    public Task<(string PaymentReference, string Status)?> ParseWebhookPayloadAsync(string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var eventNode = root.TryGetProperty("data", out var data) ? data : root;
            var eventType = ReadEventType(eventNode);

            if (TryReadNestedResource(eventNode, out var resourceId, out var attributes))
            {
                var paymentReference = ReadPaymentReference(attributes) ?? resourceId;
                var status = ReadPaymentStatus(attributes, eventType);
                if (HasPaidCheckoutPayment(payload) || IsPaidEvent(eventType))
                {
                    status = "paid";
                }

                if (!string.IsNullOrWhiteSpace(paymentReference))
                {
                    return Task.FromResult<(string PaymentReference, string Status)?>((paymentReference, status));
                }
            }

            if (eventNode.TryGetProperty("attributes", out var eventAttributes))
            {
                var paymentReference = ReadPaymentReference(eventAttributes);
                var status = ReadPaymentStatus(eventAttributes, eventType);
                if (!string.IsNullOrWhiteSpace(paymentReference))
                {
                    return Task.FromResult<(string PaymentReference, string Status)?>((paymentReference, status));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to parse PayMongo webhook payload");
        }

        return Task.FromResult<(string PaymentReference, string Status)?>(null);
    }

    private HttpClient CreateAuthorizedClient(string apiKey)
    {
        var client = _httpClientFactory.CreateClient("PayMongo");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:")));
        return client;
    }

    private static IReadOnlyList<string[]> ResolvePaymentMethodAttempts(string paymentMethod)
    {
        switch (paymentMethod.ToLowerInvariant())
        {
            case "gcash":
                return new[] { new[] { "gcash" } };
            case "paymaya":
            case "maya":
                return new[] { new[] { "paymaya" } };
            case "qrph":
            case "qr":
                return new[] { new[] { "qrph" } };
            case "card":
                return new[]
                {
                    new[] { "card", "gcash", "paymaya", "qrph" },
                    new[] { "gcash", "paymaya", "qrph" },
                    new[] { "card" },
                };
            default:
                return new[]
                {
                    new[] { "gcash", "paymaya", "qrph", "card" },
                    new[] { "gcash", "paymaya", "qrph" },
                    new[] { "gcash" },
                };
        }
    }

    private static bool IsPaymentMethodNotAllowed(string body) =>
        body.Contains("payment method", StringComparison.OrdinalIgnoreCase)
        && (body.Contains("not allowed", StringComparison.OrdinalIgnoreCase)
            || body.Contains("not enabled", StringComparison.OrdinalIgnoreCase));

    private static string? ReadSignaturePart(string header, string key)
    {
        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0) continue;
            if (part[..separator].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return part[(separator + 1)..];
            }
        }

        return null;
    }

    private static string ComputeHexHmac(string secret, string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static bool SecureEquals(string left, string? right)
    {
        if (string.IsNullOrWhiteSpace(right) || left.Length != right.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right.ToLowerInvariant()));
    }

    private static string? ReadEventType(JsonElement eventNode)
    {
        if (eventNode.TryGetProperty("type", out var typeEl))
        {
            return typeEl.GetString();
        }

        if (eventNode.TryGetProperty("attributes", out var attrs) &&
            attrs.TryGetProperty("type", out var nestedType))
        {
            return nestedType.GetString();
        }

        return null;
    }

    private static bool TryReadNestedResource(JsonElement eventNode, out string? resourceId, out JsonElement attributes)
    {
        resourceId = null;
        attributes = default;

        JsonElement resource = default;
        if (eventNode.TryGetProperty("data", out var nested) && nested.ValueKind == JsonValueKind.Object)
        {
            resource = nested;
        }
        else if (eventNode.TryGetProperty("attributes", out var attrs) &&
                 attrs.TryGetProperty("data", out var legacy) &&
                 legacy.ValueKind == JsonValueKind.Object)
        {
            resource = legacy;
        }
        else
        {
            return false;
        }

        resourceId = resource.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (!resource.TryGetProperty("attributes", out attributes))
        {
            return false;
        }

        return true;
    }

    private static bool IsPaidEvent(string? eventType) =>
        !string.IsNullOrWhiteSpace(eventType) &&
        (eventType.Contains("payment.paid", StringComparison.OrdinalIgnoreCase) ||
         eventType.Contains("checkout_session.payment.paid", StringComparison.OrdinalIgnoreCase));

    private static string? ExtractCheckoutSessionUrl(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        if (!data.TryGetProperty("attributes", out var attrs))
        {
            return null;
        }

        if (attrs.TryGetProperty("checkout_url", out var checkoutUrl))
        {
            return checkoutUrl.GetString();
        }

        return null;
    }

    private static string? ExtractCheckoutSessionId(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        return data.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
    }

    private static bool HasPaidCheckoutPayment(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!TryGetCheckoutAttributes(doc.RootElement, out var attrs))
        {
            return false;
        }

        if (attrs.TryGetProperty("status", out var sessionStatus) && IsPaidStatus(sessionStatus.GetString()))
        {
            return true;
        }

        if (attrs.TryGetProperty("payment_intent", out var paymentIntent) &&
            paymentIntent.ValueKind == JsonValueKind.Object &&
            paymentIntent.TryGetProperty("attributes", out var intentAttrs) &&
            intentAttrs.TryGetProperty("status", out var intentStatus) &&
            IsPaidStatus(intentStatus.GetString()))
        {
            return true;
        }

        if (!attrs.TryGetProperty("payments", out var payments) || payments.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var payment in payments.EnumerateArray())
        {
            if (payment.TryGetProperty("attributes", out var paymentAttrs) &&
                paymentAttrs.TryGetProperty("status", out var statusEl) &&
                IsPaidStatus(statusEl.GetString()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetCheckoutAttributes(JsonElement root, out JsonElement attrs)
    {
        attrs = default;
        if (!root.TryGetProperty("data", out var data))
        {
            return false;
        }

        if (data.TryGetProperty("data", out var nested) &&
            nested.ValueKind == JsonValueKind.Object &&
            nested.TryGetProperty("attributes", out attrs))
        {
            return true;
        }

        return data.TryGetProperty("attributes", out attrs);
    }

    private static bool IsLegacyLinkMarkedPaid(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("attributes", out var attrs) ||
            !attrs.TryGetProperty("status", out var statusEl))
        {
            return false;
        }

        return IsPaidStatus(statusEl.GetString());
    }

    private static bool HasPaidPaymentRecords(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("data", out var data))
        {
            return false;
        }

        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var payment in data.EnumerateArray())
            {
                if (payment.TryGetProperty("attributes", out var attrs) &&
                    attrs.TryGetProperty("status", out var statusEl) &&
                    IsPaidStatus(statusEl.GetString()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsPaidStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) &&
        (status.Equals("paid", StringComparison.OrdinalIgnoreCase) ||
         status.Equals("succeeded", StringComparison.OrdinalIgnoreCase));

    private static string? ReadPaymentReference(JsonElement attributes)
    {
        if (attributes.TryGetProperty("metadata", out var metadata) &&
            metadata.TryGetProperty("payment_reference", out var refEl))
        {
            return refEl.GetString();
        }

        if (attributes.TryGetProperty("reference_number", out var refNum))
        {
            var value = refNum.GetString();
            if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("PAY-", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    private static string ReadPaymentStatus(JsonElement attributes, string? eventType)
    {
        if (attributes.TryGetProperty("status", out var statusEl))
        {
            var status = statusEl.GetString() ?? "unknown";
            if (IsPaidStatus(status))
            {
                return "paid";
            }

            return status;
        }

        if (!string.IsNullOrWhiteSpace(eventType) &&
            (eventType.Contains("paid", StringComparison.OrdinalIgnoreCase) ||
             eventType.Contains("succeeded", StringComparison.OrdinalIgnoreCase)))
        {
            return "paid";
        }

        return "unknown";
    }

    private async Task<ResolvedAgencyPaymentGateway> ResolveGlobalGatewayAsync(CancellationToken cancellationToken)
    {
        var enabled = await PaymentSettingsReader.IsPayMongoEnabledAsync(_db, cancellationToken);
        var apiKey = await PaymentSettingsReader.GetPayMongoApiKeyAsync(_db, _configuration, cancellationToken);
        var webhook = await PaymentSettingsReader.GetPayMongoWebhookSecretAsync(_db, _configuration, cancellationToken);
        var settings = await PaymentSettingsReader.GetAllAsync(_db, cancellationToken);
        var publicKey = settings.GetValueOrDefault(PaymentSettingsDefaults.PayMongoPublicKey);

        return new ResolvedAgencyPaymentGateway(
            enabled,
            true,
            enabled ? "paymongo" : "simulated",
            apiKey,
            webhook,
            string.IsNullOrWhiteSpace(publicKey) ? null : publicKey,
            false);
    }
}
