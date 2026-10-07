using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class AgencyLicenseType : BaseEntity
{
    public long AgencyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AgencyLicenseKind Kind { get; set; }
    public bool IsRequiredForSpsic { get; set; } = true;
    public string? Description { get; set; }

    public Agency Agency { get; set; } = null!;
    public ICollection<AgencyLicense> Licenses { get; set; } = new List<AgencyLicense>();
    public ICollection<AgencyLicenseApplication> Applications { get; set; } = new List<AgencyLicenseApplication>();
    public ICollection<CommodityLicenseRule> CommodityRules { get; set; } = new List<CommodityLicenseRule>();
}
