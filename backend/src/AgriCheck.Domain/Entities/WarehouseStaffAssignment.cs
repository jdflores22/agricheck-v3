using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class WarehouseStaffAssignment : BaseEntity
{
    public long UserId { get; set; }
    public long WarehouseFacilityId { get; set; }
    public WarehouseStaffAssignmentRole Role { get; set; } = WarehouseStaffAssignmentRole.ComplianceAdmin;

    public User User { get; set; } = null!;
    public WarehouseFacility WarehouseFacility { get; set; } = null!;
}
