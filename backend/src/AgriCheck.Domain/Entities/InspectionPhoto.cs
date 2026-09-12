using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class InspectionPhoto : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long InspectionId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? Caption { get; set; }

    public Inspection Inspection { get; set; } = null!;
}
