using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class SpsMustShipOutRule : BaseEntity
{
    public string HsCodePrefix { get; set; } = string.Empty;
    public string? CommodityKeyword { get; set; }
    public int ValidityDays { get; set; } = 60;
    public string? Notes { get; set; }
}
