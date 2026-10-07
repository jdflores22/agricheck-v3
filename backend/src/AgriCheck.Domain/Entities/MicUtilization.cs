using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class MicUtilization : BaseEntity
{
    public long MicId { get; set; }
    public long EntryId { get; set; }
    public decimal Volume { get; set; }
    public DateTime UtilizedAt { get; set; } = DateTime.UtcNow;
    public long? SpsImportClearanceId { get; set; }

    public MavImportCertificate Mic { get; set; } = null!;
    public SpsImportClearance? SpsImportClearance { get; set; }
    public Entry Entry { get; set; } = null!;
}
