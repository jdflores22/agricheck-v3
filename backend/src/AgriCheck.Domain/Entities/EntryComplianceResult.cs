using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class EntryComplianceResult : BaseEntity
{
    public long EntryId { get; set; }
    public long ChecklistItemId { get; set; }
    public ComplianceResultStatus Status { get; set; } = ComplianceResultStatus.Pending;
    public string? Notes { get; set; }
    public long? EvaluatedByUserId { get; set; }

    public Entry Entry { get; set; } = null!;
    public ComplianceChecklistItem ChecklistItem { get; set; } = null!;
    public User? EvaluatedBy { get; set; }
}
