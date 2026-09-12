using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class SubmissionFileReview : BaseEntity
{
    public long SubmissionFileId { get; set; }
    public long ReviewerUserId { get; set; }
    public EvaluationDecision Decision { get; set; } = EvaluationDecision.Pending;
    public string? Comment { get; set; }

    public SubmissionFile SubmissionFile { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
}
