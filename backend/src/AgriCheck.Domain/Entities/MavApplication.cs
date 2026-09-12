using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class MavApplication : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string ReferenceNumber { get; set; } = string.Empty;
    public long ApplicationPeriodId { get; set; }
    public long ImporterId { get; set; }
    public long? AccreditationSubmissionId { get; set; }
    public long? MavHsDetailId { get; set; }
    public string HsCode { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public decimal RequestedVolume { get; set; }
    public decimal? AllocatedVolume { get; set; }
    public MavApplicationStatus Status { get; set; } = MavApplicationStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public string? DocumentsJson { get; set; }

    public MavApplicationPeriod ApplicationPeriod { get; set; } = null!;
    public User Importer { get; set; } = null!;
    public User? ReviewedBy { get; set; }
    public MavHsDetail? MavHsDetail { get; set; }
    public MavLicense? License { get; set; }
}
