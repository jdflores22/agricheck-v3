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
    public long? AssignedOperatorVehicleId { get; set; }
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    /// <summary>Driver confirmed geofence at assigned warehouse; rotating handoff QR enabled until doctor scan.</summary>
    public DateTime? WarehouseHandoffEligibleAt { get; set; }
    public string? DeliveryComplianceHoldReason { get; set; }
    public DateTime? DeliveryComplianceHoldAt { get; set; }
    public DateTime? DeliveryComplianceClearedAt { get; set; }
    public long? DeliveryComplianceClearedByUserId { get; set; }
    public string? DeliveryComplianceClearanceNotes { get; set; }
    public long? SpsImportClearanceId { get; set; }
    public long? WarehouseFacilityId { get; set; }

    public Entry Entry { get; set; } = null!;
    public WarehouseFacility? WarehouseFacility { get; set; }
    public SpsImportClearance? SpsImportClearance { get; set; }
    public User? ClaimedBy { get; set; }
    public User? AssignedDriver { get; set; }
    public OperatorVehicle? AssignedOperatorVehicle { get; set; }
    public ICollection<ContainerLocation> Locations { get; set; } = new List<ContainerLocation>();
    public ICollection<WarehouseInventory> Inventories { get; set; } = new List<WarehouseInventory>();
    public ICollection<ContainerInspectionPhoto> InspectionPhotos { get; set; } = new List<ContainerInspectionPhoto>();
    public ICollection<ContainerTransportTag> TransportTags { get; set; } = new List<ContainerTransportTag>();
    public ICollection<ContainerDoctorInspection> DoctorInspections { get; set; } = new List<ContainerDoctorInspection>();
    public ICollection<InspectorAssignment> InspectorAssignments { get; set; } = new List<InspectorAssignment>();
}
