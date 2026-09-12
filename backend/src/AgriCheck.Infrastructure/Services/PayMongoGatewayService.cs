using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        CancellationToken cancellationToken = default)
    {
        var payMongoEnabled = await PaymentSettingsReader.IsPayMongoEnabledAsync(_db, cancellationToken);
        var apiKey = await PaymentSettingsReader.GetPayMongoApiKeyAsync(_db, _configuration, cancellationToken);

        if (!payMongoEnabled || string.IsNullOrWhiteSpace(apiKey))
        {
            return new PaymentIntentResult(paymentReference, null, null, "simulated");
        }

        try
        {
            var client = CreateAuthorizedClient(apiKey);
            var amountCentavos = (int)(amount * 100);

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
                        payment_method_types = ResolvePaymentMethodTypes(paymentMethod),
                        success_url = successUrl,
                        cancel_url = cancelUrl,
                        reference_number = paymentReference,
                        description,
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
            else
            {
                _logger.LogWarning("PayMongo checkout_sessions failed: {Status} {Body}", checkoutResponse.StatusCode, checkoutBody);
            }

            throw new InvalidOperationException("PayMongo checkout session could not be created.");
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
        var secret = await PaymentSettingsReader.GetPayMongoWebhookSecretAsync(_db, _configuration, cancellationToken);
        if (string.IsNullOrWhiteSpace(secret)) return true;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();
        return string.Equals(computed, signature, StringComparison.OrdinalIgnoreCase);
    }

    public Task<(string PaymentReference, string Status)?> ParseWebhookPayloadAsync(string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("attributes", out var eventAttributes))
            {
                var eventType = eventAttributes.TryGetProperty("type", out var eventTypeEl)
                    ? eventTypeEl.GetString()
                    : data.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;

                if (eventAttributes.TryGetProperty("data", out var nestedData) &&
                    nestedData.TryGetProperty("attributes", out var nestedAttributes))
                {
                    var paymentReference = ReadPaymentReference(nestedAttributes);
                    var status = ReadPaymentStatus(nestedAttributes, eventType);
                    if (!string.IsNullOrWhiteSpace(paymentReference))
                    {
                        return Task.FromResult<(string PaymentReference, string Status)?>((paymentReference, status));
                    }
                }

                var directReference = ReadPaymentReference(eventAttributes);
                var directStatus = ReadPaymentStatus(eventAttributes, eventType);
                if (!string.IsNullOrWhiteSpace(directReference))
                {
                    return Task.FromResult<(string PaymentReference, string Status)?>((directReference, directStatus));
                }
            }

            if (root.TryGetProperty("attributes", out var flatAttributes))
            {
                var paymentReference = ReadPaymentReference(flatAttributes);
                var status = ReadPaymentStatus(flatAttributes, null);
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

    private static string[] ResolvePaymentMethodTypes(string paymentMethod)
    {
        switch (paymentMethod.ToLowerInvariant())
        {
            case "gcash":
                return new[] { "gcash" };
            case "paymaya":
            case "maya":
                return new[] { "paymaya" };
            case "qrph":
            case "qr":
                return new[] { "qrph" };
            case "card":
                return new[] { "card" };
            default:
                return new[] { "card", "gcash", "paymaya", "qrph" };
        }
    }

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
        if (!doc.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("attributes", out var attrs) ||
            !attrs.TryGetProperty("payments", out var payments))
        {
            return false;
        }

        if (payments.ValueKind != JsonValueKind.Array)
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
}
