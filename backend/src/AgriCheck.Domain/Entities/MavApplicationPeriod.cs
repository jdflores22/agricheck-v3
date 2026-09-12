using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class MavApplicationPeriod : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public int MavYear { get; set; }
    public MavPoolType PoolType { get; set; }
    public DateTime OpeningDate { get; set; }
    public DateTime ClosingDate { get; set; }
    public MavApplicationPeriodStatus Status { get; set; } = MavApplicationPeriodStatus.Upcoming;
    public long? AgencyId { get; set; }

    public Agency? Agency { get; set; }
    public ICollection<MavCommodityAllocation> CommodityAllocations { get; set; } = new List<MavCommodityAllocation>();
    public ICollection<MavApplication> Applications { get; set; } = new List<MavApplication>();
}
