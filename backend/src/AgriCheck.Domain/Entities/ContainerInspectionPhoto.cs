using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class ContainerInspectionPhoto : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long ContainerId { get; set; }
    public long EntryId { get; set; }
    public ContainerInspectionPhotoType PhotoType { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public EvaluationDecision ReviewDecision { get; set; } = EvaluationDecision.Pending;
    public string? ReviewComment { get; set; }
    public long? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Container Container { get; set; } = null!;
    public Entry Entry { get; set; } = null!;
    public User? ReviewedBy { get; set; }
}
