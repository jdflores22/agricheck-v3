using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Compliance;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class AgencyPortInspectionService : IAgencyPortInspectionService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AgencyPortInspectionService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PortDabiInspectionDto> RecordPortDabiAsync(
        Guid entryUuid,
        RecordPortDabiInspectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var (inspector, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries
            .FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Entry not found.");

        var clearance = await _db.SpsImportClearances
            .AsNoTracking()
            .Where(c => c.EntryId == entry.Id && c.Status == SpsImportClearanceStatus.Issued)
            .OrderByDescending(c => c.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ClientPortalException("COMPLIANCE_SPSIC_REQUIRED", "Issued SPS clearance required for port DABI.");

        var erfi = await _db.ErfiSubmissions.AsNoTracking().FirstOrDefaultAsync(e => e.EntryId == entry.Id, cancellationToken);
        var completedAt = request.CompletedAt?.ToUniversalTime() ?? DateTime.UtcNow;

        var inspection = new Inspection
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            AgencyId = agency.Id,
            InspectorUserId = inspector.Id,
            Status = InspectionStatus.Completed,
            Kind = InspectionKind.PortDabi,
            ScheduledAt = completedAt,
            CompletedAt = completedAt,
            Findings = request.Findings,
            SpsImportClearanceId = clearance.Id,
            ErfiSubmissionId = erfi?.Id,
            SpsUsedStampAt = request.StampSpsUsed ? completedAt : null,
        };

        if (request.StampSpsUsed)
        {
            entry.CompliancePhase = EntryCompliancePhase.PortCleared;
        }

        _db.Inspections.Add(inspection);
        ComplianceTimelineRecorder.Add(
            _db,
            entry.Id,
            "Compliance.PortDabi",
            "Port DABI inspection recorded",
            request.StampSpsUsed ? $"SPSIC {clearance.ClearanceNumber} stamped USED." : null,
            inspector.Id);

        await _db.SaveChangesAsync(cancellationToken);

        return new PortDabiInspectionDto(
            inspection.Uuid,
            entry.Uuid,
            inspection.Status.ToString(),
            inspection.SpsUsedStampAt,
            clearance.ClearanceNumber);
    }
}
