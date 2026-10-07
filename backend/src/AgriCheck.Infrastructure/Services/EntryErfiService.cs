using System.Text.Json;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class EntryErfiService : IEntryErfiService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EntryErfiService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EntryErfiSubmissionDto?> GetForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken);
        if (entry is null) return null;

        var submission = await _db.ErfiSubmissions
            .AsNoTracking()
            .Include(e => e.SpsImportClearance)
            .FirstOrDefaultAsync(e => e.EntryId == entry.Id, cancellationToken);

        return submission is null ? null : Map(submission, entry.Uuid);
    }

    public async Task<EntryErfiSubmissionDto> UpsertDraftAsync(Guid entryUuid, UpsertEntryErfiRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        var clearance = await _db.SpsImportClearances
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EntryId == entry.Id && c.Status == SpsImportClearanceStatus.Issued, cancellationToken)
            ?? throw new ClientPortalException("SPS_CLEARANCE_REQUIRED", "Issued SPS clearance is required before e-RFI.");

        var submission = await _db.ErfiSubmissions.FirstOrDefaultAsync(e => e.EntryId == entry.Id, cancellationToken);
        if (submission is null)
        {
            submission = new ErfiSubmission
            {
                EntryId = entry.Id,
                SpsImportClearanceId = clearance.Id,
                FiledByUserId = user.Id,
                Status = ErfiSubmissionStatus.Draft,
            };
            _db.ErfiSubmissions.Add(submission);
        }
        else if (submission.Status != ErfiSubmissionStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only draft e-RFI can be edited.");
        }

        submission.EtaAtPoe = request.EtaAtPoe?.ToUniversalTime();
        submission.BolReference = string.IsNullOrWhiteSpace(request.BolReference) ? null : request.BolReference.Trim();
        submission.IfmDestinationJson = request.IfmDestinationJson;

        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(submission).Reference(s => s.SpsImportClearance).LoadAsync(cancellationToken);
        return Map(submission, entry.Uuid);
    }

    public async Task<EntryErfiSubmissionDto> FileAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        var submission = await _db.ErfiSubmissions
            .Include(e => e.SpsImportClearance)
            .FirstOrDefaultAsync(e => e.EntryId == entry.Id, cancellationToken)
            ?? throw new ClientPortalException("ERFI_NOT_FOUND", "Create an e-RFI draft first.");

        if (submission.Status != ErfiSubmissionStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "e-RFI has already been filed.");
        }

        if (submission.EtaAtPoe is null)
        {
            throw new ClientPortalException("VALIDATION", "ETA at port of entry is required.");
        }

        await ValidateIfmDestinationAsync(entry, submission.IfmDestinationJson, cancellationToken);

        submission.Status = ErfiSubmissionStatus.Filed;
        submission.FiledAt = DateTime.UtcNow;
        entry.CompliancePhase = EntryCompliancePhase.ErfiFiled;

        _db.ComplianceSyncOutbox.Add(new ComplianceSyncOutbox
        {
            EntityType = "ErfiSubmission",
            EntityId = submission.Id,
            TargetSystem = ComplianceSyncTargetSystem.DaTradeErfi,
            Operation = "DA_TRADE_ERFI",
            PayloadJson = $"{{\"entryId\":{entry.Id},\"erfiId\":{submission.Id}}}",
        });

        ComplianceTimelineRecorder.Add(
            _db,
            entry.Id,
            "Compliance.Erfi",
            "e-RFI filed",
            $"ETA {submission.EtaAtPoe:yyyy-MM-dd HH:mm} UTC.",
            user.Id);

        await _db.SaveChangesAsync(cancellationToken);
        return Map(submission, entry.Uuid);
    }

    private async Task ValidateIfmDestinationAsync(Entry entry, string? ifmJson, CancellationToken cancellationToken)
    {
        if (entry.FinalWarehouseFacilityId is null)
        {
            throw new ClientPortalException(
                "COMPLIANCE_IFM_DESTINATION",
                "Link the entry final warehouse before filing e-RFI (IFM destination).");
        }

        if (string.IsNullOrWhiteSpace(ifmJson))
        {
            return;
        }

        long? ifmFacilityId = null;
        try
        {
            using var doc = JsonDocument.Parse(ifmJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("warehouseFacilityId", out var idProp) && idProp.TryGetInt64(out var id))
            {
                ifmFacilityId = id;
            }
            else if (root.TryGetProperty("warehouseFacilityCode", out var codeProp)
                && codeProp.GetString() is { Length: > 0 } facilityCode)
            {
                ifmFacilityId = await _db.WarehouseFacilities
                    .AsNoTracking()
                    .Where(f => f.Code == facilityCode)
                    .Select(f => f.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }
        catch (JsonException)
        {
            throw new ClientPortalException("COMPLIANCE_IFM_INVALID", "IFM destination JSON is invalid.");
        }

        if (ifmFacilityId is null || ifmFacilityId != entry.FinalWarehouseFacilityId)
        {
            throw new ClientPortalException(
                "COMPLIANCE_IFM_MISMATCH",
                "IFM destination must match the entry's final warehouse facility.");
        }
    }

    private static EntryErfiSubmissionDto Map(ErfiSubmission submission, Guid entryUuid) =>
        new(
            submission.Uuid,
            entryUuid,
            submission.Status.ToString(),
            submission.FiledAt,
            submission.EtaAtPoe,
            submission.BolReference,
            submission.SpsImportClearance.Uuid,
            submission.SpsImportClearance.ClearanceNumber);
}
