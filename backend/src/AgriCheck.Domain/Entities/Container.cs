using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class Container : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EntryId { get; set; }
    public int SequenceNumber { get; set; }
    public string ContainerNumber { get; set; } = string.Empty;
    public string? ContainerType { get; set; }
    public string? FormDataJson { get; set; }
    public ContainerStatus Status { get; set; } = ContainerStatus.Pending;
    public long? ClaimedByUserId { get; set; }
    public long? AssignedDriverUserId { get; set; }
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }

    public Entry Entry { get; set; } = null!;
    public User? ClaimedBy { get; set; }
    public User? AssignedDriver { get; set; }
    public ICollection<ContainerLocation> Locations { get; set; } = new List<ContainerLocation>();
    public ICollection<WarehouseInventory> Inventories { get; set; } = new List<WarehouseInventory>();
    public ICollection<ContainerInspectionPhoto> InspectionPhotos { get; set; } = new List<ContainerInspectionPhoto>();
    public ICollection<ContainerTransportTag> TransportTags { get; set; } = new List<ContainerTransportTag>();
    public ICollection<ContainerDoctorInspection> DoctorInspections { get; set; } = new List<ContainerDoctorInspection>();
}
