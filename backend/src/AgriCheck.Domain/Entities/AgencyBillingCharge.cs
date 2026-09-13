using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class AgencyBillingCharge : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long AgencyBillingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }

    public AgencyBilling AgencyBilling { get; set; } = null!;
}
