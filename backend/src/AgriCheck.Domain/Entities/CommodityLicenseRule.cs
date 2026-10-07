using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class CommodityLicenseRule : BaseEntity
{
    public long AgencyId { get; set; }
    public long AgencyLicenseTypeId { get; set; }
    public long? CommodityId { get; set; }
    public string? HsCodePrefix { get; set; }
    public bool IsRequired { get; set; } = true;

    public Agency Agency { get; set; } = null!;
    public AgencyLicenseType LicenseType { get; set; } = null!;
    public Commodity? Commodity { get; set; }
}
