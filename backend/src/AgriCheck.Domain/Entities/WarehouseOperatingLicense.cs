using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

/// <summary>
/// DA-issued warehouse operating license (LTO) — mother-unit gate before a facility is selectable for import.
/// </summary>
public class WarehouseOperatingLicense : BaseEntity
{
    public long WarehouseFacilityId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public WarehouseOperatingLicenseStatus Status { get; set; } = WarehouseOperatingLicenseStatus.Active;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public long IssuedByUserId { get; set; }
    public string? Notes { get; set; }

    public WarehouseFacility WarehouseFacility { get; set; } = null!;
    public User IssuedByUser { get; set; } = null!;
}
