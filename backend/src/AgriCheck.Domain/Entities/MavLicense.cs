using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class MavLicense : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string LicenseNumber { get; set; } = string.Empty;
    public long ApplicationId { get; set; }
    public long ImporterId { get; set; }
    public int MavYear { get; set; }
    public string HsCode { get; set; } = string.Empty;
    public string CommodityName { get; set; } = string.Empty;
    public decimal AwardedVolume { get; set; }
    public MavPoolType PoolType { get; set; }
    public MavLicenseStatus Status { get; set; } = MavLicenseStatus.Active;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string? RevokedReason { get; set; }

    public MavApplication Application { get; set; } = null!;
    public User Importer { get; set; } = null!;
    public MavAccount? Account { get; set; }
    public ICollection<MavImportCertificate> ImportCertificates { get; set; } = new List<MavImportCertificate>();
}
