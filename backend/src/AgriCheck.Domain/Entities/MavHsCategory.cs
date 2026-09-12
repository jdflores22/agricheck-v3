using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class MavHsCategory : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string HsCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public long? AgencyId { get; set; }
    public bool IsActive { get; set; } = true;

    public Agency? Agency { get; set; }
    public ICollection<MavHsHeading> Headings { get; set; } = new List<MavHsHeading>();
}
