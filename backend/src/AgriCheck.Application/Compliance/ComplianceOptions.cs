namespace AgriCheck.Application.Compliance;

public class ComplianceOptions
{
    public const string SectionName = "Compliance";

    /// <summary>
    /// When true, entry submit enforces all implemented gates (beyond accreditation).
    /// </summary>
    public bool StrictEntrySubmitGates { get; set; }

    /// <summary>
    /// RSAS registrations stay Submitted until DA verifies (recommended for production).
    /// </summary>
    public bool RequireDaRsasVerification { get; set; } = true;

    /// <summary>
    /// When false, client self-serve CSW registration is blocked (agency/DA records CSW).
    /// </summary>
    public bool AllowClientCswSelfRegistration { get; set; }

    /// <summary>
    /// Block SPS submit when HS-derived issuer does not match entry bureau.
    /// </summary>
    public bool EnforceSpsEntryAgencyMatch { get; set; } = true;

    /// <summary>
    /// Require RSAS+CSW on final warehouse before starting or submitting SPSIC.
    /// </summary>
    public bool RequireDestinationForSpsic { get; set; } = true;

    /// <summary>
    /// Enforce SPSIC + e-RFI + destination match when receiving import containers at CSW.
    /// </summary>
    public bool RequireCswHaulCompliance { get; set; } = true;

    /// <summary>
    /// Validate warehouse destination (scope, segregation, contract/facility capacity) on save and submit.
    /// </summary>
    public bool EnforceEntryDestinationValidation { get; set; } = true;

    /// <summary>
    /// Block draft save when destination validation fails (when enforcement is enabled).
    /// </summary>
    public bool BlockEntrySaveOnDestinationValidation { get; set; } = true;

    /// <summary>
    /// Require an active importer–warehouse capacity allocation record.
    /// </summary>
    public bool RequireImporterWarehouseAllocation { get; set; } = true;
}
