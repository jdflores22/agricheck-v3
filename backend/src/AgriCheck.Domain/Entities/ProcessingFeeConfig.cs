using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class ProcessingFeeConfig : BaseEntity
{
    public long AgencyId { get; set; }
    public EntryType EntryType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PHP";
    public bool IsActive { get; set; } = true;

    public Agency Agency { get; set; } = null!;
}
