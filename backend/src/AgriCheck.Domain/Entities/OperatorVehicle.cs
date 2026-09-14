using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class OperatorVehicle : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long OperatorUserId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? DefaultDriverUserId { get; set; }
    public bool IsActive { get; set; } = true;

    public User OperatorUser { get; set; } = null!;
    public User? DefaultDriver { get; set; }
}
