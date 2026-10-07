using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgriCheck.Infrastructure.Services;

public class EntrySpsicService : IEntrySpsicService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IComplianceGateService _complianceGate;
    private readonly IAgencyLicenseService _agencyLicenses;
    private readonly ISpsIssuingRuleService _issuingRules;
    private readonly IFacilityComplianceService _facilityCompliance;
    private readonly ComplianceOptions _options;

    public EntrySpsicService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IComplianceGateService complianceGate,
        IAgencyLicenseService agencyLicenses,
        ISpsIssuingRuleService issuingRules,
        IFacilityComplianceService facilityCompliance,
        IOptions<ComplianceOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _complianceGate = complianceGate;
        _agencyLicenses = agencyLicenses;
        _issuingRules = issuingRules;
        _facilityCompliance = facilityCompliance;
        _options = options.Value;
    }

    public async Task<EntrySpsApplicationDto?> GetForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken);
        if (entry is null) return null;

        var application = await LoadApplicationGraphAsync(entry.Id, cancellationToken);
        return application is null ? null : Map(application, entry.Uuid);
    }

    public async Task<EntrySpsApplicationDto> CreateApplicationAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries
            .Include(e => e.Detail)
            .FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.EntryType != EntryType.Import)
        {
            throw new ClientPortalException("INVALID_ENTRY_TYPE", "SPS Import Clearance applies to import entries only.");
        }

        var existing = await _db.SpsImportClearanceApplications.AnyAsync(a => a.EntryId == entry.Id, cancellationToken);
        if (existing)
        {
            throw new ClientPortalException("SPS_APPLICATION_EXISTS", "An SPS application already exists for this entry.");
        }

        await _complianceGate.RequireAsync(
            new ComplianceGateContext(
                user.Id,
                entry.Id,
                entry.AgencyId,
                ComplianceGatePhase.SpsicApplicationStart,
                entry.FinalWarehouseFacilityId,
                entry.Detail?.CommodityId),
            cancellationToken);

        var license = await _agencyLicenses.GetPrimaryActiveLicenseAsync(user.Id, entry.AgencyId, cancellationToken);
        if (license is null)
        {
            throw new ClientPortalException("COMPLIANCE_AGENCY_LICENSE", "Active bureau license is required.");
        }

        var licenseEntity = await _db.AgencyLicenses.FirstAsync(l => l.Uuid == license.Uuid, cancellationToken);

        var application = new SpsImportClearanceApplication
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            UserId = user.Id,
            AgencyId = entry.AgencyId,
            AgencyLicenseId = licenseEntity.Id,
            Status = SpsImportClearanceApplicationStatus.Draft,
        };

        _db.SpsImportClearanceApplications.Add(application);
        entry.CompliancePhase = EntryCompliancePhase.AgencyLicensed;
        await _db.SaveChangesAsync(cancellationToken);
        await SeedApplicationDocumentsAsync(application.Id, cancellationToken);
        await SeedPrerequisiteClearancesAsync(entry, application, licenseEntity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        application = await LoadApplicationGraphAsync(entry.Id, cancellationToken)
            ?? throw new ClientPortalException("SPS_CREATE_FAILED", "Failed to create SPS application.");

        return Map(application, entry.Uuid);
    }

    public async Task<EntrySpsApplicationDto> SubmitApplicationAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries
            .Include(e => e.Detail)
            .FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        var application = await _db.SpsImportClearanceApplications
            .FirstOrDefaultAsync(a => a.EntryId == entry.Id, cancellationToken)
            ?? throw new ClientPortalException("SPS_APPLICATION_NOT_FOUND", "Create an SPS application first.");

        if (application.Status != SpsImportClearanceApplicationStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "Application has already been submitted.");
        }

        var readiness = await BuildReadinessAsync(entry, application.Id, cancellationToken);
        if (!readiness.AllRequiredDocumentsReady)
        {
            throw new ClientPortalException("SPS_DOCUMENTS_INCOMPLETE", "Complete required SPS attachments (Art. III §3) before submit.");
        }

        if (_options.EnforceSpsEntryAgencyMatch && readiness.SuggestedIssuer is not null && !readiness.AgencyMatchesEntry)
        {
            throw new ClientPortalException(
                "COMPLIANCE_SPSIC_ISSUER_MISMATCH",
                $"Commodity HS maps to {readiness.SuggestedIssuer.AgencyCode}; update entry bureau or commodity classification.");
        }

        await _complianceGate.RequireAsync(
            new ComplianceGateContext(
                user.Id,
                entry.Id,
                entry.AgencyId,
                ComplianceGatePhase.SpsicApplicationStart,
                entry.FinalWarehouseFacilityId,
                entry.Detail?.CommodityId),
            cancellationToken);

        application.Status = SpsImportClearanceApplicationStatus.Submitted;
        application.SubmittedAt = DateTime.UtcNow;
        entry.CompliancePhase = EntryCompliancePhase.SpsicUnderReview;
        await _db.SaveChangesAsync(cancellationToken);

        application = await LoadApplicationGraphAsync(entry.Id, cancellationToken)!;
        return Map(application!, entry.Uuid);
    }

    public async Task<EntrySpsReadinessDto> AttachDocumentAsync(
        Guid entryUuid,
        string documentCode,
        Guid entryFileUuid,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries
            .AsNoTracking()
            .Include(e => e.Agency)
            .Include(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        var application = await _db.SpsImportClearanceApplications
            .FirstOrDefaultAsync(a => a.EntryId == entry.Id, cancellationToken)
            ?? throw new ClientPortalException("SPS_APPLICATION_NOT_FOUND", "Create an SPS application first.");

        if (application.Status != SpsImportClearanceApplicationStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "Documents can only be attached while the application is in draft.");
        }

        var code = documentCode.Trim().ToUpperInvariant();
        var templateExists = await _db.SpsDocumentChecklistTemplates.AnyAsync(t => t.Code == code, cancellationToken);
        if (!templateExists)
        {
            throw new ClientPortalException("INVALID_DOCUMENT_CODE", "Unknown SPS document code.");
        }

        var file = await _db.EntryFiles.FirstOrDefaultAsync(f => f.Uuid == entryFileUuid && f.EntryId == entry.Id, cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "Entry file not found.");

        var row = await _db.SpsApplicationDocuments
            .FirstOrDefaultAsync(d => d.ApplicationId == application.Id && d.DocumentCode == code, cancellationToken);

        if (row is null)
        {
            row = new SpsApplicationDocument
            {
                ApplicationId = application.Id,
                DocumentCode = code,
                Status = "Attached",
                EntryFileId = file.Id,
            };
            _db.SpsApplicationDocuments.Add(row);
        }
        else
        {
            row.EntryFileId = file.Id;
            row.Status = "Attached";
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await BuildReadinessAsync(entry, application.Id, cancellationToken);
    }

    public async Task<EntrySpsReadinessDto> GetReadinessAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries
            .AsNoTracking()
            .Include(e => e.Agency)
            .Include(e => e.MicUtilizations).ThenInclude(u => u.Mic)
            .FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        var applicationId = await _db.SpsImportClearanceApplications
            .AsNoTracking()
            .Where(a => a.EntryId == entry.Id)
            .Select(a => (long?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return await BuildReadinessAsync(entry, applicationId, cancellationToken);
    }

    private async Task SeedPrerequisiteClearancesAsync(
        Entry entry,
        SpsImportClearanceApplication application,
        AgencyLicense licenseEntity,
        CancellationToken cancellationToken)
    {
        _db.SpsPrerequisiteClearances.Add(new SpsPrerequisiteClearance
        {
            EntryId = entry.Id,
            SpsApplicationId = application.Id,
            IssuingAgencyId = entry.AgencyId,
            PrerequisiteCode = "AGENCY_LICENSE",
            Status = "Cleared",
            ReferenceNumber = licenseEntity.LicenseNumber,
            ClearedAt = DateTime.UtcNow,
        });

        if (entry.FinalWarehouseFacilityId is long warehouseId)
        {
            var destinationReady = await _facilityCompliance.IsDestinationReadyAsync(warehouseId, cancellationToken);
            _db.SpsPrerequisiteClearances.Add(new SpsPrerequisiteClearance
            {
                EntryId = entry.Id,
                SpsApplicationId = application.Id,
                IssuingAgencyId = entry.AgencyId,
                PrerequisiteCode = "DESTINATION_RSAS_CSW",
                Status = destinationReady ? "Cleared" : "Pending",
                ClearedAt = destinationReady ? DateTime.UtcNow : null,
            });
        }
    }

    private async Task SeedApplicationDocumentsAsync(long applicationId, CancellationToken cancellationToken)
    {
        var templates = await _db.SpsDocumentChecklistTemplates
            .AsNoTracking()
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);

        foreach (var template in templates)
        {
            _db.SpsApplicationDocuments.Add(new SpsApplicationDocument
            {
                ApplicationId = applicationId,
                DocumentCode = template.Code,
                Status = "Pending",
            });
        }
    }

    private async Task<EntrySpsReadinessDto> BuildReadinessAsync(Entry entry, long? applicationId, CancellationToken cancellationToken)
    {
        var hsCode = entry.MicUtilizations
            .OrderByDescending(u => u.UtilizedAt)
            .Select(u => u.Mic?.HsCode)
            .FirstOrDefault(h => !string.IsNullOrWhiteSpace(h));

        var issuer = await _issuingRules.ResolveIssuerAsync(hsCode, entry.AgencyId, cancellationToken);
        var agencyMatches = issuer is null || issuer.AgencyId == entry.AgencyId;

        var templates = await _db.SpsDocumentChecklistTemplates
            .AsNoTracking()
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);

        var uploaded = applicationId is null
            ? new List<SpsApplicationDocument>()
            : await _db.SpsApplicationDocuments
                .AsNoTracking()
                .Where(d => d.ApplicationId == applicationId)
                .ToListAsync(cancellationToken);

        var hasLicense = await _agencyLicenses.GetPrimaryActiveLicenseAsync(entry.UserId, entry.AgencyId, cancellationToken) is not null;
        var destinationReady = entry.FinalWarehouseFacilityId is long warehouseId
            && await _facilityCompliance.IsDestinationReadyAsync(warehouseId, cancellationToken);

        var uploadedByCode = uploaded.ToDictionary(d => d.DocumentCode, StringComparer.OrdinalIgnoreCase);
        var checklist = templates.Select(t =>
        {
            uploadedByCode.TryGetValue(t.Code, out var row);
            var status = row?.Status ?? "NotStarted";
            if (row?.EntryFileId is not null)
            {
                status = "Attached";
            }
            else if (t.Code == "D" && hasLicense)
            {
                status = "Attached";
            }
            else if (t.Code == "E" && destinationReady)
            {
                status = "Attached";
            }

            return new SpsChecklistItemDto(t.Code, t.Title, t.IsRequired, status, row?.EntryFileId);
        }).ToList();

        var allRequiredReady = checklist
            .Where(c => c.IsRequired)
            .All(c => c.Status == "Attached" || c.Status == "Waived");

        return new EntrySpsReadinessDto(
            entry.Uuid,
            hsCode,
            issuer,
            agencyMatches,
            checklist,
            allRequiredReady);
    }

    private async Task<SpsImportClearanceApplication?> LoadApplicationGraphAsync(long entryId, CancellationToken cancellationToken) =>
        await _db.SpsImportClearanceApplications
            .AsNoTracking()
            .Include(a => a.IssuedClearance)
            .FirstOrDefaultAsync(a => a.EntryId == entryId, cancellationToken);

    internal static EntrySpsApplicationDto Map(SpsImportClearanceApplication application, Guid entryUuid)
    {
        EntrySpsClearanceDto? clearance = null;
        if (application.IssuedClearance is not null)
        {
            var c = application.IssuedClearance;
            clearance = new EntrySpsClearanceDto(
                c.Uuid,
                c.ClearanceNumber,
                c.Status.ToString(),
                c.IssuedAt,
                c.MustShipOutBy,
                c.MustArriveBy,
                c.DaTradeReference);
        }

        return new EntrySpsApplicationDto(
            application.Uuid,
            entryUuid,
            application.Status.ToString(),
            application.SubmittedAt,
            application.ReviewComments,
            clearance);
    }
}

public class AgencySpsicReviewService : IAgencySpsicReviewService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ISpsMustShipOutService _mustShipOut;

    public AgencySpsicReviewService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        ISpsMustShipOutService mustShipOut)
    {
        _db = db;
        _currentUser = currentUser;
        _mustShipOut = mustShipOut;
    }

    public async Task<PagedResult<AgencySpsApplicationListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.SpsImportClearanceApplications
            .AsNoTracking()
            .Include(a => a.Entry)
            .Include(a => a.User).ThenInclude(u => u.Profile)
            .Where(a => a.AgencyId == agency.Id && a.Status == SpsImportClearanceApplicationStatus.Submitted);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(a => new AgencySpsApplicationListItemDto(
            a.Uuid,
            a.Entry.Uuid,
            a.Entry.ReferenceNo,
            a.User.Profile?.CompanyName ?? a.User.Email,
            a.Status.ToString(),
            a.SubmittedAt)).ToList();

        return new PagedResult<AgencySpsApplicationListItemDto>(items, page, pageSize, total);
    }

    public async Task<EntrySpsApplicationDto?> GetAsync(Guid applicationUuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var application = await _db.SpsImportClearanceApplications
            .AsNoTracking()
            .Include(a => a.Entry)
            .Include(a => a.IssuedClearance)
            .FirstOrDefaultAsync(a => a.Uuid == applicationUuid && a.AgencyId == agency.Id, cancellationToken);

        return application is null ? null : EntrySpsicService.Map(application, application.Entry.Uuid);
    }

    public async Task<EntrySpsClearanceDto> IssueClearanceAsync(Guid applicationUuid, IssueSpsClearanceRequest request, CancellationToken cancellationToken = default)
    {
        var (reviewer, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var application = await _db.SpsImportClearanceApplications
            .Include(a => a.Entry).ThenInclude(e => e.Detail)
            .FirstOrDefaultAsync(a => a.Uuid == applicationUuid && a.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Application not found.");

        if (application.Status != SpsImportClearanceApplicationStatus.Submitted)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only submitted applications can be issued.");
        }

        if (await _db.SpsImportClearances.AnyAsync(c => c.ApplicationId == application.Id, cancellationToken))
        {
            throw new ClientPortalException("ALREADY_ISSUED", "Clearance already issued.");
        }

        var clearanceNumber = request.ClearanceNumber.Trim();
        if (clearanceNumber.Length == 0)
        {
            throw new ClientPortalException("VALIDATION", "Clearance number is required.");
        }

        var entry = application.Entry;
        var issuedAt = request.IssuedAt.ToUniversalTime();
        var hsCode = await _db.MicUtilizations
            .AsNoTracking()
            .Where(u => u.EntryId == entry.Id)
            .OrderByDescending(u => u.UtilizedAt)
            .Select(u => u.Mic.HsCode)
            .FirstOrDefaultAsync(cancellationToken);

        var validityDays = await _mustShipOut.GetValidityDaysAsync(hsCode, entry.Detail?.CommodityName, cancellationToken);
        var mustShipOut = request.MustShipOutBy?.ToUniversalTime()
            ?? issuedAt.AddDays(validityDays);

        var clearance = new SpsImportClearance
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            ApplicationId = application.Id,
            IssuingAgencyId = agency.Id,
            ClearanceNumber = clearanceNumber,
            Status = SpsImportClearanceStatus.Issued,
            IssuedAt = issuedAt,
            MustShipOutBy = mustShipOut,
            MustArriveBy = request.MustArriveBy?.ToUniversalTime(),
            PortOfEntry = entry.Detail?.PortOfEntry,
            FinalWarehouseFacilityId = entry.FinalWarehouseFacilityId,
            CommodityDescription = entry.Detail?.CommodityName,
            DaTradeReference = string.IsNullOrWhiteSpace(request.DaTradeReference) ? null : request.DaTradeReference.Trim(),
        };

        _db.SpsImportClearances.Add(clearance);
        await _db.SaveChangesAsync(cancellationToken);

        var containers = await _db.Containers.Where(c => c.EntryId == entry.Id).ToListAsync(cancellationToken);
        foreach (var container in containers)
        {
            container.SpsImportClearanceId = clearance.Id;
        }

        var micRows = await _db.MicUtilizations.Where(u => u.EntryId == entry.Id).ToListAsync(cancellationToken);
        foreach (var micRow in micRows)
        {
            micRow.SpsImportClearanceId = clearance.Id;
        }

        application.Status = SpsImportClearanceApplicationStatus.Approved;
        application.ReviewComments = request.ReviewComments;
        application.ReviewerUserId = reviewer.Id;
        entry.CompliancePhase = EntryCompliancePhase.SpsicIssued;

        ComplianceTimelineRecorder.Add(
            _db,
            entry.Id,
            "Compliance.Spsic",
            "SPS Import Clearance issued",
            $"SPSIC {clearance.ClearanceNumber} valid until ship-out {mustShipOut:yyyy-MM-dd}.",
            reviewer.Id);

        await _db.SaveChangesAsync(cancellationToken);

        _db.ComplianceSyncOutbox.Add(new ComplianceSyncOutbox
        {
            EntityType = "SpsImportClearance",
            EntityId = clearance.Id,
            TargetSystem = ComplianceSyncTargetSystem.DaTradeSpsic,
            Operation = "DA_TRADE_SPSIC_ISSUE",
            PayloadJson = $"{{\"clearanceNumber\":\"{clearance.ClearanceNumber}\",\"entryId\":{entry.Id}}}",
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new EntrySpsClearanceDto(
            clearance.Uuid,
            clearance.ClearanceNumber,
            clearance.Status.ToString(),
            clearance.IssuedAt,
            clearance.MustShipOutBy,
            clearance.MustArriveBy,
            clearance.DaTradeReference);
    }
}
