using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class DeliveryComplianceEvent : BaseEntity
{
    public long ContainerId { get; set; }
    public long? DriverUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Details { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Container Container { get; set; } = null!;
    public User? DriverUser { get; set; }
}
