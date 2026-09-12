using AgriCheck.Application.AgencyPortal;
using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AgriCheck.Infrastructure.Services;

public class AgencyDashboardService : IAgencyDashboardService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AgencyDashboardService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AgencyDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entries = _db.Entries.Where(e => e.AgencyId == agency.Id);

        return new AgencyDashboardDto(
            await entries.CountAsync(e =>
                (e.Status == EntryStatus.Submitted || e.Status == EntryStatus.UnderReview) &&
                e.PaymentStatus == PaymentStatus.Paid &&
                !e.EvaluatorAssignments.Any(a => a.Status == AssignmentStatus.Active), cancellationToken),
            await _db.EvaluatorAssignments.CountAsync(a =>
                a.AgencyId == agency.Id && a.EvaluatorUserId == user.Id && a.Status == AssignmentStatus.Active, cancellationToken),
            await _db.Inspections.CountAsync(i =>
                i.AgencyId == agency.Id && i.Status != InspectionStatus.Completed && i.Status != InspectionStatus.Failed, cancellationToken),
            await _db.AgencyBillings.CountAsync(b =>
                b.AgencyId == agency.Id && b.Status != AgencyBillingStatus.Paid && b.Status != AgencyBillingStatus.Cancelled, cancellationToken),
            await _db.AccreditationSubmissions.CountAsync(s =>
                s.Status == AccreditationSubmissionStatus.Submitted || s.Status == AccreditationSubmissionStatus.UnderReview, cancellationToken),
            agency.Code,
            agency.Name);
    }
}

public class EvaluatorService : IEvaluatorService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IFileStorageService _fileStorage;
    private readonly IEntryWorkflowService _workflow;

    public EvaluatorService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        INotificationService notifications,
        IFileStorageService fileStorage,
        IEntryWorkflowService workflow)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
        _fileStorage = fileStorage;
        _workflow = workflow;
    }

    public async Task<PagedResult<AgencyEntryListItemDto>> ListQueueAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.Entries
            .Include(e => e.User).ThenInclude(u => u.Profile)
            .Include(e => e.Detail)
            .Include(e => e.EvaluatorAssignments)
            .Where(e => e.AgencyId == agency.Id &&
                        e.PaymentStatus == PaymentStatus.Paid &&
                        (e.Status == EntryStatus.Submitted || e.Status == EntryStatus.UnderReview) &&
                        !e.EvaluatorAssignments.Any(a => a.Status == AssignmentStatus.Active));

        return await PageEntriesAsync(query, user.Id, page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<AgencyEntryListItemDto>> ListMyAssignmentsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.Entries
            .Include(e => e.User).ThenInclude(u => u.Profile)
            .Include(e => e.Detail)
            .Include(e => e.EvaluatorAssignments)
            .Where(e => e.AgencyId == agency.Id &&
                        e.EvaluatorAssignments.Any(a => a.EvaluatorUserId == user.Id && a.Status == AssignmentStatus.Active));

        return await PageEntriesAsync(query, user.Id, page, pageSize, cancellationToken);
    }

    public async Task AssignToSelfAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await AgencyContextHelper.RequireAgencyEntryAsync(_db, agency, entryUuid, cancellationToken);

        if (entry.PaymentStatus != PaymentStatus.Paid)
        {
            throw new ClientPortalException("PAYMENT_REQUIRED", "Entry processing fee must be paid before evaluation.");
        }

        if (entry.Status is not (EntryStatus.Submitted or EntryStatus.UnderReview))
        {
            throw new ClientPortalException("INVALID_STATUS", "Entry is not available for evaluation.");
        }

        if (await _db.EvaluatorAssignments.AnyAsync(a => a.EntryId == entry.Id && a.Status == AssignmentStatus.Active, cancellationToken))
        {
            throw new ClientPortalException("ALREADY_ASSIGNED", "Entry is already assigned to an evaluator.");
        }

        var previous = entry.Status;
        if (entry.Status == EntryStatus.Submitted)
        {
            entry.Status = EntryStatus.UnderReview;
            entry.StatusHistory.Add(new EntryStatusHistory
            {
                FromStatus = previous,
                ToStatus = EntryStatus.UnderReview,
                ChangedByUserId = user.Id,
                Comment = "Assigned for evaluation"
            });
        }

        _db.EvaluatorAssignments.Add(new EvaluatorAssignment
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            AgencyId = agency.Id,
            EvaluatorUserId = user.Id,
            Status = AssignmentStatus.Active,
            AssignedAt = DateTime.UtcNow
        });

        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "evaluator_assigned",
            Title = "Evaluator assigned",
            Description = $"Entry assigned to evaluator for review.",
            ActorUserId = user.Id
        });

        await EnsureComplianceResultsAsync(entry, agency, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AgencyEntryEvaluationDto?> GetEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await QueryEvaluationGraph()
            .FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.AgencyId == agency.Id, cancellationToken);
        if (entry is null) return null;

        var (formSchemaJson, formName) = await GetEntryFormMetaAsync(entry.AgencyId, cancellationToken);
        return MapEvaluation(entry, user.Id, formSchemaJson, formName);
    }

    public async Task EvaluateFileAsync(Guid fileUuid, EvaluateFileRequest request, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<EvaluationDecision>(request.Decision, true, out var decision))
        {
            throw new ClientPortalException("INVALID_DECISION", "Decision must be Approved, Rejected, or RevisionRequired.");
        }

        var file = await _db.EntryFiles
            .Include(f => f.Entry)
            .FirstOrDefaultAsync(f => f.Uuid == fileUuid && f.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        await RequireActiveAssignmentAsync(file.EntryId, user.Id, cancellationToken);

        var evaluation = await _db.FileEvaluations
            .FirstOrDefaultAsync(e => e.EntryFileId == file.Id && e.EvaluatorUserId == user.Id, cancellationToken);

        if (evaluation is null)
        {
            evaluation = new FileEvaluation { EntryFileId = file.Id, EvaluatorUserId = user.Id };
            _db.FileEvaluations.Add(evaluation);
        }

        evaluation.Decision = decision;
        evaluation.Comment = request.Comment;

        if (string.Equals(file.DocumentType, EntryMavHelper.MavCertificateDocumentType, StringComparison.OrdinalIgnoreCase)
            && file.Entry.EntryType == EntryType.Import)
        {
            file.Entry.MavDocumentStatus = EntryMavHelper.MapEvaluationToMavStatus(decision);
            file.Entry.MavRemarks = request.Comment;
        }

        await ApplyComplianceStatusFromFilesAsync(file.Entry, user.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        if (decision == EvaluationDecision.RevisionRequired)
        {
            await _notifications.NotifyAsync(
                file.Entry.UserId,
                "entry_file_compliance",
                "Document revision required",
                $"A document on entry {file.Entry.ReferenceNo} requires revision.",
                "Entry",
                file.Entry.Uuid.ToString(),
                cancellationToken);
        }
    }

    public async Task EvaluateMavDocumentAsync(Guid entryUuid, EvaluateEntryMavRequest request, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<EvaluationDecision>(request.Decision, true, out var decision) ||
            decision is EvaluationDecision.Pending)
        {
            throw new ClientPortalException("INVALID_DECISION", "Decision must be Approved, Rejected, or RevisionRequired.");
        }

        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.EntryType != EntryType.Import)
        {
            throw new ClientPortalException("INVALID_ENTRY_TYPE", "MAV review applies to import entries only.");
        }

        await RequireActiveAssignmentAsync(entry.Id, user.Id, cancellationToken);

        entry.MavDocumentStatus = EntryMavHelper.MapEvaluationToMavStatus(decision);
        entry.MavRemarks = request.Remarks?.Trim();

        var mavFile = await _db.EntryFiles
            .FirstOrDefaultAsync(
                f => f.EntryId == entry.Id &&
                     f.DocumentType == EntryMavHelper.MavCertificateDocumentType,
                cancellationToken);

        if (mavFile is not null)
        {
            var evaluation = await _db.FileEvaluations
                .FirstOrDefaultAsync(e => e.EntryFileId == mavFile.Id && e.EvaluatorUserId == user.Id, cancellationToken);

            if (evaluation is null)
            {
                evaluation = new FileEvaluation { EntryFileId = mavFile.Id, EvaluatorUserId = user.Id };
                _db.FileEvaluations.Add(evaluation);
            }

            evaluation.Decision = decision;
            evaluation.Comment = request.Remarks;
        }

        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "mav_document_reviewed",
            Title = "MAV document reviewed",
            Description = request.Remarks ?? $"MAV document marked as {decision}.",
            ActorUserId = user.Id
        });

        await ApplyComplianceStatusFromFilesAsync(entry, user.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        if (decision == EvaluationDecision.RevisionRequired)
        {
            await _notifications.NotifyAsync(
                entry.UserId,
                "mav_revision_required",
                "MAV certificate revision required",
                $"MAV certificate on entry {entry.ReferenceNo} requires revision.",
                "Entry",
                entry.Uuid.ToString(),
                cancellationToken);
        }
    }

    public async Task UpdateComplianceAsync(Guid entryUuid, UpdateComplianceRequest request, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await AgencyContextHelper.RequireAgencyEntryAsync(_db, agency, entryUuid, cancellationToken);
        await RequireActiveAssignmentAsync(entry.Id, user.Id, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!Enum.TryParse<ComplianceResultStatus>(item.Status, true, out var status))
            {
                throw new ClientPortalException("INVALID_STATUS", $"Invalid compliance status for item {item.ItemId}.");
            }

            var result = await _db.EntryComplianceResults
                .FirstOrDefaultAsync(r => r.EntryId == entry.Id && r.ChecklistItemId == item.ItemId, cancellationToken)
                ?? throw new ClientPortalException("CHECKLIST_ITEM_NOT_FOUND", "Checklist item not found for entry.");

            result.Status = status;
            result.Notes = item.Notes;
            result.EvaluatedByUserId = user.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddNoteAsync(Guid entryUuid, AddEvaluatorNoteRequest request, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await AgencyContextHelper.RequireAgencyEntryAsync(_db, agency, entryUuid, cancellationToken);
        await RequireActiveAssignmentAsync(entry.Id, user.Id, cancellationToken);

        _db.EvaluatorNotes.Add(new EvaluatorNote
        {
            EntryId = entry.Id,
            AuthorUserId = user.Id,
            Note = request.Note.Trim(),
            IsInternal = request.IsInternal
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteEvaluationAsync(Guid entryUuid, CompleteEvaluationRequest request, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries
            .Include(e => e.EvaluatorAssignments)
            .FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        var assignment = await RequireActiveAssignmentAsync(entry.Id, user.Id, cancellationToken);
        if (!Enum.TryParse<EvaluationDecision>(request.Decision, true, out var decision) ||
            decision is EvaluationDecision.Pending)
        {
            throw new ClientPortalException("INVALID_DECISION", "Decision must be Approved, Rejected, or RevisionRequired.");
        }

        if (decision == EvaluationDecision.Approved &&
            entry.EntryType == EntryType.Import &&
            entry.MavDocumentStatus is not EntryMavDocumentStatus.Approved)
        {
            throw new ClientPortalException(
                "MAV_NOT_APPROVED",
                "Approve the MAV certificate before completing entry evaluation.");
        }

        var previous = entry.Status;
        entry.Status = decision switch
        {
            EvaluationDecision.Approved => EntryStatus.DaIssueBilling,
            EvaluationDecision.Rejected => EntryStatus.Rejected,
            EvaluationDecision.RevisionRequired => EntryStatus.ForCompliance,
            _ => entry.Status
        };

        assignment.Status = AssignmentStatus.Completed;
        assignment.CompletedAt = DateTime.UtcNow;

        entry.StatusHistory.Add(new EntryStatusHistory
        {
            FromStatus = previous,
            ToStatus = entry.Status,
            ChangedByUserId = user.Id,
            Comment = request.Comment ?? $"Evaluation completed: {decision}"
        });

        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "evaluation_completed",
            Title = "Evaluation completed",
            Description = request.Comment ?? $"Evaluator decision: {decision}",
            ActorUserId = user.Id
        });

        await _db.SaveChangesAsync(cancellationToken);

        if (decision == EvaluationDecision.Approved)
        {
            await _workflow.CreateAndIssueDaBillingAsync(entry, user.Id, cancellationToken);
        }

        await _notifications.NotifyAsync(
            entry.UserId,
            "entry_status",
            "Entry status updated",
            $"Your entry {entry.ReferenceNo} is now {entry.Status}.",
            "Entry",
            entry.Uuid.ToString(),
            cancellationToken);
    }

    public async Task<StoredFileDownload> DownloadFileAsync(Guid entryUuid, Guid fileUuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var file = await _db.EntryFiles
            .Include(f => f.Entry)
            .FirstOrDefaultAsync(
                f => f.Uuid == fileUuid && f.Entry.Uuid == entryUuid && f.Entry.AgencyId == agency.Id,
                cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        return new StoredFileDownload(
            StoredFilePathResolver.ResolveEntryFilePath(_fileStorage, entryUuid, file.StoredFileName),
            file.ContentType,
            file.OriginalFileName);
    }

    public async Task<StoredFileDownload> DownloadFileVersionAsync(
        Guid entryUuid,
        Guid fileUuid,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var file = await _db.EntryFiles
            .Include(f => f.Entry)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(
                f => f.Uuid == fileUuid && f.Entry.Uuid == entryUuid && f.Entry.AgencyId == agency.Id,
                cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        var version = file.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber)
            ?? throw new ClientPortalException("VERSION_NOT_FOUND", "Document version not found.");

        return new StoredFileDownload(
            StoredFilePathResolver.ResolveEntryFilePath(_fileStorage, entryUuid, version.StoredFileName),
            version.ContentType,
            version.OriginalFileName);
    }

    private async Task ApplyComplianceStatusFromFilesAsync(Entry entry, long actorUserId, CancellationToken cancellationToken)
    {
        var files = await _db.EntryFiles
            .Include(f => f.Evaluations)
            .Where(f => f.EntryId == entry.Id)
            .ToListAsync(cancellationToken);

        if (files.Count == 0) return;

        var requiresRevision = files.Any(f => f.Evaluations.Any(e => e.Decision == EvaluationDecision.RevisionRequired));
        if (!requiresRevision || entry.Status == EntryStatus.ForCompliance) return;

        var previous = entry.Status;
        entry.Status = EntryStatus.ForCompliance;
        var deadlineDays = await SystemSettingsReader.GetIntAsync(_db, "compliance_deadline_days", 30, cancellationToken);
        entry.ComplianceDeadlineAt = DateTime.UtcNow.AddDays(deadlineDays);
        entry.StatusHistory.Add(new EntryStatusHistory
        {
            FromStatus = previous,
            ToStatus = EntryStatus.ForCompliance,
            ChangedByUserId = actorUserId,
            Comment = "Entry marked for compliance due to document revisions"
        });
        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "entry_for_compliance",
            Title = "For compliance",
            Description = $"One or more documents require revision. Deadline: {entry.ComplianceDeadlineAt:yyyy-MM-dd}.",
            ActorUserId = actorUserId
        });

        await _notifications.NotifyAsync(
            entry.UserId,
            "compliance_required",
            "Compliance required",
            $"Entry {entry.ReferenceNo} requires document revisions by {entry.ComplianceDeadlineAt:yyyy-MM-dd}.",
            "Entry",
            entry.Uuid.ToString(),
            cancellationToken);
    }

    public async Task<PagedResult<AgencyEntryListItemDto>> ListApprovedEntriesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.Entries
            .Include(e => e.User).ThenInclude(u => u.Profile)
            .Include(e => e.Detail)
            .Include(e => e.EvaluatorAssignments)
            .Include(e => e.Inspections)
            .Where(e => e.AgencyId == agency.Id &&
                        EntryStatusRules.CanScheduleAgencyInspection(e.Status) &&
                        !e.Inspections.Any(i => i.Status == InspectionStatus.Scheduled ||
                                                i.Status == InspectionStatus.InProgress ||
                                                i.Status == InspectionStatus.Completed));

        return await PageEntriesAsync(query, user.Id, page, pageSize, cancellationToken);
    }

    private async Task<PagedResult<AgencyEntryListItemDto>> PageEntriesAsync(
        IQueryable<Entry> query,
        long userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderByDescending(e => e.SubmittedAt ?? e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entries
            .Select(e => MapEntryListItem(e, userId))
            .ToList();

        return new PagedResult<AgencyEntryListItemDto>(items, page, pageSize, total);
    }

    private static AgencyEntryListItemDto MapEntryListItem(Entry entry, long userId)
    {
        var applicant = entry.User.Profile is not null
            ? $"{entry.User.Profile.FirstName} {entry.User.Profile.LastName}".Trim()
            : entry.User.Email;

        return new AgencyEntryListItemDto(
            entry.Uuid,
            entry.ReferenceNo,
            entry.EntryType.ToString(),
            entry.Status.ToString(),
            applicant,
            ResolveEntryCompanyName(entry),
            entry.Detail?.CommodityName,
            entry.PaymentStatus.ToString(),
            entry.SubmittedAt,
            entry.EvaluatorAssignments.Any(a => a.EvaluatorUserId == userId && a.Status == AssignmentStatus.Active));
    }

    private static string? ResolveEntryCompanyName(Entry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.FormDataJson))
        {
            try
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(entry.FormDataJson);
                if (values is not null)
                {
                    foreach (var key in new[] { "company_name", "companyName", "txt_company_name", "business_name" })
                    {
                        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                        {
                            return value.Trim();
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed form payloads and fall back to profile data.
            }
        }

        var profileCompany = entry.User.Profile?.CompanyName;
        return string.IsNullOrWhiteSpace(profileCompany) ? null : profileCompany.Trim();
    }

    private async Task<EvaluatorAssignment> RequireActiveAssignmentAsync(long entryId, long userId, CancellationToken cancellationToken)
    {
        return await _db.EvaluatorAssignments.FirstOrDefaultAsync(a =>
            a.EntryId == entryId && a.EvaluatorUserId == userId && a.Status == AssignmentStatus.Active, cancellationToken)
            ?? throw new ClientPortalException("NOT_ASSIGNED", "You must assign this entry to yourself before evaluating.");
    }

    private async Task EnsureComplianceResultsAsync(Entry entry, Agency agency, CancellationToken cancellationToken)
    {
        if (await _db.EntryComplianceResults.AnyAsync(r => r.EntryId == entry.Id, cancellationToken))
        {
            return;
        }

        var checklist = await _db.ComplianceChecklists
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.AgencyId == agency.Id && c.IsActive, cancellationToken);

        if (checklist is null) return;

        foreach (var item in checklist.Items.OrderBy(i => i.SortOrder))
        {
            _db.EntryComplianceResults.Add(new EntryComplianceResult
            {
                EntryId = entry.Id,
                ChecklistItemId = item.Id,
                Status = ComplianceResultStatus.Pending
            });
        }
    }

    private IQueryable<Entry> QueryEvaluationGraph() =>
        _db.Entries
            .Include(e => e.User).ThenInclude(u => u.Profile)
            .Include(e => e.Agency)
            .Include(e => e.Detail)
            .Include(e => e.Files).ThenInclude(f => f.Evaluations)
            .Include(e => e.Files).ThenInclude(f => f.Versions)
            .Include(e => e.ComplianceResults).ThenInclude(r => r.ChecklistItem)
            .Include(e => e.EvaluatorNotes).ThenInclude(n => n.Author).ThenInclude(a => a.Profile)
            .Include(e => e.StatusHistory)
            .Include(e => e.TimelineEvents)
            .Include(e => e.EvaluatorAssignments)
            .Include(e => e.MicUtilizations).ThenInclude(u => u.Mic);

    private static AgencyEntryEvaluationDto MapEvaluation(
        Entry entry,
        long userId,
        string? formSchemaJson,
        string? formName)
    {
        var applicant = entry.User.Profile is not null
            ? $"{entry.User.Profile.FirstName} {entry.User.Profile.LastName}".Trim()
            : entry.User.Email;

        var isAssignedToMe = entry.EvaluatorAssignments.Any(
            a => a.EvaluatorUserId == userId && a.Status == AssignmentStatus.Active);

        return new AgencyEntryEvaluationDto(
            entry.Uuid,
            entry.ReferenceNo,
            entry.EntryType.ToString(),
            entry.Status.ToString(),
            entry.Agency.Code,
            applicant,
            ResolveEntryCompanyName(entry),
            entry.PaymentStatus.ToString(),
            entry.SubmittedAt,
            entry.Notes,
            entry.FormDataJson,
            formSchemaJson,
            formName,
            isAssignedToMe,
            entry.Detail is null ? null : new EntryDetailDto(
                entry.Detail.CommodityId, entry.Detail.CommodityName, entry.Detail.Description, entry.Detail.Quantity,
                entry.Detail.Unit, entry.Detail.OriginCountry, entry.Detail.DestinationCountry, entry.Detail.PortOfEntry),
            entry.Files.Select(f => MapEntryFile(f, userId)).ToList(),
            entry.ComplianceResults.OrderBy(r => r.ChecklistItem.SortOrder).Select(r => new ComplianceItemResultDto(
                r.ChecklistItemId, r.ChecklistItem.Label, r.ChecklistItem.IsRequired, r.Status.ToString(), r.Notes)).ToList(),
            entry.EvaluatorNotes.OrderByDescending(n => n.CreatedAt).Select(n => new EvaluatorNoteDto(
                n.Note, n.IsInternal,
                n.Author.Profile is not null ? $"{n.Author.Profile.FirstName} {n.Author.Profile.LastName}" : n.Author.Email,
                n.CreatedAt)).ToList(),
            entry.StatusHistory.OrderByDescending(h => h.CreatedAt).Select(h => new StatusHistoryDto(
                h.FromStatus.ToString(), h.ToStatus.ToString(), h.Comment, h.CreatedAt)).ToList(),
            entry.TimelineEvents.OrderByDescending(t => t.CreatedAt).Select(t => new TimelineEventDto(
                t.EventType, t.Title, t.Description, t.CreatedAt)).ToList(),
            EntryMavHelper.MapMavInfo(entry));
    }

    private static AgencyEntryFileDto MapEntryFile(EntryFile file, long userId)
    {
        var evaluation = file.Evaluations.FirstOrDefault(e => e.EvaluatorUserId == userId);
        var versions = file.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new EntryFileVersionDto(
                v.VersionNumber,
                v.OriginalFileName,
                v.FileSizeBytes,
                v.CreatedAt,
                v.StoredFileName == file.StoredFileName))
            .ToList();

        return new AgencyEntryFileDto(
            file.Uuid,
            file.OriginalFileName,
            file.DocumentType,
            file.FileSizeBytes,
            file.CreatedAt,
            evaluation?.Decision.ToString(),
            evaluation?.Comment,
            versions);
    }

    private async Task<(string? SchemaJson, string? FormName)> GetEntryFormMetaAsync(long agencyId, CancellationToken cancellationToken)
    {
        var template = await _db.FormTemplates
            .Include(t => t.AgencyTags)
            .Include(t => t.Versions)
            .Where(t =>
                t.IsActive &&
                t.Status == FormTemplateStatus.Published &&
                t.FormType == "ENTRY" &&
                t.AgencyTags.Any(a => a.AgencyId == agencyId))
            .OrderByDescending(t => t.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null) return (null, null);

        var version = template.Versions
            .Where(v => v.IsPublished)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        return (version?.SchemaJson, template.Name);
    }
}

public class InspectionService : IInspectionService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public InspectionService(AgriCheckDbContext db, ICurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<PagedResult<InspectionListItemDto>> ListAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.Inspections
            .Include(i => i.Entry).ThenInclude(e => e.User).ThenInclude(u => u.Profile)
            .Where(i => i.AgencyId == agency.Id);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InspectionStatus>(status, true, out var parsed))
        {
            query = query.Where(i => i.Status == parsed);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(i => i.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InspectionListItemDto(
                i.Uuid, i.Entry.Uuid, i.Entry.ReferenceNo, i.Status.ToString(),
                i.Entry.User.Profile != null ? $"{i.Entry.User.Profile.FirstName} {i.Entry.User.Profile.LastName}" : i.Entry.User.Email,
                i.ScheduledAt, i.CompletedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<InspectionListItemDto>(items, page, pageSize, total);
    }

    public async Task<InspectionDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var inspection = await QueryGraph().FirstOrDefaultAsync(i => i.Uuid == uuid && i.AgencyId == agency.Id, cancellationToken);
        return inspection is null ? null : Map(inspection);
    }

    public async Task<InspectionDto> CreateAsync(CreateInspectionRequest request, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await AgencyContextHelper.RequireAgencyEntryAsync(_db, agency, request.EntryUuid, cancellationToken);

        if (!EntryStatusRules.CanScheduleAgencyInspection(entry.Status))
        {
            throw new ClientPortalException("ENTRY_NOT_APPROVED", "Entry must be approved and paid through DA billing before agency inspection scheduling.");
        }

        if (await _db.Inspections.AnyAsync(i => i.EntryId == entry.Id && i.Status != InspectionStatus.Failed, cancellationToken))
        {
            throw new ClientPortalException("INSPECTION_EXISTS", "An active inspection already exists for this entry.");
        }

        var inspection = new Inspection
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            AgencyId = agency.Id,
            InspectorUserId = user.Id,
            Status = InspectionStatus.Scheduled,
            ScheduledAt = request.ScheduledAt ?? DateTime.UtcNow.AddDays(1)
        };

        _db.Inspections.Add(inspection);
        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "inspection_scheduled",
            Title = "Inspection scheduled",
            Description = $"Inspection scheduled for {inspection.ScheduledAt:yyyy-MM-dd}.",
            ActorUserId = user.Id
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Map(await QueryGraph().FirstAsync(i => i.Id == inspection.Id, cancellationToken));
    }

    public async Task<InspectionDto> UpdateAsync(Guid uuid, UpdateInspectionRequest request, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var inspection = await _db.Inspections.FirstOrDefaultAsync(i => i.Uuid == uuid && i.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Inspection not found.");

        if (inspection.InspectorUserId != user.Id && !_currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "Only the assigned inspector can update this inspection.");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<InspectionStatus>(request.Status, true, out var status))
        {
            inspection.Status = status;
        }

        inspection.Findings = request.Findings ?? inspection.Findings;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await QueryGraph().FirstAsync(i => i.Id == inspection.Id, cancellationToken));
    }

    public async Task<InspectionDto> CompleteAsync(Guid uuid, CompleteInspectionRequest request, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var inspection = await _db.Inspections.Include(i => i.Entry).FirstOrDefaultAsync(i => i.Uuid == uuid && i.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Inspection not found.");

        var passed = request.Result.Equals("pass", StringComparison.OrdinalIgnoreCase) ||
                     request.Result.Equals("passed", StringComparison.OrdinalIgnoreCase);

        inspection.Status = passed ? InspectionStatus.Completed : InspectionStatus.Failed;
        inspection.CompletedAt = DateTime.UtcNow;
        inspection.Findings = request.Findings ?? inspection.Findings;

        inspection.Entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "inspection_completed",
            Title = passed ? "Inspection passed" : "Inspection failed",
            Description = request.Findings,
            ActorUserId = user.Id
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Map(await QueryGraph().FirstAsync(i => i.Id == inspection.Id, cancellationToken));
    }

    public async Task<InspectionPhotoDto> UploadPhotoAsync(Guid inspectionUuid, Stream fileStream, string fileName, string contentType, string? caption, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var inspection = await _db.Inspections.FirstOrDefaultAsync(i => i.Uuid == inspectionUuid && i.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Inspection not found.");

        var (storedFileName, _) = await _fileStorage.SaveAsync(fileStream, $"inspections/{inspection.Uuid}", fileName, cancellationToken);
        var photo = new InspectionPhoto
        {
            Uuid = Guid.NewGuid(),
            InspectionId = inspection.Id,
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            Caption = caption
        };

        _db.InspectionPhotos.Add(photo);
        await _db.SaveChangesAsync(cancellationToken);
        return new InspectionPhotoDto(photo.Uuid, photo.OriginalFileName, photo.Caption, photo.CreatedAt);
    }

    private IQueryable<Inspection> QueryGraph() =>
        _db.Inspections
            .Include(i => i.Entry).ThenInclude(e => e.User).ThenInclude(u => u.Profile)
            .Include(i => i.Photos);

    private static InspectionDto Map(Inspection inspection)
    {
        var applicant = inspection.Entry.User.Profile is not null
            ? $"{inspection.Entry.User.Profile.FirstName} {inspection.Entry.User.Profile.LastName}"
            : inspection.Entry.User.Email;

        return new InspectionDto(
            inspection.Uuid,
            inspection.Entry.Uuid,
            inspection.Entry.ReferenceNo,
            inspection.Status.ToString(),
            applicant,
            inspection.ScheduledAt,
            inspection.CompletedAt,
            inspection.Findings,
            inspection.Photos.OrderByDescending(p => p.CreatedAt).Select(p => new InspectionPhotoDto(
                p.Uuid, p.OriginalFileName, p.Caption, p.CreatedAt)).ToList());
    }
}

public class AgencyBillingService : IAgencyBillingService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEntryWorkflowService _workflow;

    public AgencyBillingService(AgriCheckDbContext db, ICurrentUserService currentUser, IEntryWorkflowService workflow)
    {
        _db = db;
        _currentUser = currentUser;
        _workflow = workflow;
    }

    public async Task<PagedResult<AgencyBillingListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var query = _db.AgencyBillings.Include(b => b.Entry).Where(b => b.AgencyId == agency.Id);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(b => b.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new AgencyBillingListItemDto(
                b.Uuid, b.BillNumber, b.Description, b.Amount, b.Status.ToString(),
                b.Entry != null ? b.Entry.ReferenceNo : null, b.IssuedAt, b.PaidAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AgencyBillingListItemDto>(items, page, pageSize, total);
    }

    public async Task<AgencyBillingListItemDto> CreateAsync(CreateAgencyBillingRequest request, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await AgencyContextHelper.RequireAgencyEntryAsync(_db, agency, request.EntryUuid, cancellationToken);

        var billing = new AgencyBilling
        {
            Uuid = Guid.NewGuid(),
            AgencyId = agency.Id,
            EntryId = entry.Id,
            BillNumber = await ReferenceNumberGenerator.AgencyBillAsync(_db, cancellationToken),
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Status = AgencyBillingStatus.Draft,
            IssuedByUserId = user.Id
        };

        _db.AgencyBillings.Add(billing);
        await _db.SaveChangesAsync(cancellationToken);

        return new AgencyBillingListItemDto(
            billing.Uuid, billing.BillNumber, billing.Description, billing.Amount, billing.Status.ToString(),
            entry.ReferenceNo, billing.IssuedAt, billing.PaidAt);
    }

    public async Task<AgencyBillingListItemDto> IssueAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var billing = await _db.AgencyBillings.Include(b => b.Entry).FirstOrDefaultAsync(b => b.Uuid == uuid && b.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Billing record not found.");

        if (billing.Status != AgencyBillingStatus.Draft)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only draft billings can be issued.");
        }

        billing.Status = AgencyBillingStatus.Issued;
        billing.IssuedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new AgencyBillingListItemDto(
            billing.Uuid, billing.BillNumber, billing.Description, billing.Amount, billing.Status.ToString(),
            billing.Entry?.ReferenceNo, billing.IssuedAt, billing.PaidAt);
    }

    public async Task<AgencyBillingListItemDto> MarkPaidAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var billing = await _db.AgencyBillings.Include(b => b.Entry).FirstOrDefaultAsync(b => b.Uuid == uuid && b.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Billing record not found.");

        if (billing.Status == AgencyBillingStatus.Paid)
        {
            throw new ClientPortalException("ALREADY_PAID", "Billing is already paid.");
        }

        billing.Status = AgencyBillingStatus.Paid;
        billing.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _workflow.OnDaBillingPaidAsync(billing, user.Id, cancellationToken);

        return new AgencyBillingListItemDto(
            billing.Uuid, billing.BillNumber, billing.Description, billing.Amount, billing.Status.ToString(),
            billing.Entry?.ReferenceNo, billing.IssuedAt, billing.PaidAt);
    }

    public async Task<AgencyBillingListItemDto> VerifyPaymentAsync(
        Guid uuid,
        VerifyDaBillingPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var billing = await _db.AgencyBillings.Include(b => b.Entry).FirstOrDefaultAsync(b => b.Uuid == uuid && b.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Billing record not found.");

        if (billing.Status != AgencyBillingStatus.PaymentPending)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only uploaded payments can be verified.");
        }

        if (request.Approved)
        {
            billing.Status = AgencyBillingStatus.Paid;
            billing.PaidAt = DateTime.UtcNow;
            billing.VerifiedByUserId = user.Id;
            billing.VerifiedAt = DateTime.UtcNow;
            billing.VerificationNotes = request.Notes?.Trim();
            await _db.SaveChangesAsync(cancellationToken);
            await _workflow.OnDaBillingPaidAsync(billing, user.Id, cancellationToken);
        }
        else
        {
            billing.Status = AgencyBillingStatus.Issued;
            billing.VerificationNotes = request.Notes?.Trim();
            billing.PaymentUploadedAt = null;
            billing.PaymentReference = null;
            billing.PaymentProofStoredFileName = null;
            billing.PaymentProofOriginalFileName = null;
            billing.PaymentProofContentType = null;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new AgencyBillingListItemDto(
            billing.Uuid, billing.BillNumber, billing.Description, billing.Amount, billing.Status.ToString(),
            billing.Entry?.ReferenceNo, billing.IssuedAt, billing.PaidAt);
    }
}

public class AccreditationReviewService : IAccreditationReviewService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly INotificationService _notifications;
    private readonly IAccreditationNumberGenerator _accreditationNumberGenerator;
    private readonly IAccreditationCertificateService _accreditationCertificateService;

    public AccreditationReviewService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        INotificationService notifications,
        IAccreditationNumberGenerator accreditationNumberGenerator,
        IAccreditationCertificateService accreditationCertificateService)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _notifications = notifications;
        _accreditationNumberGenerator = accreditationNumberGenerator;
        _accreditationCertificateService = accreditationCertificateService;
    }

    public async Task<AccreditationOfficerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var user = await AccreditationContextHelper.RequireAccreditationOfficerAsync(_db, _currentUser, cancellationToken);

        var unclaimedQuery = BuildFilteredQuery("unclaimed", user.Id);
        var myQuery = BuildFilteredQuery("mine", user.Id);

        var unclaimedRows = await unclaimedQuery
            .Include(s => s.User).ThenInclude(u => u.Profile)
            .Include(s => s.AssignedOfficer).ThenInclude(o => o!.Profile)
            .Include(s => s.History)
            .OrderByDescending(s => s.SubmittedAt ?? s.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var unclaimed = unclaimedRows.Select(s => MapListItem(s, user.Id)).ToList();

        var myRows = await myQuery
            .Include(s => s.User).ThenInclude(u => u.Profile)
            .Include(s => s.AssignedOfficer).ThenInclude(o => o!.Profile)
            .Include(s => s.History)
            .OrderByDescending(s => s.ClaimedAt ?? s.SubmittedAt ?? s.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var myApplications = myRows.Select(s => MapListItem(s, user.Id)).ToList();

        return new AccreditationOfficerDashboardDto(
            await unclaimedQuery.CountAsync(cancellationToken),
            await myQuery.CountAsync(cancellationToken),
            await _db.AccreditationSubmissions.CountAsync(s => s.Status == AccreditationSubmissionStatus.Approved, cancellationToken),
            await _db.AccreditationSubmissions.CountAsync(s => s.Status == AccreditationSubmissionStatus.Rejected, cancellationToken),
            unclaimed,
            myApplications);
    }

    public async Task<PagedResult<AgencyAccreditationListItemDto>> ListAsync(
        int page,
        int pageSize,
        string? filter,
        CancellationToken cancellationToken = default)
    {
        var user = await AccreditationContextHelper.RequireAccreditationOfficerAsync(_db, _currentUser, cancellationToken);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = BuildFilteredQuery(filter, user.Id);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Include(s => s.User).ThenInclude(u => u.Profile)
            .Include(s => s.AssignedOfficer).ThenInclude(o => o!.Profile)
            .Include(s => s.History)
            .OrderByDescending(s => s.SubmittedAt ?? s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(s => MapListItem(s, user.Id)).ToList();

        return new PagedResult<AgencyAccreditationListItemDto>(items, page, pageSize, total);
    }

    public async Task<AgencyAccreditationDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await AccreditationContextHelper.RequireAccreditationOfficerAsync(_db, _currentUser, cancellationToken);
        var submission = await _db.AccreditationSubmissions
            .Include(s => s.User).ThenInclude(u => u.Profile)
            .Include(s => s.AssignedOfficer).ThenInclude(o => o!.Profile)
            .Include(s => s.Files).ThenInclude(f => f.Reviews)
            .Include(s => s.Files).ThenInclude(f => f.Versions)
            .Include(s => s.History).ThenInclude(h => h.Actor!).ThenInclude(a => a.Profile)
            .FirstOrDefaultAsync(s => s.Uuid == uuid, cancellationToken);

        if (submission is null) return null;

        var (formSchemaJson, formName) = await GetAccreditationFormMetaAsync(cancellationToken);
        var certificate = await AccreditationCertificateLookup.FindForSubmissionAsync(_db, submission.Uuid, cancellationToken: cancellationToken);
        return MapDetail(submission, user.Id, formSchemaJson, formName, certificate);
    }

    public async Task ClaimAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await AccreditationContextHelper.RequireAccreditationOfficerAsync(_db, _currentUser, cancellationToken);
        var submission = await _db.AccreditationSubmissions.FirstOrDefaultAsync(s => s.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Submission not found.");

        if (submission.Status != AccreditationSubmissionStatus.Submitted)
        {
            throw new ClientPortalException("NOT_CLAIMABLE", "Only submitted applications can be claimed.");
        }

        if (submission.AssignedOfficerUserId is not null)
        {
            throw new ClientPortalException("ALREADY_CLAIMED", "This application has already been claimed by another officer.");
        }

        submission.AssignedOfficerUserId = user.Id;
        submission.ClaimedAt = DateTime.UtcNow;
        submission.Status = AccreditationSubmissionStatus.UnderReview;
        submission.History.Add(new AccreditationHistory
        {
            Status = AccreditationSubmissionStatus.UnderReview,
            Comment = "Claimed for evaluation",
            ActorUserId = user.Id
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await AccreditationContextHelper.RequireAccreditationOfficerAsync(_db, _currentUser, cancellationToken);
        var submission = await _db.AccreditationSubmissions.FirstOrDefaultAsync(s => s.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Submission not found.");

        if (submission.AssignedOfficerUserId != user.Id)
        {
            throw new ClientPortalException("NOT_ASSIGNED", "You can only release applications assigned to you.");
        }

        submission.AssignedOfficerUserId = null;
        submission.ClaimedAt = null;
        submission.Status = AccreditationSubmissionStatus.Submitted;
        submission.History.Add(new AccreditationHistory
        {
            Status = AccreditationSubmissionStatus.Submitted,
            Comment = "Released back to the unclaimed review queue",
            ActorUserId = user.Id
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReviewFileAsync(Guid fileUuid, ReviewSubmissionFileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await AccreditationContextHelper.RequireAccreditationOfficerAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<EvaluationDecision>(request.Decision, true, out var decision))
        {
            throw new ClientPortalException("INVALID_DECISION", "Decision must be Approved, Rejected, or RevisionRequired.");
        }

        var file = await _db.SubmissionFiles
            .Include(f => f.Submission)
            .FirstOrDefaultAsync(f => f.Uuid == fileUuid, cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        EnsureCanReviewSubmission(file.Submission, user.Id);

        if (decision is EvaluationDecision.RevisionRequired or EvaluationDecision.Rejected &&
            string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new ClientPortalException("REMARKS_REQUIRED", "Remarks are required when marking a file for revision or rejection.");
        }

        var review = await _db.SubmissionFileReviews
            .FirstOrDefaultAsync(r => r.SubmissionFileId == file.Id && r.ReviewerUserId == user.Id, cancellationToken);

        if (review is null)
        {
            review = new SubmissionFileReview { SubmissionFileId = file.Id, ReviewerUserId = user.Id };
            _db.SubmissionFileReviews.Add(review);
        }

        review.Decision = decision;
        review.Comment = request.Comment;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredFileDownload> DownloadFileAsync(Guid submissionUuid, Guid fileUuid, CancellationToken cancellationToken = default)
    {
        var user = await AccreditationContextHelper.RequireAccreditationOfficerAsync(_db, _currentUser, cancellationToken);
        var file = await _db.SubmissionFiles
            .Include(f => f.Submission)
            .FirstOrDefaultAsync(f => f.Uuid == fileUuid && f.Submission.Uuid == submissionUuid, cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        EnsureCanReviewSubmission(file.Submission, user.Id);

        return new StoredFileDownload(
            StoredFilePathResolver.ResolveAccreditationFilePath(_fileStorage, submissionUuid, file.StoredFileName),
            file.ContentType,
            file.OriginalFileName);
    }

    public async Task<StoredFileDownload> DownloadFileVersionAsync(
        Guid submissionUuid,
        Guid fileUuid,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var user = await AccreditationContextHelper.RequireAccreditationOfficerAsync(_db, _currentUser, cancellationToken);
        var file = await _db.SubmissionFiles
            .Include(f => f.Submission)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Uuid == fileUuid && f.Submission.Uuid == submissionUuid, cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        EnsureCanReviewSubmission(file.Submission, user.Id);

        var version = file.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber)
            ?? throw new ClientPortalException("VERSION_NOT_FOUND", "Document version not found.");

        return new StoredFileDownload(
            StoredFilePathResolver.ResolveAccreditationFilePath(_fileStorage, submissionUuid, version.StoredFileName),
            version.ContentType,
            version.OriginalFileName);
    }

    public async Task<CompleteAccreditationReviewResponse> CompleteReviewAsync(Guid uuid, CompleteAccreditationReviewRequest request, CancellationToken cancellationToken = default)
    {
        var user = await AccreditationContextHelper.RequireAccreditationOfficerAsync(_db, _currentUser, cancellationToken);
        var submission = await _db.AccreditationSubmissions
            .Include(s => s.Files).ThenInclude(f => f.Reviews)
            .FirstOrDefaultAsync(s => s.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Submission not found.");

        EnsureCanReviewSubmission(submission, user.Id);

        EnsureAllDocumentsEvaluated(submission, user.Id);

        if (!Enum.TryParse<EvaluationDecision>(request.Decision, true, out var decision) ||
            decision is EvaluationDecision.Pending)
        {
            throw new ClientPortalException("INVALID_DECISION", "Decision must be Approved, Rejected, or RevisionRequired.");
        }

        if (decision == EvaluationDecision.Approved)
        {
            EnsureAllFilesApproved(submission, user.Id);
        }
        else if (HasBlockingFileReviews(submission, user.Id) && decision != EvaluationDecision.RevisionRequired)
        {
            throw new ClientPortalException(
                "INVALID_FINAL_DECISION",
                "This application can only be marked as Revision Required while documents are pending review, need revision, or were rejected.");
        }

        submission.Status = decision switch
        {
            EvaluationDecision.Approved => AccreditationSubmissionStatus.Approved,
            EvaluationDecision.Rejected => AccreditationSubmissionStatus.Rejected,
            EvaluationDecision.RevisionRequired => AccreditationSubmissionStatus.RevisionRequired,
            _ => submission.Status
        };

        submission.ReviewComments = request.Comment;
        if (decision == EvaluationDecision.Approved)
        {
            submission.AccreditationNumber = !string.IsNullOrWhiteSpace(request.AccreditationNumber)
                ? request.AccreditationNumber.Trim()
                : await _accreditationNumberGenerator.GenerateAsync(submission, cancellationToken);
        }

        submission.History.Add(new AccreditationHistory
        {
            Status = submission.Status,
            Comment = request.Comment,
            ActorUserId = user.Id
        });

        await _db.SaveChangesAsync(cancellationToken);

        AccreditationCertificateIssueResult? certificateResult = null;
        if (decision == EvaluationDecision.Approved)
        {
            certificateResult = await _accreditationCertificateService.TryIssueForApprovedSubmissionAsync(submission, user.Id, cancellationToken);
        }

        if (decision == EvaluationDecision.RevisionRequired)
        {
            await _notifications.NotifyAsync(
                submission.UserId,
                "accreditation_revision_required",
                "Accreditation revision required",
                $"Your accreditation application for {submission.CompanyName} requires document revisions.",
                "AccreditationSubmission",
                submission.Uuid.ToString(),
                cancellationToken);
        }

        return new CompleteAccreditationReviewResponse(
            submission.Status.ToString(),
            submission.AccreditationNumber,
            certificateResult?.Issued == true,
            certificateResult?.CertificateUuid,
            certificateResult?.CertificateNumber,
            certificateResult?.Message);
    }

    private IQueryable<AccreditationSubmission> BuildFilteredQuery(string? filter, long currentUserId)
    {
        var normalized = filter?.Trim().ToLowerInvariant() ?? "all";
        var query = _db.AccreditationSubmissions
            .Include(s => s.User).ThenInclude(u => u.Profile)
            .Include(s => s.AssignedOfficer).ThenInclude(o => o!.Profile)
            .Where(s => s.Status != AccreditationSubmissionStatus.Draft);

        return normalized switch
        {
            "unclaimed" => query.Where(s =>
                s.Status == AccreditationSubmissionStatus.Submitted && s.AssignedOfficerUserId == null),
            "submitted" => query.Where(s => s.Status == AccreditationSubmissionStatus.Submitted),
            "mine" or "my" => query.Where(s => s.AssignedOfficerUserId == currentUserId),
            "under_review" or "underreview" => query.Where(s => s.Status == AccreditationSubmissionStatus.UnderReview),
            "revision" or "revisionrequired" => query.Where(s => s.Status == AccreditationSubmissionStatus.RevisionRequired),
            "approved" => query.Where(s => s.Status == AccreditationSubmissionStatus.Approved),
            "rejected" => query.Where(s => s.Status == AccreditationSubmissionStatus.Rejected),
            _ => query,
        };
    }

    private static void EnsureCanReviewSubmission(AccreditationSubmission submission, long userId)
    {
        if (submission.AssignedOfficerUserId != userId)
        {
            throw new ClientPortalException("NOT_ASSIGNED", "This application is assigned to another accreditation officer.");
        }
    }

    private static void EnsureAllDocumentsEvaluated(AccreditationSubmission submission, long reviewerId)
    {
        if (submission.Files.Count == 0)
        {
            throw new ClientPortalException("NO_FILES", "Cannot complete review for an application with no submitted documents.");
        }

        foreach (var file in submission.Files)
        {
            var review = file.Reviews.FirstOrDefault(r => r.ReviewerUserId == reviewerId);
            if (review is null)
            {
                throw new ClientPortalException(
                    "DOCUMENTS_NOT_EVALUATED",
                    "All required documents must be reviewed before submitting the application outcome.");
            }
        }
    }

    private static void EnsureAllFilesApproved(AccreditationSubmission submission, long reviewerId)
    {
        if (submission.Files.Count == 0)
        {
            throw new ClientPortalException("NO_FILES", "Cannot approve an application with no submitted documents.");
        }

        foreach (var file in submission.Files)
        {
            var review = file.Reviews.FirstOrDefault(r => r.ReviewerUserId == reviewerId);
            if (review?.Decision != EvaluationDecision.Approved)
            {
                throw new ClientPortalException(
                    "FILES_NOT_ALL_APPROVED",
                    "All documents must be approved before the application can be approved.");
            }
        }
    }

    private static bool HasBlockingFileReviews(AccreditationSubmission submission, long reviewerId)
    {
        if (submission.Files.Count == 0) return false;

        foreach (var file in submission.Files)
        {
            var review = file.Reviews.FirstOrDefault(r => r.ReviewerUserId == reviewerId);
            if (review is null || review.Decision != EvaluationDecision.Approved)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<(string? SchemaJson, string? FormName)> GetAccreditationFormMetaAsync(CancellationToken cancellationToken)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Versions)
            .Where(t => t.IsActive && t.Status == FormTemplateStatus.Published && t.FormType == "ACCREDITATION")
            .OrderByDescending(t => t.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null) return (null, null);

        var version = template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        return (version?.SchemaJson, template.Name);
    }

    private static AgencyAccreditationListItemDto MapListItem(AccreditationSubmission submission, long currentUserId) =>
        new(
            submission.Uuid,
            submission.CompanyName,
            submission.SubmissionType,
            submission.Status.ToString(),
            AccreditationSubmissionStatusMapper.GetDisplayStatus(submission),
            submission.User.Profile is not null
                ? $"{submission.User.Profile.FirstName} {submission.User.Profile.LastName}"
                : submission.User.Email,
            submission.SubmittedAt,
            submission.AssignedOfficer?.Profile is not null
                ? $"{submission.AssignedOfficer.Profile.FirstName} {submission.AssignedOfficer.Profile.LastName}"
                : submission.AssignedOfficer?.Email,
            submission.ClaimedAt,
            submission.AssignedOfficerUserId == currentUserId);

    private static AgencyAccreditationDetailDto MapDetail(
        AccreditationSubmission s,
        long reviewerId,
        string? formSchemaJson,
        string? formName,
        Certificate? certificate = null) =>
        new(
            s.Uuid,
            s.CompanyName,
            s.SubmissionType,
            s.Status.ToString(),
            s.User.Profile is not null ? $"{s.User.Profile.FirstName} {s.User.Profile.LastName}" : s.User.Email,
            s.FormDataJson,
            s.ReviewComments,
            s.AssignedOfficer?.Profile is not null
                ? $"{s.AssignedOfficer.Profile.FirstName} {s.AssignedOfficer.Profile.LastName}"
                : s.AssignedOfficer?.Email,
            s.ClaimedAt,
            s.SubmittedAt,
            s.AccreditationNumber,
            formSchemaJson,
            formName,
            s.AssignedOfficerUserId == reviewerId,
            s.Status == AccreditationSubmissionStatus.Submitted && s.AssignedOfficerUserId is null,
            certificate?.Uuid,
            certificate?.CertificateNumber,
            s.Files.Select(f =>
            {
                var review = f.Reviews.FirstOrDefault(r => r.ReviewerUserId == reviewerId);
                var versions = f.Versions
                    .OrderByDescending(v => v.VersionNumber)
                    .Select(v => new SubmissionFileVersionDto(
                        v.VersionNumber,
                        v.OriginalFileName,
                        v.FileSizeBytes,
                        v.CreatedAt,
                        v.StoredFileName == f.StoredFileName))
                    .ToList();
                return new AgencySubmissionFileDto(
                    f.Uuid,
                    f.OriginalFileName,
                    review?.Decision.ToString(),
                    review?.Comment,
                    f.FileSizeBytes,
                    f.CreatedAt,
                    versions);
            }).ToList(),
            s.History.OrderByDescending(h => h.CreatedAt).Select(h => AccreditationHistoryMapper.ForOfficer(h)).ToList());
}

public class SecretaryReportService : ISecretaryReportService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SecretaryReportService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SecretaryReportDto> GetReportAsync(CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entries = _db.Entries.Where(e => e.AgencyId == agency.Id);

        return new SecretaryReportDto(
            await entries.CountAsync(cancellationToken),
            await entries.CountAsync(e => e.Status == EntryStatus.Submitted, cancellationToken),
            await entries.CountAsync(e => e.Status == EntryStatus.UnderReview, cancellationToken),
            await entries.CountAsync(e => e.Status == EntryStatus.Approved, cancellationToken),
            await entries.CountAsync(e => e.Status == EntryStatus.Rejected, cancellationToken),
            await _db.Inspections.CountAsync(i => i.AgencyId == agency.Id && i.Status == InspectionStatus.Completed, cancellationToken),
            await _db.AccreditationSubmissions.CountAsync(s =>
                s.Status == AccreditationSubmissionStatus.Submitted || s.Status == AccreditationSubmissionStatus.UnderReview, cancellationToken),
            await _db.AgencyBillings.CountAsync(b => b.AgencyId == agency.Id && b.Status == AgencyBillingStatus.Issued, cancellationToken),
            await _db.AgencyBillings.CountAsync(b => b.AgencyId == agency.Id && b.Status == AgencyBillingStatus.Paid, cancellationToken));
    }
}
