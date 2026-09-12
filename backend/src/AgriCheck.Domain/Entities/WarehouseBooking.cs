using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class WarehouseBooking : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string BookingNumber { get; set; } = string.Empty;
    public long UserId { get; set; }
    public long WarehouseFacilityId { get; set; }
    public WarehouseBookingStatus Status { get; set; } = WarehouseBookingStatus.Pending;
    public string ContainerReference { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string? Notes { get; set; }
    public decimal? Amount { get; set; }

    public User User { get; set; } = null!;
    public WarehouseFacility WarehouseFacility { get; set; } = null!;
}
