using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class AgencyLicenseApplication : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public long AgencyId { get; set; }
    public long AgencyLicenseTypeId { get; set; }
    public AgencyLicenseApplicationStatus Status { get; set; } = AgencyLicenseApplicationStatus.Draft;
    public string? FormDataJson { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? ReviewComments { get; set; }

    public User User { get; set; } = null!;
    public Agency Agency { get; set; } = null!;
    public AgencyLicenseType LicenseType { get; set; } = null!;
    public AgencyLicense? IssuedLicense { get; set; }
}
