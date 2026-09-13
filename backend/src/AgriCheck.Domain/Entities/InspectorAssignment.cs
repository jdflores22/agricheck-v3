using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class InspectorAssignment : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long ContainerId { get; set; }
    public long AgencyId { get; set; }
    public long InspectorUserId { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Container Container { get; set; } = null!;
    public Agency Agency { get; set; } = null!;
    public User Inspector { get; set; } = null!;
}
