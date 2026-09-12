using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class DriverProfileHistory : BaseEntity
{
    public long DriverProfileId { get; set; }
    public long? ChangedByUserId { get; set; }
    public string ChangeDescription { get; set; } = string.Empty;

    public DriverProfile DriverProfile { get; set; } = null!;
    public User? ChangedBy { get; set; }
}
