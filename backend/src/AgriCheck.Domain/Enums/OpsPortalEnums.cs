namespace AgriCheck.Domain.Enums;

public enum ContainerStatus
{
    Pending,
    ReadyForTransport,
    AwaitingConfirmation,
    Assigned,
    InTransit,
    UnderInspection,
    Inspected,
    AtWarehouse,
    Released
}

public enum WarehouseInventoryStatus
{
    Stored,
    Released
}

public enum OfflineSyncStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum DriverDocumentType
{
    License,
    IdCard,
    VehicleRegistration,
    Insurance,
    ProfilePhoto
}
