using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class CommodityCategory : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Commodity> Commodities { get; set; } = new List<Commodity>();
}
