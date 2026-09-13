using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class AgencyPaymentSettings : BaseEntity
{
    public long AgencyId { get; set; }
    public bool PayMongoEnabled { get; set; }
    public string? PayMongoApiKey { get; set; }
    public string? PayMongoWebhookSecret { get; set; }
    public string? PayMongoPublicKey { get; set; }
    public bool CashPaymentEnabled { get; set; } = true;
    public string? CashPaymentInstructions { get; set; }

    public Agency Agency { get; set; } = null!;
}
