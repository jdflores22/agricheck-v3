using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class StorageTallySheet : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long WarehouseFacilityId { get; set; }
    public long ContainerId { get; set; }
    public long EntryId { get; set; }
    public string Direction { get; set; } = "In";
    public decimal? VolumeKg { get; set; }
    public string? CommodityDescription { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public long RecordedByUserId { get; set; }

    public WarehouseFacility WarehouseFacility { get; set; } = null!;
    public Container Container { get; set; } = null!;
    public Entry Entry { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}
