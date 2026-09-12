using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class ComplianceChecklistItem : BaseEntity
{
    public long ChecklistId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;

    public ComplianceChecklist Checklist { get; set; } = null!;
    public ICollection<EntryComplianceResult> Results { get; set; } = new List<EntryComplianceResult>();
}
