using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class EvaluatorAssignment : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EntryId { get; set; }
    public long AgencyId { get; set; }
    public long EvaluatorUserId { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Entry Entry { get; set; } = null!;
    public Agency Agency { get; set; } = null!;
    public User Evaluator { get; set; } = null!;
}
