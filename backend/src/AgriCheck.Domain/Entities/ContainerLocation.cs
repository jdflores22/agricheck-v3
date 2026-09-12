using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class ContainerLocation : BaseEntity
{
    public long ContainerId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Container Container { get; set; } = null!;
}
