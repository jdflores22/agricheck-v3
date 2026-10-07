namespace AgriCheck.Domain.Enums;

/// <summary>
/// Integrated import-case compliance spine (P8). Advances as gates pass.
/// </summary>
public enum EntryCompliancePhase
{
    Draft = 0,
    AccreditationReady = 1,
    AgencyLicensed = 2,
    DestinationReady = 3,
    SpsicUnderReview = 4,
    SpsicIssued = 5,
    ErfiFiled = 6,
    PortCleared = 7,
    AtWarehouse = 8,
    Released = 9
}
