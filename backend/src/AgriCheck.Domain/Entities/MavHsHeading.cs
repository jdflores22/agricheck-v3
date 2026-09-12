using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class MavHsHeading : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long CategoryId { get; set; }
    public string HeadingNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public MavHsCategory Category { get; set; } = null!;
    public ICollection<MavHsDetail> Details { get; set; } = new List<MavHsDetail>();
}
