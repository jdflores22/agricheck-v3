using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.MavPortal;
using AgriCheck.Application.MavPortal.Dtos;
using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class EntryService : IEntryService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly INotificationService _notifications;
    private readonly IMavMicService _micService;

    public EntryService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        INotificationService notifications,
        IMavMicService micService)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _notifications = notifications;
        _micService = micService;
    }

    public async Task<PagedResult<EntryListItemDto>> ListAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var query = _db.Entries
            .Include(e => e.Agency)
            .Include(e => e.Detail)
            .Where(e => e.UserId == user.Id);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EntryStatus>(status, true, out var parsed))
        {
            query = query.Where(e => e.Status == parsed);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EntryListItemDto(
                e.Uuid, e.ReferenceNo, e.EntryType.ToString(), e.Status.ToString(), e.Agency.Code,
                e.Detail != null ? e.Detail.CommodityName : null, e.CreatedAt, e.UpdatedAt, e.SubmittedAt,
                e.PaymentStatus.ToString(), e.PaymentAmount, e.ComplianceDeadlineAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<EntryListItemDto>(items, page, pageSize, total);
    }

    public async Task<EntryDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await QueryEntryGraph().FirstOrDefaultAsync(e => e.Uuid == uuid && e.UserId == user.Id, cancellationToken);
        return entry is null ? null : MapEntry(entry);
    }

    public async Task<EntryDto> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await UserContextHelper.RequireAccreditedAsync(_db, user.Id, cancellationToken);
        var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == request.AgencyId && a.IsActive, cancellationToken)
            ?? throw new ClientPortalException("AGENCY_NOT_FOUND", "Agency not found.");

        if (!Enum.TryParse<EntryType>(request.EntryType, true, out var entryType))
        {
            throw new ClientPortalException("INVALID_ENTRY_TYPE", "Entry type must be Import or Export.");
        }

        var entry = new Entry
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = await ReferenceNumberGenerator.EntryAsync(_db, cancellationToken),
            UserId = user.Id,
            AgencyId = agency.Id,
            EntryType = entryType,
            Status = EntryStatus.Draft,
            Notes = request.Notes,
            FormDataJson = NormalizeFormDataJson(request.FormDataJson),
            Detail = MapDetailInput(request.Detail)
        };

        if (entryType == EntryType.Import)
        {
            entry.ImportTrack = EntryMavHelper.ResolveImportTrack(request.ImportTrack, request.FormDataJson);
            if (entry.ImportTrack == EntryImportTrack.Mav && request.MavNo is not null)
            {
                await EntryMavHelper.EnsureMavNoAvailableAsync(_db, request.MavNo, null, cancellationToken);
                entry.MavNo = EntryMavHelper.NormalizeMavNo(request.MavNo);
            }
        }

        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "entry_created",
            Title = "Entry created",
            Description = $"Draft entry {entry.ReferenceNo} created.",
            ActorUserId = user.Id
        });

        _db.Entries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        await EntryContainerSyncService.SyncAsync(_db, entry, request.NumContainers, request.ContainersJson, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return MapEntry(await QueryEntryGraph().FirstAsync(e => e.Id == entry.Id, cancellationToken));
    }

    public async Task<EntryDto> UpdateAsync(Guid uuid, UpdateEntryRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.Include(e => e.Detail).Include(e => e.Containers).FirstOrDefaultAsync(e => e.Uuid == uuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.Status != EntryStatus.Draft)
        {
            throw new ClientPortalException("ENTRY_NOT_EDITABLE", "Only draft entries can be edited.");
        }

        entry.Notes = request.Notes;
        entry.FormDataJson = NormalizeFormDataJson(request.FormDataJson);
        if (entry.Detail is null)
        {
            entry.Detail = MapDetailInput(request.Detail);
        }
        else
        {
            entry.Detail.CommodityId = request.Detail.CommodityId;
            entry.Detail.CommodityName = request.Detail.CommodityName;
            entry.Detail.Description = request.Detail.Description;
            entry.Detail.Quantity = request.Detail.Quantity;
            entry.Detail.Unit = request.Detail.Unit;
            entry.Detail.OriginCountry = request.Detail.OriginCountry;
            entry.Detail.DestinationCountry = request.Detail.DestinationCountry;
            entry.Detail.PortOfEntry = request.Detail.PortOfEntry;
        }

        if (entry.EntryType == EntryType.Import)
        {
            var nextTrack = EntryMavHelper.ResolveImportTrack(request.ImportTrack, request.FormDataJson, entry.ImportTrack);
            EntryMavHelper.EnsureImportTrackChangeAllowed(entry, nextTrack);
            entry.ImportTrack = nextTrack;
            if (entry.ImportTrack == EntryImportTrack.Mav && request.MavNo is not null)
            {
                await EntryMavHelper.EnsureMavNoAvailableAsync(_db, request.MavNo, entry.Id, cancellationToken);
                entry.MavNo = EntryMavHelper.NormalizeMavNo(request.MavNo);
            }
            else if (entry.ImportTrack == EntryImportTrack.Regular)
            {
                entry.MavNo = null;
                entry.MavRemarks = null;
                entry.MavDocumentStatus = EntryMavDocumentStatus.NotProvided;
            }
        }

        await EntryContainerSyncService.SyncAsync(_db, entry, request.NumContainers, request.ContainersJson, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return MapEntry(await QueryEntryGraph().FirstAsync(e => e.Id == entry.Id, cancellationToken));
    }

    public async Task<EntryDto> SubmitAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        await UserContextHelper.RequireAccreditedAsync(_db, user.Id, cancellationToken);
        var entry = await _db.Entries
            .Include(e => e.Detail)
            .Include(e => e.Files)
            .Include(e => e.Containers)
            .FirstOrDefaultAsync(e => e.Uuid == uuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.Status != EntryStatus.Draft)
        {
            throw new ClientPortalException("ENTRY_ALREADY_SUBMITTED", "Entry has already been submitted.");
        }

        await EntryFormValidator.ValidateDraftCompletenessAsync(_db, entry, cancellationToken);
        await EntryContainerSyncService.ValidateOnSubmitAsync(_db, entry, cancellationToken);
        await EntryMavHelper.ValidateImportMavOnSubmitAsync(_db, entry, cancellationToken);

        if (EntryMavHelper.IsMavTrack(entry) && entry.MavDocumentStatus == EntryMavDocumentStatus.NotProvided)
        {
            entry.MavDocumentStatus = EntryMavDocumentStatus.PendingReview;
        }

        var previous = entry.Status;
        var processingFee = await ResolveProcessingFeeAsync(entry.AgencyId, entry.EntryType, cancellationToken);
        entry.Status = EntryStatus.Submitted;
        entry.SubmittedAt = DateTime.UtcNow;
        entry.PaymentAmount = processingFee;
        entry.PaymentStatus = PaymentStatus.Unpaid;

        entry.StatusHistory.Add(new EntryStatusHistory
        {
            FromStatus = previous,
            ToStatus = EntryStatus.Submitted,
            ChangedByUserId = user.Id,
            Comment = "Entry submitted by applicant"
        });

        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "entry_submitted",
            Title = "Entry submitted",
            Description = "Entry submitted for agency review.",
            ActorUserId = user.Id
        });

        var bill = new ClientBill
        {
            Uuid = Guid.NewGuid(),
            UserId = user.Id,
            EntryId = entry.Id,
            BillNumber = await ReferenceNumberGenerator.BillAsync(_db, cancellationToken),
            Description = $"Processing fee for {entry.ReferenceNo}",
            Amount = processingFee,
            Status = ClientBillStatus.Unpaid,
            DueDate = DateTime.UtcNow.AddDays(7),
            PaymentLinkToken = Guid.NewGuid().ToString("N")
        };
        _db.ClientBills.Add(bill);

        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyAsync(
            user.Id,
            "entry_submitted",
            "Entry submitted",
            $"Entry {entry.ReferenceNo} was submitted for agency review.",
            "Entry",
            entry.Uuid.ToString(),
            cancellationToken);

        return MapEntry(await QueryEntryGraph().FirstAsync(e => e.Id == entry.Id, cancellationToken));
    }

    public async Task<CheckMavNoResultDto> CheckMavNoAsync(string mavNo, Guid? excludeEntryUuid, CancellationToken cancellationToken = default)
    {
        await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var normalized = EntryMavHelper.NormalizeMavNo(mavNo);
        if (normalized is null)
        {
            return new CheckMavNoResultDto(false, "MAV No. is required.");
        }

        long? excludeEntryId = null;
        if (excludeEntryUuid.HasValue)
        {
            excludeEntryId = await _db.Entries
                .Where(e => e.Uuid == excludeEntryUuid.Value)
                .Select(e => (long?)e.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var taken = await _db.Entries.AnyAsync(
            e => e.MavNo == normalized && (excludeEntryId == null || e.Id != excludeEntryId),
            cancellationToken);

        return taken
            ? new CheckMavNoResultDto(false, "MAV No. is already used by another entry.")
            : new CheckMavNoResultDto(true, "MAV No. is available.");
    }

    public async Task<EntryDto> UpdateMavNoAsync(Guid uuid, UpdateEntryMavRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Uuid == uuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.Status != EntryStatus.Draft)
        {
            throw new ClientPortalException("ENTRY_NOT_EDITABLE", "Only draft entries can be edited.");
        }

        if (entry.EntryType != EntryType.Import)
        {
            throw new ClientPortalException("INVALID_ENTRY_TYPE", "MAV No. applies to import entries only.");
        }

        await EntryMavHelper.EnsureMavNoAvailableAsync(_db, request.MavNo, entry.Id, cancellationToken);
        entry.ImportTrack = EntryImportTrack.Mav;
        entry.MavNo = EntryMavHelper.NormalizeMavNo(request.MavNo);
        await _db.SaveChangesAsync(cancellationToken);
        return MapEntry(await QueryEntryGraph().FirstAsync(e => e.Id == entry.Id, cancellationToken));
    }

    public async Task<EntryDto> UtilizeMicAsync(Guid uuid, UtilizeEntryMicRequest request, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Uuid == uuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.Status != EntryStatus.Draft)
        {
            throw new ClientPortalException("ENTRY_NOT_EDITABLE", "MIC can only be linked while the entry is a draft.");
        }

        if (entry.EntryType != EntryType.Import)
        {
            throw new ClientPortalException("INVALID_ENTRY_TYPE", "MIC utilization applies to import entries only.");
        }

        entry.ImportTrack = EntryImportTrack.Mav;
        await _micService.UtilizeAsync(request.MicUuid, new UtilizeMicRequest(uuid, request.Volume), cancellationToken);

        entry = await QueryEntryGraph().FirstAsync(e => e.Id == entry.Id, cancellationToken);
        if (entry.PrimaryMicId is null)
        {
            var latestUtilization = entry.MicUtilizations.OrderByDescending(u => u.UtilizedAt).FirstOrDefault();
            if (latestUtilization is not null)
            {
                entry.PrimaryMicId = latestUtilization.MicId;
                await _db.SaveChangesAsync(cancellationToken);
                entry = await QueryEntryGraph().FirstAsync(e => e.Id == entry.Id, cancellationToken);
            }
        }

        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "mic_utilized",
            Title = "MIC volume linked",
            Description = $"Linked {request.Volume:0.###} volume from MIC to this entry.",
            ActorUserId = user.Id
        });
        await _db.SaveChangesAsync(cancellationToken);

        return MapEntry(await QueryEntryGraph().FirstAsync(e => e.Id == entry.Id, cancellationToken));
    }

    public async Task<EntryFileDto> UploadComplianceFileAsync(Guid entryUuid, Guid fileUuid, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var file = await _db.EntryFiles
            .Include(f => f.Entry)
            .Include(f => f.Evaluations)
            .FirstOrDefaultAsync(f => f.Uuid == fileUuid && f.Entry.Uuid == entryUuid && f.Entry.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        if (file.Entry.Status != EntryStatus.ForCompliance)
            throw new ClientPortalException("ENTRY_NOT_EDITABLE", "Compliance uploads are only allowed for entries marked for compliance.");

        var latestEval = file.Evaluations.OrderByDescending(e => e.UpdatedAt).FirstOrDefault();
        if (latestEval?.Decision != EvaluationDecision.RevisionRequired)
            throw new ClientPortalException("FILE_NOT_EDITABLE", "This file does not require a compliance revision.");

        var (storedFileName, size) = await _fileStorage.SaveAsync(fileStream, $"entries/{entryUuid}/compliance", fileName, cancellationToken);
        await ArchiveEntryFileVersionAsync(file, user.Id, cancellationToken);

        file.StoredFileName = storedFileName;
        file.OriginalFileName = fileName;
        file.ContentType = contentType;
        file.FileSizeBytes = size;

        var nextVersion = await NextEntryFileVersionNumberAsync(file.Id, cancellationToken);
        _db.EntryFileVersions.Add(new EntryFileVersion
        {
            Uuid = Guid.NewGuid(),
            EntryFileId = file.Id,
            VersionNumber = nextVersion,
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = size,
            UploadedByUserId = user.Id
        });

        foreach (var evaluation in file.Evaluations.Where(e => e.Decision == EvaluationDecision.RevisionRequired))
        {
            evaluation.Decision = EvaluationDecision.Pending;
            evaluation.Comment = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        file = await _db.EntryFiles.Include(f => f.Versions).Include(f => f.Evaluations).FirstAsync(f => f.Id == file.Id, cancellationToken);
        return MapFile(file);
    }

    public async Task<EntryDto> ResubmitComplianceAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await QueryEntryGraph().FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.Status != EntryStatus.ForCompliance)
            throw new ClientPortalException("INVALID_STATUS", "Only entries marked for compliance can be resubmitted.");

        var pendingRevisions = entry.Files
            .SelectMany(f => f.Evaluations)
            .Any(e => e.Decision == EvaluationDecision.RevisionRequired);
        if (pendingRevisions)
            throw new ClientPortalException("COMPLIANCE_INCOMPLETE", "Upload revised documents for all flagged files before resubmitting.");

        var previous = entry.Status;
        entry.Status = EntryStatus.UnderReview;
        entry.StatusHistory.Add(new EntryStatusHistory
        {
            FromStatus = previous,
            ToStatus = EntryStatus.UnderReview,
            ChangedByUserId = user.Id,
            Comment = "Compliance documents resubmitted by applicant"
        });
        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "compliance_resubmitted",
            Title = "Compliance resubmitted",
            Description = "Revised documents submitted for agency review.",
            ActorUserId = user.Id
        });

        await _db.SaveChangesAsync(cancellationToken);

        await AgencyEvaluatorNotificationHelper.NotifyComplianceResubmittedAsync(
            _db,
            _notifications,
            entry,
            cancellationToken);

        return MapEntry(entry);
    }

    public async Task<EntryFileDto> UploadFileAsync(Guid entryUuid, Stream fileStream, string fileName, string contentType, string? documentType, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.Status != EntryStatus.Draft)
        {
            throw new ClientPortalException("ENTRY_NOT_EDITABLE", "Files can only be uploaded to draft entries.");
        }

        var (storedFileName, size) = await _fileStorage.SaveAsync(fileStream, $"entries/{entry.Uuid}", fileName, cancellationToken);
        var file = new EntryFile
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = size,
            DocumentType = documentType
        };

        _db.EntryFiles.Add(file);
        await _db.SaveChangesAsync(cancellationToken);

        if (string.Equals(documentType, EntryMavHelper.MavCertificateDocumentType, StringComparison.OrdinalIgnoreCase)
            && entry.EntryType == EntryType.Import)
        {
            entry.ImportTrack = EntryImportTrack.Mav;
            entry.MavDocumentStatus = EntryMavDocumentStatus.PendingReview;
            entry.MavRemarks = null;
            entry.TimelineEvents.Add(new TimelineEvent
            {
                EventType = "mav_certificate_uploaded",
                Title = "MAV certificate uploaded",
                Description = "MAV certificate document attached to entry.",
                ActorUserId = user.Id
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        _db.EntryFileVersions.Add(new EntryFileVersion
        {
            Uuid = Guid.NewGuid(),
            EntryFileId = file.Id,
            VersionNumber = 1,
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = size,
            UploadedByUserId = user.Id
        });
        await _db.SaveChangesAsync(cancellationToken);

        file = await _db.EntryFiles.Include(f => f.Versions).Include(f => f.Evaluations).FirstAsync(f => f.Id == file.Id, cancellationToken);
        return MapFile(file);
    }

    public async Task<StoredFileDownload> DownloadFileAsync(Guid entryUuid, Guid fileUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var file = await _db.EntryFiles
            .Include(f => f.Entry)
            .FirstOrDefaultAsync(f => f.Uuid == fileUuid && f.Entry.Uuid == entryUuid && f.Entry.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("FILE_NOT_FOUND", "File not found.");

        return new StoredFileDownload(
            StoredFilePathResolver.ResolveEntryFilePath(_fileStorage, entryUuid, file.StoredFileName),
            file.ContentType,
            file.OriginalFileName);
    }

    private IQueryable<Entry> QueryEntryGraph() =>
        _db.Entries
            .Include(e => e.Agency)
            .Include(e => e.Detail)
            .Include(e => e.Files).ThenInclude(f => f.Evaluations)
            .Include(e => e.Files).ThenInclude(f => f.Versions)
            .Include(e => e.StatusHistory)
            .Include(e => e.TimelineEvents)
            .Include(e => e.Bills)
            .Include(e => e.Containers)
            .Include(e => e.MicUtilizations).ThenInclude(u => u.Mic);

    private async Task<int> NextEntryFileVersionNumberAsync(long entryFileId, CancellationToken cancellationToken)
    {
        var max = await _db.EntryFileVersions.Where(v => v.EntryFileId == entryFileId).MaxAsync(v => (int?)v.VersionNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    private async Task ArchiveEntryFileVersionAsync(EntryFile file, long userId, CancellationToken cancellationToken)
    {
        if (await _db.EntryFileVersions.AnyAsync(v => v.EntryFileId == file.Id && v.StoredFileName == file.StoredFileName, cancellationToken))
            return;

        var versionNumber = await NextEntryFileVersionNumberAsync(file.Id, cancellationToken);
        _db.EntryFileVersions.Add(new EntryFileVersion
        {
            Uuid = Guid.NewGuid(),
            EntryFileId = file.Id,
            VersionNumber = versionNumber,
            OriginalFileName = file.OriginalFileName,
            StoredFileName = file.StoredFileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.FileSizeBytes,
            UploadedByUserId = userId
        });
    }

    private async Task<decimal> ResolveProcessingFeeAsync(long agencyId, EntryType entryType, CancellationToken cancellationToken)
    {
        return await PaymentSettingsReader.ResolveEntryProcessingFeeForAgencyAsync(_db, agencyId, entryType, cancellationToken);
    }

    private static EntryFileDto MapFile(EntryFile file)
    {
        var evaluation = file.Evaluations.OrderByDescending(e => e.UpdatedAt).FirstOrDefault();
        var versions = file.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new EntryFileVersionDto(
                v.VersionNumber,
                v.OriginalFileName,
                v.FileSizeBytes,
                v.CreatedAt,
                v.StoredFileName == file.StoredFileName))
            .ToList();
        return new EntryFileDto(
            file.Uuid,
            file.OriginalFileName,
            file.ContentType,
            file.FileSizeBytes,
            file.DocumentType,
            file.CreatedAt,
            evaluation?.Decision.ToString(),
            evaluation?.Comment,
            versions);
    }

    private static EntryDetail MapDetailInput(EntryDetailInputDto input) => new()
    {
        CommodityId = input.CommodityId,
        CommodityName = input.CommodityName,
        Description = input.Description,
        Quantity = input.Quantity,
        Unit = input.Unit,
        OriginCountry = input.OriginCountry,
        DestinationCountry = input.DestinationCountry,
        PortOfEntry = input.PortOfEntry
    };

    private static string? NormalizeFormDataJson(string? formDataJson)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return null;
        }

        var trimmed = formDataJson.Trim();
        return trimmed is "{}" or "null" ? null : trimmed;
    }

    private static EntryDto MapEntry(Entry entry) => new(
        entry.Uuid,
        entry.ReferenceNo,
        entry.EntryType.ToString(),
        entry.Status.ToString(),
        entry.AgencyId,
        entry.Agency.Code,
        entry.Agency.Name,
        entry.Notes,
        entry.FormDataJson,
        entry.SubmittedAt,
        entry.PaymentStatus.ToString(),
        entry.PaymentAmount,
        entry.ComplianceDeadlineAt,
        entry.Detail is null ? null : new EntryDetailDto(
            entry.Detail.CommodityId, entry.Detail.CommodityName, entry.Detail.Description, entry.Detail.Quantity,
            entry.Detail.Unit, entry.Detail.OriginCountry, entry.Detail.DestinationCountry, entry.Detail.PortOfEntry),
        entry.Files.OrderByDescending(f => f.CreatedAt).Select(MapFile).ToList(),
        entry.Containers.OrderBy(c => c.SequenceNumber).Select(c => new EntryContainerDto(
            c.Uuid, c.SequenceNumber, c.ContainerNumber, c.ContainerType, c.FormDataJson)).ToList(),
        entry.StatusHistory.OrderByDescending(h => h.CreatedAt).Select(h => new StatusHistoryDto(h.FromStatus.ToString(), h.ToStatus.ToString(), h.Comment, h.CreatedAt)).ToList(),
        entry.TimelineEvents.OrderByDescending(t => t.CreatedAt).Select(t => new TimelineEventDto(t.EventType, t.Title, t.Description, t.CreatedAt)).ToList(),
        entry.Bills.OrderByDescending(b => b.CreatedAt).Select(b => new ClientBillSummaryDto(b.Uuid, b.BillNumber, b.Description, b.Amount, b.Status.ToString(), b.DueDate, b.PaidAt)).ToList(),
        EntryMavHelper.MapMavInfo(entry));
}
