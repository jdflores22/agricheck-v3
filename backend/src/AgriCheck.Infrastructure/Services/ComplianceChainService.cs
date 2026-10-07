using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class ComplianceChainService : IComplianceChainService
{
    private readonly AgriCheckDbContext _db;
    private readonly IComplianceGateService _gateService;
    private readonly IAgencyLicenseService _agencyLicenses;
    private readonly IFacilityComplianceService _facilityCompliance;
    private readonly ICurrentUserService _currentUser;

    public ComplianceChainService(
        AgriCheckDbContext db,
        IComplianceGateService gateService,
        IAgencyLicenseService agencyLicenses,
        IFacilityComplianceService facilityCompliance,
        ICurrentUserService currentUser)
    {
        _db = db;
        _gateService = gateService;
        _agencyLicenses = agencyLicenses;
        _facilityCompliance = facilityCompliance;
        _currentUser = currentUser;
    }

    public async Task<EntryComplianceChainDto?> GetForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserUuid is null)
        {
            throw new ClientPortalException("UNAUTHORIZED", "Authentication required.");
        }

        var userId = await _db.Users
            .Where(u => u.Uuid == _currentUser.UserUuid)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (userId == 0)
        {
            throw new ClientPortalException("USER_NOT_FOUND", "User not found.");
        }

        var entry = await LoadEntryGraphAsync(entryUuid, userId, null, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        return await BuildChainAsync(entry, userId, cancellationToken);
    }

    public async Task<EntryComplianceChainDto?> GetForEntryAsAgencyAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await LoadEntryGraphAsync(entryUuid, null, agency.Id, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        return await BuildChainAsync(entry, entry.UserId, cancellationToken);
    }

    private async Task<Domain.Entities.Entry?> LoadEntryGraphAsync(
        Guid entryUuid,
        long? ownerUserId,
        long? agencyId,
        CancellationToken cancellationToken)
    {
        var query = _db.Entries
            .AsNoTracking()
            .Include(e => e.Agency)
            .Include(e => e.FinalWarehouseFacility)
            .Include(e => e.Detail)
            .Where(e => e.Uuid == entryUuid);

        if (ownerUserId is long uid)
        {
            query = query.Where(e => e.UserId == uid);
        }

        if (agencyId is long aid)
        {
            query = query.Where(e => e.AgencyId == aid);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<EntryComplianceChainDto> BuildChainAsync(
        Domain.Entities.Entry entry,
        long licenseUserId,
        CancellationToken cancellationToken)
    {
        var accreditationStatus = await _db.AccreditationSubmissions
            .Where(s => s.UserId == licenseUserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.Status)
            .FirstOrDefaultAsync(cancellationToken);

        var accreditationLayer = new ComplianceLayerDto(
            "Accreditation",
            accreditationStatus == AccreditationSubmissionStatus.Approved ? "Complete" : "Incomplete",
            accreditationStatus == AccreditationSubmissionStatus.Approved
                ? "DA importer accreditation approved."
                : "Submit and obtain approved DA accreditation.");

        var activeLicense = await _agencyLicenses.GetPrimaryActiveLicenseAsync(licenseUserId, entry.AgencyId, cancellationToken);
        var agencyLicenseLayer = activeLicense is not null
            ? new ComplianceLayerDto(
                "AgencyLicense",
                "Complete",
                $"{activeLicense.LicenseTypeCode} {activeLicense.LicenseNumber} valid until {activeLicense.ExpiresAt:yyyy-MM-dd}.")
            : new ComplianceLayerDto(
                "AgencyLicense",
                "Incomplete",
                $"Active bureau license (LTO/LTI) for {entry.Agency.Code} is required for SPSIC.");

        var destinationLayer = await BuildDestinationLayerAsync(entry, cancellationToken);

        var spsicLayer = await BuildSpsicLayerAsync(entry, cancellationToken);

        var erfiLayer = await BuildErfiLayerAsync(entry.Id, cancellationToken);

        var mavLayer = BuildMavLayer(entry);

        var gateResult = await _gateService.EvaluateAsync(
            new ComplianceGateContext(
                licenseUserId,
                entry.Id,
                entry.AgencyId,
                ComplianceGatePhase.EntrySubmit,
                entry.FinalWarehouseFacilityId,
                entry.Detail?.CommodityId),
            cancellationToken);

        var blockers = gateResult.Blockers.ToList();
        var warnings = gateResult.Warnings.ToList();

        if (entry.FinalWarehouseFacilityId is null && entry.EntryType == EntryType.Import)
        {
            warnings.Add(new ComplianceBlockerDto(
                "COMPLIANCE_DESTINATION_FACILITY",
                "Link a final cold/dry warehouse before SPSIC (AC 06 §3(E)).",
                "DestinationFacility"));
        }

        var nextAction = ResolveNextAction(entry, accreditationStatus, blockers, warnings);

        return new EntryComplianceChainDto(
            entry.Uuid,
            entry.ReferenceNo,
            entry.Status.ToString(),
            entry.CompliancePhase.ToString(),
            entry.AgencyId,
            entry.Agency.Code,
            accreditationLayer,
            agencyLicenseLayer,
            destinationLayer,
            spsicLayer,
            erfiLayer,
            mavLayer,
            blockers,
            warnings,
            nextAction);
    }

    private async Task<ComplianceLayerDto> BuildErfiLayerAsync(long entryId, CancellationToken cancellationToken)
    {
        var erfi = await _db.ErfiSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EntryId == entryId, cancellationToken);

        if (erfi is null)
        {
            return new ComplianceLayerDto(
                "Erfi",
                "NotStarted",
                "File e-RFI at least 48 hours before port arrival after SPSIC is issued.");
        }

        if (erfi.Status == ErfiSubmissionStatus.Filed)
        {
            return new ComplianceLayerDto(
                "Erfi",
                "Complete",
                $"e-RFI filed {erfi.FiledAt:yyyy-MM-dd HH:mm} UTC.",
                erfi.Uuid.ToString());
        }

        return new ComplianceLayerDto(
            "Erfi",
            "InProgress",
            "e-RFI draft saved — file before arrival.",
            erfi.Uuid.ToString());
    }

    private async Task<ComplianceLayerDto> BuildSpsicLayerAsync(Domain.Entities.Entry entry, CancellationToken cancellationToken)
    {
        var application = await _db.SpsImportClearanceApplications
            .AsNoTracking()
            .Include(a => a.IssuedClearance)
            .FirstOrDefaultAsync(a => a.EntryId == entry.Id, cancellationToken);

        if (application?.IssuedClearance is not null)
        {
            var c = application.IssuedClearance;
            return new ComplianceLayerDto(
                "SpsImportClearance",
                "Complete",
                $"SPSIC {c.ClearanceNumber} issued {c.IssuedAt:yyyy-MM-dd}.",
                c.ClearanceNumber);
        }

        if (application is not null)
        {
            return new ComplianceLayerDto(
                "SpsImportClearance",
                application.Status == SpsImportClearanceApplicationStatus.Draft ? "InProgress" : "Incomplete",
                $"SPS application {application.Status}.",
                application.Uuid.ToString());
        }

        return new ComplianceLayerDto(
            "SpsImportClearance",
            "NotStarted",
            "Start SPS Import Clearance application when entry prerequisites are met.");
    }

    private async Task<ComplianceLayerDto> BuildDestinationLayerAsync(Domain.Entities.Entry entry, CancellationToken cancellationToken)
    {
        if (entry.FinalWarehouseFacilityId is null)
        {
            return new ComplianceLayerDto(
                "DestinationFacility",
                "NotStarted",
                "No final warehouse linked on this entry.");
        }

        var facility = entry.FinalWarehouseFacility;
        var label = facility is null
            ? $"Facility #{entry.FinalWarehouseFacilityId}"
            : $"{facility.Code} — {facility.Name}";

        var dest = await _facilityCompliance.GetDestinationStatusAsync(entry.FinalWarehouseFacilityId.Value, cancellationToken);
        if (dest.HasRsasRegistration && dest.HasActiveCswAccreditation)
        {
            return new ComplianceLayerDto(
                "DestinationFacility",
                "Complete",
                $"{label} — RSAS {dest.RsasExternalRef}, CSW {dest.CswCertificateNumber}.",
                facility?.Code);
        }

        var parts = new List<string> { label };
        if (!dest.HasRsasRegistration)
        {
            parts.Add($"RSAS: {dest.RsasStatus}");
        }

        if (!dest.HasActiveCswAccreditation)
        {
            parts.Add($"CSW: {dest.CswStatus}");
        }

        return new ComplianceLayerDto(
            "DestinationFacility",
            "Incomplete",
            string.Join(" · ", parts),
            facility?.Code);
    }

    private static ComplianceLayerDto BuildMavLayer(Domain.Entities.Entry entry)
    {
        if (entry.EntryType != EntryType.Import || entry.ImportTrack != EntryImportTrack.Mav)
        {
            return new ComplianceLayerDto("Mav", "NotApplicable", "Regular import track (no MAV MIC).");
        }

        var summary = string.IsNullOrWhiteSpace(entry.MavNo)
            ? "MAV track — MAV number not set."
            : $"MAV {entry.MavNo}, document status {entry.MavDocumentStatus}.";

        return new ComplianceLayerDto(
            "Mav",
            string.IsNullOrWhiteSpace(entry.MavNo) ? "Incomplete" : "InProgress",
            summary,
            entry.MavNo);
    }

    private static string? ResolveNextAction(
        Domain.Entities.Entry entry,
        AccreditationSubmissionStatus accreditationStatus,
        List<ComplianceBlockerDto> blockers,
        List<ComplianceBlockerDto> warnings)
    {
        if (blockers.Count > 0)
        {
            return blockers[0].Message;
        }

        if (accreditationStatus != AccreditationSubmissionStatus.Approved)
        {
            return "Complete DA accreditation.";
        }

        if (entry.Status == EntryStatus.Draft)
        {
            return "Submit entry to the agency when the draft is complete.";
        }

        if (entry.FinalWarehouseFacilityId is null && entry.EntryType == EntryType.Import)
        {
            return "Set final warehouse facility on the entry.";
        }

        if (warnings.Count > 0)
        {
            return warnings[0].Message;
        }

        return "Apply for bureau license and SPS Import Clearance (upcoming modules).";
    }
}
