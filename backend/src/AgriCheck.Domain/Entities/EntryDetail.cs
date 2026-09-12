using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class EntryDetail : BaseEntity
{
    public long EntryId { get; set; }
    public long? CommodityId { get; set; }
    public string? CommodityName { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public string? OriginCountry { get; set; }
    public string? DestinationCountry { get; set; }
    public string? PortOfEntry { get; set; }

    public Entry Entry { get; set; } = null!;
    public Commodity? Commodity { get; set; }
}
