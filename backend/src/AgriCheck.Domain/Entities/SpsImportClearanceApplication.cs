using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class SpsImportClearanceApplication : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EntryId { get; set; }
    public long UserId { get; set; }
    public long AgencyId { get; set; }
    public long? AgencyLicenseId { get; set; }
    public SpsImportClearanceApplicationStatus Status { get; set; } = SpsImportClearanceApplicationStatus.Draft;
    public string? FormDataJson { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? ReviewComments { get; set; }
    public long? ReviewerUserId { get; set; }

    public Entry Entry { get; set; } = null!;
    public User User { get; set; } = null!;
    public Agency Agency { get; set; } = null!;
    public AgencyLicense? AgencyLicense { get; set; }
    public User? Reviewer { get; set; }
    public SpsImportClearance? IssuedClearance { get; set; }
}
