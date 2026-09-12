using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class DriverDocument : BaseEntity
{
    public long DriverProfileId { get; set; }
    public DriverDocumentType DocumentType { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;

    public DriverProfile DriverProfile { get; set; } = null!;
}
