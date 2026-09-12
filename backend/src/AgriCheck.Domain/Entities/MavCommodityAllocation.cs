using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class MavCommodityAllocation : BaseEntity
{
    public long ApplicationPeriodId { get; set; }
    public long CommodityId { get; set; }
    public string HsCode { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public decimal TotalVolume { get; set; }
    public decimal AllocatedVolume { get; set; }
    public decimal MinimumImportVolume { get; set; }
    public long? AgencyId { get; set; }

    public MavApplicationPeriod ApplicationPeriod { get; set; } = null!;
    public Commodity Commodity { get; set; } = null!;
    public Agency? Agency { get; set; }
}
