using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class SpsApplicationDocument : BaseEntity
{
    public long ApplicationId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public long? EntryFileId { get; set; }
    public string? Notes { get; set; }

    public SpsImportClearanceApplication Application { get; set; } = null!;
    public EntryFile? EntryFile { get; set; }
}
