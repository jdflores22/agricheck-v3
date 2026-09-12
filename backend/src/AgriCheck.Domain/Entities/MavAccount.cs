using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class MavAccount : BaseEntity
{
    public long LicenseId { get; set; }
    public decimal AwardedVolume { get; set; }
    public decimal UtilizedVolume { get; set; }
    public DateTime? LastTransactionAt { get; set; }

    public MavLicense License { get; set; } = null!;
    public ICollection<MavAccountTransaction> Transactions { get; set; } = new List<MavAccountTransaction>();
    public ICollection<MavImportCertificate> ImportCertificates { get; set; } = new List<MavImportCertificate>();
}
