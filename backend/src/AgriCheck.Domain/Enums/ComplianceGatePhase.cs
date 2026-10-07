namespace AgriCheck.Domain.Enums;

public enum ComplianceGatePhase
{
    EntryCreate = 0,
    EntrySubmit = 1,
    SpsicApplicationStart = 2,
    SpsicIssue = 3,
    ErfiFile = 4,
    WarehouseBooking = 5,
    ContainerDispatchToCsw = 6
}
