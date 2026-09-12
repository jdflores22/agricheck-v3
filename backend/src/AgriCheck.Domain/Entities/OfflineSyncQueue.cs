using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class OfflineSyncQueue : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public OfflineSyncStatus Status { get; set; } = OfflineSyncStatus.Pending;
    public DateTime? SyncedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public User User { get; set; } = null!;
}
