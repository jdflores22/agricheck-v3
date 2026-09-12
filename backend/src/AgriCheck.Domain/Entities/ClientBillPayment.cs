using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class ClientBillPayment : BaseEntity
{
    public long ClientBillId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string? PaymentUrl { get; set; }
    public string? GatewayTransactionId { get; set; }
    public string Status { get; set; } = "completed";

    public ClientBill ClientBill { get; set; } = null!;
}
