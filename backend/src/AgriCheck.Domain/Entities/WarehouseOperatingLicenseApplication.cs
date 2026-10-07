using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class WarehouseOperatingLicenseApplication : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long WarehouseFacilityId { get; set; }
    public long SubmittedByUserId { get; set; }
    public string OperatorLegalName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Notes { get; set; }
    public WarehouseOperatingLicenseApplicationStatus Status { get; set; } = WarehouseOperatingLicenseApplicationStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public long? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComments { get; set; }
    public long? IssuedLicenseId { get; set; }

    public WarehouseFacility WarehouseFacility { get; set; } = null!;
    public User SubmittedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public WarehouseOperatingLicense? IssuedLicense { get; set; }
}
