using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class MavImportCertificate : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string CertificateNumber { get; set; } = string.Empty;
    public long LicenseId { get; set; }
    public long AccountId { get; set; }
    public long ImporterId { get; set; }
    public string HsCode { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public decimal AuthorizedVolume { get; set; }
    public decimal UtilizedVolume { get; set; }
    public MavImportCertificateStatus Status { get; set; } = MavImportCertificateStatus.Active;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public MavLicense License { get; set; } = null!;
    public MavAccount Account { get; set; } = null!;
    public User Importer { get; set; } = null!;
    public ICollection<MicUtilization> Utilizations { get; set; } = new List<MicUtilization>();
}
