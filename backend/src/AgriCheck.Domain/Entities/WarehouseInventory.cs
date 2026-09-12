using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class WarehouseInventory : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long ContainerId { get; set; }
    public long WarehouseFacilityId { get; set; }
    public string? LocationCode { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public long ReceivedByUserId { get; set; }
    public WarehouseInventoryStatus Status { get; set; } = WarehouseInventoryStatus.Stored;

    public Container Container { get; set; } = null!;
    public WarehouseFacility WarehouseFacility { get; set; } = null!;
    public User ReceivedBy { get; set; } = null!;
    public ICollection<ReleaseRecord> ReleaseRecords { get; set; } = new List<ReleaseRecord>();
}
