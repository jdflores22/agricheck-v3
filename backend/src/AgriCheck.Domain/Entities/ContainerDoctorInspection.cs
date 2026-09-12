using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class ContainerDoctorInspection : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long ContainerId { get; set; }
    public long DoctorUserId { get; set; }
    public DoctorInspectionStatus Status { get; set; } = DoctorInspectionStatus.Pending;
    public string? Findings { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Container Container { get; set; } = null!;
    public User Doctor { get; set; } = null!;
}
