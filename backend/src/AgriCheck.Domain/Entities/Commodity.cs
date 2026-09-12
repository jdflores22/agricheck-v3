using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class Commodity : BaseEntity
{
    public long CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public CommodityCategory Category { get; set; } = null!;
}
