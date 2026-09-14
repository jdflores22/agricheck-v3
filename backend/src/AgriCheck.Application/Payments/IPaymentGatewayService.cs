namespace AgriCheck.Application.Payments;

public record PaymentIntentResult(
    string PaymentReference,
    string? GatewayTransactionId,
    string? PaymentUrl,
    string Mode);

public interface IPaymentGatewayService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount,
        string description,
        string paymentReference,
        string paymentMethod,
        string successUrl,
        string cancelUrl,
        long? agencyId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsCheckoutSessionPaidAsync(string checkoutSessionId, long? agencyId = null, CancellationToken cancellationToken = default);

    Task<bool> IsGatewayPaymentPaidAsync(string gatewayTransactionId, long? agencyId = null, CancellationToken cancellationToken = default);

    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, CancellationToken cancellationToken = default);

    Task<(string PaymentReference, string Status)?> ParseWebhookPayloadAsync(string payload, CancellationToken cancellationToken = default);
}
