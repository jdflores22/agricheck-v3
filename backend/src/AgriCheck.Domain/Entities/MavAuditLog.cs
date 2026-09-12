using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class MavAuditLog : BaseEntity
{
    public long? UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PayloadJson { get; set; }

    public User? User { get; set; }
}
