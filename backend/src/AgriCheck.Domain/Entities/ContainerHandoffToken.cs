using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class ContainerHandoffToken : BaseEntity
{
    public Guid Nonce { get; set; } = Guid.NewGuid();
    public long ContainerId { get; set; }
    public long IssuedByDriverUserId { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public long? ConsumedByUserId { get; set; }
    public string QrPayload { get; set; } = string.Empty;

    public Container Container { get; set; } = null!;
    public User IssuedByDriver { get; set; } = null!;
    public User? ConsumedBy { get; set; }
}
