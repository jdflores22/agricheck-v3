using AgriCheck.Application.AdminPortal.Dtos;

namespace AgriCheck.Application.AgencyPortal.Dtos;

public record AgencyPaymentSettingsDto(
    long AgencyId,
    string AgencyCode,
    string AgencyName,
    PaymentGatewaySettingsDto Gateway,
    bool CashPaymentEnabled,
    string? CashPaymentInstructions,
    bool UsesGlobalPayMongoFallback,
    IReadOnlyList<GlobalEntryProcessingFeeDto> ProcessingFees);

public record UpdateAgencyPaymentSettingsRequest(
    bool PayMongoEnabled,
    bool CashPaymentEnabled,
    string? PayMongoApiKey,
    string? PayMongoWebhookSecret,
    string? PayMongoPublicKey,
    string? CashPaymentInstructions,
    decimal ImportFeeAmount,
    decimal ExportFeeAmount,
    string Currency);

public record AgencyPendingCashPaymentDto(
    Guid BillUuid,
    string BillNumber,
    string? EntryReferenceNo,
    string ClientName,
    decimal Amount,
    string PaymentMethod,
    string? ExternalReference,
    DateTime SubmittedAt);

public record VerifyAgencyCashPaymentRequest(bool Approved, string? Notes);

public record AgencyBillingRevenueMonthlyDto(string Month, decimal Amount, int PaymentCount);

public record AgencyBillingRevenueReportDto(
    string AgencyCode,
    string AgencyName,
    decimal Collected,
    decimal Pending,
    int PaidCount,
    int PendingCashCount,
    int OpenBillings,
    IReadOnlyList<AgencyBillingRevenueMonthlyDto> ByMonth);
