using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class ComplianceSyncOutbox : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public ComplianceSyncTargetSystem TargetSystem { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public ComplianceSyncOutboxStatus Status { get; set; } = ComplianceSyncOutboxStatus.Pending;
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTime? SyncedAt { get; set; }
}
