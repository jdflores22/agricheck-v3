using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class ContainerTransportTag : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long ContainerId { get; set; }
    public long EntryId { get; set; }
    public string TransportType { get; set; } = "warehouse_nmis";
    public long TaggedByUserId { get; set; }
    public DateTime TaggedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledWarehouseDate { get; set; }
    public string? QrPayload { get; set; }
    public string? QrCodeData { get; set; }

    public Container Container { get; set; } = null!;
    public Entry Entry { get; set; } = null!;
    public User TaggedBy { get; set; } = null!;
}
