using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class SubmissionFileVersion : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long SubmissionFileId { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public long? UploadedByUserId { get; set; }

    public SubmissionFile SubmissionFile { get; set; } = null!;
}
