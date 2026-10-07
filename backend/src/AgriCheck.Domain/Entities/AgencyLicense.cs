using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class AgencyLicense : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public long AgencyId { get; set; }
    public long AgencyLicenseTypeId { get; set; }
    public long? ApplicationId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public AgencyLicenseStatus Status { get; set; } = AgencyLicenseStatus.Active;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
    public Agency Agency { get; set; } = null!;
    public AgencyLicenseType LicenseType { get; set; } = null!;
    public AgencyLicenseApplication? Application { get; set; }
}
