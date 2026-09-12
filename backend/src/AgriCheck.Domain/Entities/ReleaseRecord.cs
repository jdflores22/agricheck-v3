using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class ReleaseRecord : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long WarehouseInventoryId { get; set; }
    public long ReleaseAuthorizationId { get; set; }
    public long ReleasedByUserId { get; set; }
    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
    public string? RecipientSignaturePath { get; set; }

    public WarehouseInventory WarehouseInventory { get; set; } = null!;
    public ReleaseAuthorization ReleaseAuthorization { get; set; } = null!;
    public User ReleasedBy { get; set; } = null!;
}
