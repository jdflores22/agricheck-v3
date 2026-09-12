using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class AccreditationSubmission : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string SubmissionType { get; set; } = "NEW";
    public AccreditationSubmissionStatus Status { get; set; } = AccreditationSubmissionStatus.Draft;
    public string CompanyName { get; set; } = string.Empty;
    public string? FormDataJson { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? ReviewComments { get; set; }
    public string? AccreditationNumber { get; set; }
    public long? AssignedOfficerUserId { get; set; }
    public DateTime? ClaimedAt { get; set; }

    public User User { get; set; } = null!;
    public User? AssignedOfficer { get; set; }
    public ICollection<SubmissionFile> Files { get; set; } = new List<SubmissionFile>();
    public ICollection<AccreditationHistory> History { get; set; } = new List<AccreditationHistory>();
}
