using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class AuditLog : BaseEntity
{
    public long? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? PayloadJson { get; set; }
    public string? IpAddress { get; set; }

    public User? ActorUser { get; set; }
}
