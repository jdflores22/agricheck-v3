using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

/// <summary>
/// Importer storage contract / capacity allocation at a licensed warehouse (kg headroom).
/// </summary>
public class WarehouseImporterAllocation : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long ImporterUserId { get; set; }
    public long WarehouseFacilityId { get; set; }
    public string ContractReference { get; set; } = string.Empty;
    public decimal AllocatedCapacityKg { get; set; }
    /// <summary>JSON array of <see cref="WarehouseCommodityClass"/> names allowed on this contract.</summary>
    public string AllowedCommodityClassesJson { get; set; } = "[]";
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }
    public WarehouseImporterAllocationStatus Status { get; set; } = WarehouseImporterAllocationStatus.Active;

    public User ImporterUser { get; set; } = null!;
    public WarehouseFacility WarehouseFacility { get; set; } = null!;
}
