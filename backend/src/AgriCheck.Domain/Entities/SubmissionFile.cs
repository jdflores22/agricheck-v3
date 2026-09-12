using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class SubmissionFile : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long SubmissionId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public AccreditationSubmission Submission { get; set; } = null!;
    public ICollection<SubmissionFileReview> Reviews { get; set; } = new List<SubmissionFileReview>();
    public ICollection<SubmissionFileVersion> Versions { get; set; } = new List<SubmissionFileVersion>();
}
