using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class Inspection : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EntryId { get; set; }
    public long AgencyId { get; set; }
    public long InspectorUserId { get; set; }
    public InspectionStatus Status { get; set; } = InspectionStatus.Scheduled;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Findings { get; set; }

    public Entry Entry { get; set; } = null!;
    public Agency Agency { get; set; } = null!;
    public User Inspector { get; set; } = null!;
    public ICollection<InspectionPhoto> Photos { get; set; } = new List<InspectionPhoto>();
}
