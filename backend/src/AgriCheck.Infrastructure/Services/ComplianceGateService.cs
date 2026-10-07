using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgriCheck.Infrastructure.Services;

public class ComplianceGateService : IComplianceGateService
{
    private readonly AgriCheckDbContext _db;
    private readonly IAgencyLicenseService _agencyLicenses;
    private readonly IFacilityComplianceService _facilityCompliance;
    private readonly IEntryDestinationValidationService _destinationValidation;
    private readonly ComplianceOptions _options;

    public ComplianceGateService(
        AgriCheckDbContext db,
        IAgencyLicenseService agencyLicenses,
        IFacilityComplianceService facilityCompliance,
        IEntryDestinationValidationService destinationValidation,
        IOptions<ComplianceOptions> options)
    {
        _db = db;
        _agencyLicenses = agencyLicenses;
        _facilityCompliance = facilityCompliance;
        _destinationValidation = destinationValidation;
        _options = options.Value;
    }

    public async Task<ComplianceEvaluationResult> EvaluateAsync(ComplianceGateContext context, CancellationToken cancellationToken = default)
    {
        var blockers = new List<ComplianceBlockerDto>();
        var warnings = new List<ComplianceBlockerDto>();

        if (RequiresAccreditation(context.Phase))
        {
            var accreditation = await GetAccreditationStatusAsync(context.UserId, cancellationToken);
            if (accreditation is not "Approved")
            {
                blockers.Add(new ComplianceBlockerDto(
                    "COMPLIANCE_ACCREDITATION",
                    "Approved DA importer accreditation is required.",
                    "Accreditation"));
            }
        }

        if (context.Phase is ComplianceGatePhase.EntrySubmit && _options.StrictEntrySubmitGates
            && context.FinalWarehouseFacilityId is null)
        {
            blockers.Add(new ComplianceBlockerDto(
                "COMPLIANCE_DESTINATION_FACILITY",
                "A final warehouse facility is required before submitting this entry.",
                "DestinationFacility"));
        }
        else if (context.Phase is ComplianceGatePhase.EntrySubmit && context.FinalWarehouseFacilityId is null)
        {
            warnings.Add(new ComplianceBlockerDto(
                "COMPLIANCE_DESTINATION_FACILITY",
                "Link a final warehouse facility before applying for SPS Import Clearance.",
                "DestinationFacility"));
        }

        if (context.Phase is ComplianceGatePhase.SpsicApplicationStart)
        {
            var hasLicense = await _agencyLicenses.HasActiveSpsicPrerequisiteLicenseAsync(context.UserId, context.AgencyId, cancellationToken);
            if (!hasLicense)
            {
                blockers.Add(new ComplianceBlockerDto(
                    "COMPLIANCE_AGENCY_LICENSE",
                    "An active bureau license (LTO or LTI) is required before starting SPS Import Clearance.",
                    "AgencyLicense"));
            }

            var hsCode = await _db.MicUtilizations
                .AsNoTracking()
                .Where(u => u.EntryId == context.EntryId)
                .OrderByDescending(u => u.UtilizedAt)
                .Select(u => u.Mic.HsCode)
                .FirstOrDefaultAsync(cancellationToken);
            if (!await _agencyLicenses.MeetsCommodityLicenseRulesAsync(
                    context.UserId,
                    context.AgencyId,
                    context.CommodityId,
                    hsCode,
                    cancellationToken))
            {
                blockers.Add(new ComplianceBlockerDto(
                    "COMPLIANCE_COMMODITY_LICENSE",
                    "An active bureau license for this commodity/HS line is required before SPSIC.",
                    "AgencyLicense"));
            }

            if (_options.RequireDestinationForSpsic)
            {
                if (context.FinalWarehouseFacilityId is null)
                {
                    blockers.Add(new ComplianceBlockerDto(
                        "COMPLIANCE_DESTINATION_FACILITY",
                        "Link a final warehouse on the entry before SPSIC (AC 06 §3(E)).",
                        "DestinationFacility"));
                }
                else if (!await _facilityCompliance.IsDestinationReadyAsync(context.FinalWarehouseFacilityId.Value, cancellationToken))
                {
                    blockers.Add(new ComplianceBlockerDto(
                        "COMPLIANCE_DESTINATION_RSAS_CSW",
                        "Final warehouse must have active RSAS registration and CSW accreditation.",
                        "DestinationFacility"));
                }
            }
        }
        else if (context.Phase is ComplianceGatePhase.EntrySubmit)
        {
            var hasLicense = await _agencyLicenses.HasActiveSpsicPrerequisiteLicenseAsync(context.UserId, context.AgencyId, cancellationToken);
            if (!hasLicense)
            {
                warnings.Add(new ComplianceBlockerDto(
                    "COMPLIANCE_AGENCY_LICENSE",
                    "Record an active bureau license (LTO/LTI) for this agency before applying for SPSIC.",
                    "AgencyLicense"));
            }

            if (context.FinalWarehouseFacilityId is long warehouseId)
            {
                var destinationReady = await _facilityCompliance.IsDestinationReadyAsync(warehouseId, cancellationToken);
                if (!destinationReady)
                {
                    warnings.Add(new ComplianceBlockerDto(
                        "COMPLIANCE_DESTINATION_RSAS_CSW",
                        "Final warehouse must have active RSAS registration and CSW accreditation (DA MC 03 / AC 06 §3(E)).",
                        "DestinationFacility"));
                }

                await AppendDestinationValidationAsync(context, warehouseId, blockers, warnings, cancellationToken);
            }
        }

        return new ComplianceEvaluationResult(blockers.Count == 0, blockers, warnings);
    }

    public async Task RequireAsync(ComplianceGateContext context, CancellationToken cancellationToken = default)
    {
        var result = await EvaluateAsync(context, cancellationToken);
        if (!result.IsCompliant && result.Blockers.Count > 0)
        {
            var first = result.Blockers[0];
            throw new ClientPortalException(first.Code, first.Message);
        }
    }

    private static bool RequiresAccreditation(ComplianceGatePhase phase) =>
        phase is ComplianceGatePhase.EntryCreate or ComplianceGatePhase.EntrySubmit;

    private async Task AppendDestinationValidationAsync(
        ComplianceGateContext context,
        long warehouseId,
        List<ComplianceBlockerDto> blockers,
        List<ComplianceBlockerDto> warnings,
        CancellationToken cancellationToken)
    {
        if (!_options.EnforceEntryDestinationValidation)
        {
            return;
        }

        var entry = await _db.Entries.AsNoTracking()
            .Include(e => e.Detail)
            .FirstOrDefaultAsync(e => e.Id == context.EntryId, cancellationToken);

        if (entry is null)
        {
            return;
        }

        var shipmentKg = ShipmentQuantityHelper.ToKg(entry.Detail?.Quantity ?? 0, entry.Detail?.Unit);
        if (shipmentKg <= 0)
        {
            return;
        }

        var validation = await _destinationValidation.ValidateForImporterAsync(
            context.UserId,
            new EntryDestinationValidationRequest(
                warehouseId,
                shipmentKg,
                "kg",
                entry.Uuid,
                entry.FormDataJson,
                context.AgencyId),
            cancellationToken);

        foreach (var check in validation.Checks.Where(c => c.Severity is "error" or "warning"))
        {
            var dto = new ComplianceBlockerDto(check.Code, check.Message, "DestinationFacility");
            if (check.Severity == "error" && (_options.StrictEntrySubmitGates || _options.BlockEntrySaveOnDestinationValidation))
            {
                blockers.Add(dto);
            }
            else if (check.Severity == "warning")
            {
                warnings.Add(dto);
            }
            else if (check.Severity == "error")
            {
                warnings.Add(dto);
            }
        }
    }

    private async Task<string> GetAccreditationStatusAsync(long userId, CancellationToken cancellationToken)
    {
        var latest = await _db.AccreditationSubmissions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return latest == default ? "None" : latest.ToString();
    }
}
