using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class SpsIssuingRule : BaseEntity
{
    public string HsCodePrefix { get; set; } = string.Empty;
    public long IssuingAgencyId { get; set; }
    public bool IsPrimaryIssuer { get; set; } = true;
    public string? Notes { get; set; }

    public Agency IssuingAgency { get; set; } = null!;
}
