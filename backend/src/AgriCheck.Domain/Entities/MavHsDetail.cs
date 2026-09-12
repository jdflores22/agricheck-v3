using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class MavHsDetail : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long HeadingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public MavHsHeading Heading { get; set; } = null!;
}
