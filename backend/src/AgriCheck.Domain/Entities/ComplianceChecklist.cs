using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class ComplianceChecklist : BaseEntity
{
    public long AgencyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Agency Agency { get; set; } = null!;
    public ICollection<ComplianceChecklistItem> Items { get; set; } = new List<ComplianceChecklistItem>();
}
