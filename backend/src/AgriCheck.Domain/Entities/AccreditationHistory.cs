using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class AccreditationHistory : BaseEntity
{
    public long SubmissionId { get; set; }
    public AccreditationSubmissionStatus Status { get; set; }
    public string? Comment { get; set; }
    public long? ActorUserId { get; set; }

    public AccreditationSubmission Submission { get; set; } = null!;
    public User? Actor { get; set; }
}
