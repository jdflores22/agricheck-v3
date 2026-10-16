using AgriCheck.Application.Notifications;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgriCheck.Infrastructure.Services;

public interface IClientDaBillingService
{
    Task<IReadOnlyList<ClientDaBillingDto>> ListForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default);
    Task<ClientDaBillingDto> UploadPaymentProofAsync(
        Guid billingUuid,
        Stream fileStream,
        string fileName,
        string contentType,
        UploadDaBillingPaymentRequest request,
        CancellationToken cancellationToken = default);
}

public class ClientDaBillingService : IClientDaBillingService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public ClientDaBillingService(AgriCheckDbContext db, ICurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<ClientDaBillingDto>> ListForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var billings = await _db.AgencyBillings
            .Include(b => b.Entry)
            .Include(b => b.Charges)
            .Where(b => b.Entry!.Uuid == entryUuid && b.Entry.UserId == user.Id)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return billings.Select(Map).ToList();
    }

    public async Task<ClientDaBillingDto> UploadPaymentProofAsync(
        Guid billingUuid,
        Stream fileStream,
        string fileName,
        string contentType,
        UploadDaBillingPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var billing = await _db.AgencyBillings
            .Include(b => b.Entry).ThenInclude(e => e!.TimelineEvents)
            .Include(b => b.Charges)
            .FirstOrDefaultAsync(b => b.Uuid == billingUuid && b.Entry!.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Billing not found.");

        if (billing.Status is not AgencyBillingStatus.Issued and not AgencyBillingStatus.PaymentPending)
        {
            throw new ClientPortalException("INVALID_STATUS", "Payment proof can only be uploaded for issued billings.");
        }

        var (storedFileName, _) = await _fileStorage.SaveAsync(
            fileStream, $"da-billing/{billing.Uuid}", fileName, cancellationToken);

        billing.PaymentReference = request.PaymentReference.Trim();
        billing.PaymentNotes = request.Notes?.Trim();
        billing.PaymentProofStoredFileName = storedFileName;
        billing.PaymentProofOriginalFileName = fileName;
        billing.PaymentProofContentType = contentType;
        billing.PaymentUploadedAt = DateTime.UtcNow;
        billing.Status = AgencyBillingStatus.PaymentPending;

        if (billing.Entry is not null)
        {
            billing.Entry.TimelineEvents.Add(new TimelineEvent
            {
                EventType = "da_billing_proof_uploaded",
                Title = "DA payment proof uploaded",
                Description = $"Payment reference {billing.PaymentReference} submitted for agency verification.",
                ActorUserId = user.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Map(billing);
    }

    private static ClientDaBillingDto Map(AgencyBilling b) => new(
        b.Uuid,
        b.Entry!.Uuid,
        b.Entry.ReferenceNo,
        b.BillNumber,
        b.Description,
        b.Amount,
        b.Status.ToString(),
        AgencyBillingStatusMapper.GetClientDisplayStatus(b),
        b.IssuedAt,
        b.PaidAt,
        b.PaymentReference,
        b.PaymentProofOriginalFileName,
        b.PaymentUploadedAt,
        b.VerificationNotes,
        b.Charges
            .OrderBy(c => c.SortOrder)
            .Select(c => new ClientDaBillingChargeDto(c.Description, c.Amount, c.SortOrder))
            .ToList());
}

public interface IContainerInspectionWorkflowService
{
    Task<IReadOnlyList<ClientContainerInspectionStatusDto>> ListForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientContainerInspectionStatusDto>> ListForAgencyEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default);
    Task<PagedResult<AgencyContainerInspectionQueueItemDto>> ListInspectionQueueForAgencyAsync(
        int page,
        int pageSize,
        string scope = "unclaimed",
        CancellationToken cancellationToken = default);
    Task<AgencyContainerInspectionDetailDto> ClaimContainerInspectionAsync(Guid containerUuid, CancellationToken cancellationToken = default);
    Task<AgencyContainerInspectionDetailDto> GetContainerInspectionDetailAsync(Guid containerUuid, CancellationToken cancellationToken = default);
    Task<AgencyContainerInspectionDetailDto> GetContainerDetailForAgencyTransportAsync(Guid containerUuid, CancellationToken cancellationToken = default);
    Task<StoredFileDownload?> GetEntryCertificatePdfForAgencyAsync(Guid entryUuid, CancellationToken cancellationToken = default);
    Task<ClientContainerInspectionPhotoDto> UploadPhotoAsync(
        Guid containerUuid,
        ContainerInspectionPhotoType photoType,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
    Task<ClientContainerInspectionPhotoDto> ReviewPhotoAsync(
        Guid photoUuid,
        ReviewContainerInspectionPhotoRequest request,
        CancellationToken cancellationToken = default);
    Task<StoredFileDownload> DownloadPhotoForAgencyAsync(Guid photoUuid, CancellationToken cancellationToken = default);
    Task<StoredFileDownload> DownloadPhotoForClientAsync(Guid entryUuid, Guid photoUuid, CancellationToken cancellationToken = default);
    Task<AgencyContainerInspectionDetailDto> CompleteContainerInspectionAsync(
        Guid containerUuid,
        CompleteContainerInspectionRequest request,
        CancellationToken cancellationToken = default);
}

public class ContainerInspectionWorkflowService : IContainerInspectionWorkflowService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IEntryWorkflowService _workflow;
    private readonly INotificationService _notifications;

    public ContainerInspectionWorkflowService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IEntryWorkflowService workflow,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _workflow = workflow;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<ClientContainerInspectionStatusDto>> ListForEntryAsync(
        Guid entryUuid,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var containers = await _db.Containers
            .Include(c => c.InspectionPhotos)
            .Include(c => c.Entry)
            .Where(c => c.Entry.Uuid == entryUuid && c.Entry.UserId == user.Id)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync(cancellationToken);

        return containers.Select(MapStatus).ToList();
    }

    public async Task<IReadOnlyList<ClientContainerInspectionStatusDto>> ListForAgencyEntryAsync(
        Guid entryUuid,
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var containers = await _db.Containers
            .Include(c => c.InspectionPhotos)
            .Include(c => c.Entry)
            .Where(c => c.Entry.Uuid == entryUuid && c.Entry.AgencyId == agency.Id)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync(cancellationToken);

        if (containers.Count == 0)
        {
            throw new ClientPortalException("NOT_FOUND", "Entry not found.");
        }

        return containers.Select(MapStatus).ToList();
    }

    public async Task<PagedResult<AgencyContainerInspectionQueueItemDto>> ListInspectionQueueForAgencyAsync(
        int page,
        int pageSize,
        string scope = "unclaimed",
        CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var containers = await _db.Containers
            .Include(c => c.InspectionPhotos)
            .Include(c => c.InspectorAssignments).ThenInclude(a => a.Inspector).ThenInclude(u => u.Profile)
            .Include(c => c.Entry).ThenInclude(e => e.User).ThenInclude(u => u.Profile)
            .Include(c => c.Entry).ThenInclude(e => e.TimelineEvents)
            .Where(c => c.Entry.AgencyId == agency.Id && c.Entry.Status == EntryStatus.ForInspection)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

        var normalizedScope = scope.Trim().ToLowerInvariant();
        var queue = containers
            .Where(ContainerInspectionQueueHelper.NeedsInspectorAction)
            .Where(container => normalizedScope switch
            {
                "mine" => ContainerInspectionQueueHelper.IsAssignedTo(container, user.Id),
                "all" => true,
                _ => !ContainerInspectionQueueHelper.HasActiveAssignment(container),
            })
            .Select(container => MapQueueItem(
                container,
                ContainerInspectionQueueHelper.IsUploadComplete(container),
                ContainerInspectionQueueHelper.IsApproved(container),
                ContainerInspectionQueueHelper.CountPendingPhotos(container),
                user.Id))
            .OrderByDescending(item => item.SubmittedAt)
            .ToList();

        var total = queue.Count;
        var items = queue
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<AgencyContainerInspectionQueueItemDto>(items, page, pageSize, total);
    }

    public async Task<AgencyContainerInspectionDetailDto> ClaimContainerInspectionAsync(
        Guid containerUuid,
        CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.InspectionPhotos)
            .Include(c => c.InspectorAssignments)
            .Include(c => c.Entry).ThenInclude(e => e.TimelineEvents)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (!ContainerInspectionQueueHelper.NeedsInspectorAction(container))
        {
            throw new ClientPortalException("NOT_AVAILABLE", "This container is not available for inspection review.");
        }

        if (ContainerInspectionQueueHelper.HasActiveAssignment(container))
        {
            throw new ClientPortalException("ALREADY_ASSIGNED", "This container has already been claimed by another inspector.");
        }

        _db.InspectorAssignments.Add(new InspectorAssignment
        {
            Uuid = Guid.NewGuid(),
            ContainerId = container.Id,
            AgencyId = agency.Id,
            InspectorUserId = user.Id,
            Status = AssignmentStatus.Active,
            AssignedAt = DateTime.UtcNow,
        });

        container.Entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "container_inspection_claimed",
            Title = $"Container {container.ContainerNumber} claimed for review",
            Description = "An inspector claimed this container for photo review.",
            ActorUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await GetContainerInspectionDetailAsync(containerUuid, cancellationToken);
    }

    public async Task<AgencyContainerInspectionDetailDto> GetContainerInspectionDetailAsync(
        Guid containerUuid,
        CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.InspectionPhotos).ThenInclude(p => p.ReviewedBy).ThenInclude(u => u!.Profile)
            .Include(c => c.InspectorAssignments).ThenInclude(a => a.Inspector).ThenInclude(u => u.Profile)
            .Include(c => c.Entry).ThenInclude(e => e.User).ThenInclude(u => u.Profile)
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.Agency)
            .Include(c => c.Entry).ThenInclude(e => e.StatusHistory)
            .Include(c => c.Entry).ThenInclude(e => e.TimelineEvents)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        var activeAssignment = ContainerInspectionQueueHelper.GetActiveAssignment(container);
        var isAssignedToMe = ContainerInspectionQueueHelper.IsAssignedTo(container, user.Id);
        var assignedInspectorName = ContainerInspectionQueueHelper.ResolveInspectorName(activeAssignment);

        var certificate = await _db.Certificates
            .Where(c => c.EntryId == container.EntryId && c.Status == CertificateStatus.Active)
            .OrderByDescending(c => c.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var isComplete = IsContainerUploadComplete(container);
        var isApproved = IsContainerApproved(container);
        var submittedAt = ResolveContainerSubmittedAt(container);
        var photos = container.InspectionPhotos
            .OrderBy(p => p.PhotoType)
            .Select(p => MapPhoto(p, container.Uuid))
            .ToList();

        var (outcome, outcomeComment, completedAt) = ResolveContainerInspectionOutcome(container);

        return new AgencyContainerInspectionDetailDto(
            container.Uuid,
            container.ContainerNumber,
            container.ContainerType,
            container.Status.ToString(),
            container.FormDataJson,
            isComplete,
            isApproved,
            submittedAt,
            outcome,
            outcomeComment,
            completedAt,
            isAssignedToMe,
            assignedInspectorName,
            photos,
            MapEntryContext(container.Entry, certificate),
            BuildContainerInspectionHistory(container, submittedAt));
    }

    public async Task<AgencyContainerInspectionDetailDto> GetContainerDetailForAgencyTransportAsync(
        Guid containerUuid,
        CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.InspectionPhotos).ThenInclude(p => p.ReviewedBy).ThenInclude(u => u!.Profile)
            .Include(c => c.InspectorAssignments).ThenInclude(a => a.Inspector).ThenInclude(u => u.Profile)
            .Include(c => c.Entry).ThenInclude(e => e.User).ThenInclude(u => u.Profile)
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.Agency)
            .Include(c => c.Entry).ThenInclude(e => e.StatusHistory)
            .Include(c => c.Entry).ThenInclude(e => e.TimelineEvents)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (container.Status != ContainerStatus.ReadyForTransport && container.Status != ContainerStatus.AwaitingConfirmation)
        {
            throw new ClientPortalException("NOT_AVAILABLE", "Container is not available for transport tag review.");
        }

        var activeAssignment = ContainerInspectionQueueHelper.GetActiveAssignment(container);
        var isAssignedToMe = ContainerInspectionQueueHelper.IsAssignedTo(container, user.Id);
        var assignedInspectorName = ContainerInspectionQueueHelper.ResolveInspectorName(activeAssignment);

        var certificate = await _db.Certificates
            .Where(c => c.EntryId == container.EntryId && c.Status == CertificateStatus.Active)
            .OrderByDescending(c => c.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var isComplete = IsContainerUploadComplete(container);
        var isApproved = IsContainerApproved(container);
        var submittedAt = ResolveContainerSubmittedAt(container);
        var photos = container.InspectionPhotos
            .OrderBy(p => p.PhotoType)
            .Select(p => MapPhoto(p, container.Uuid))
            .ToList();

        var (outcome, outcomeComment, completedAt) = ResolveContainerInspectionOutcome(container);

        return new AgencyContainerInspectionDetailDto(
            container.Uuid,
            container.ContainerNumber,
            container.ContainerType,
            container.Status.ToString(),
            container.FormDataJson,
            isComplete,
            isApproved,
            submittedAt,
            outcome,
            outcomeComment,
            completedAt,
            isAssignedToMe,
            assignedInspectorName,
            photos,
            MapEntryContext(container.Entry, certificate),
            BuildContainerInspectionHistory(container, submittedAt));
    }

    public async Task<StoredFileDownload?> GetEntryCertificatePdfForAgencyAsync(
        Guid entryUuid,
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Uuid == entryUuid && e.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Entry not found.");

        var cert = await _db.Certificates
            .Where(c => c.EntryId == entry.Id && c.Status == CertificateStatus.Active)
            .OrderByDescending(c => c.IssuedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (cert is null || string.IsNullOrWhiteSpace(cert.PdfStoredFileName))
        {
            return null;
        }

        var physicalPath = _fileStorage.GetPhysicalPath($"certificates/{cert.Uuid}", cert.PdfStoredFileName);
        if (!File.Exists(physicalPath))
        {
            return null;
        }

        return new StoredFileDownload(
            physicalPath,
            "application/pdf",
            $"certificate-{cert.CertificateNumber}.pdf");
    }

    public async Task<ClientContainerInspectionPhotoDto> UploadPhotoAsync(
        Guid containerUuid,
        ContainerInspectionPhotoType photoType,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.Entry.UserId == user.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (!EntryStatusRules.CanUploadInspectionPhotos(container.Entry.Status))
        {
            throw new ClientPortalException("ENTRY_NOT_FOR_INSPECTION", "Inspection photos can only be uploaded when the entry is for inspection.");
        }

        var existing = await _db.ContainerInspectionPhotos
            .FirstOrDefaultAsync(p => p.ContainerId == container.Id && p.PhotoType == photoType, cancellationToken);

        var (storedFileName, _) = await _fileStorage.SaveAsync(
            fileStream, $"container-inspection/{container.Uuid}", fileName, cancellationToken);

        if (existing is null)
        {
            existing = new ContainerInspectionPhoto
            {
                Uuid = Guid.NewGuid(),
                ContainerId = container.Id,
                EntryId = container.EntryId,
                PhotoType = photoType,
                OriginalFileName = fileName,
                StoredFileName = storedFileName,
                ContentType = contentType
            };
            _db.ContainerInspectionPhotos.Add(existing);
        }
        else
        {
            existing.OriginalFileName = fileName;
            existing.StoredFileName = storedFileName;
            existing.ContentType = contentType;
            existing.ReviewDecision = EvaluationDecision.Pending;
            existing.ReviewComment = null;
            existing.ReviewedAt = null;
            existing.ReviewedByUserId = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return MapPhoto(existing, container.Uuid);
    }

    public async Task<ClientContainerInspectionPhotoDto> ReviewPhotoAsync(
        Guid photoUuid,
        ReviewContainerInspectionPhotoRequest request,
        CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<EvaluationDecision>(request.Decision, true, out var decision) ||
            decision is EvaluationDecision.Pending)
        {
            throw new ClientPortalException("INVALID_DECISION", "Decision must be Approved or Rejected.");
        }

        var photo = await _db.ContainerInspectionPhotos
            .Include(p => p.Container).ThenInclude(c => c.Entry)
            .Include(p => p.Container).ThenInclude(c => c.InspectorAssignments)
            .FirstOrDefaultAsync(p => p.Uuid == photoUuid && p.Container.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Photo not found.");

        await RequireInspectorAssignmentAsync(photo.Container, user.Id, cancellationToken);

        photo.ReviewDecision = decision;
        photo.ReviewComment = request.Comment?.Trim();
        photo.ReviewedByUserId = user.Id;
        photo.ReviewedAt = DateTime.UtcNow;
        if (decision == EvaluationDecision.Rejected)
        {
            photo.Container.Status = ContainerStatus.Pending;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return MapPhoto(photo, photo.Container.Uuid);
    }

    public async Task<AgencyContainerInspectionDetailDto> CompleteContainerInspectionAsync(
        Guid containerUuid,
        CompleteContainerInspectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        if (!Enum.TryParse<EvaluationDecision>(request.Decision, true, out var decision) ||
            decision is EvaluationDecision.Pending)
        {
            throw new ClientPortalException("INVALID_DECISION", "Decision must be Approved, Rejected, or RevisionRequired.");
        }

        var container = await _db.Containers
            .Include(c => c.InspectionPhotos)
            .Include(c => c.InspectorAssignments)
            .Include(c => c.Entry).ThenInclude(e => e.User).ThenInclude(u => u.Profile)
            .Include(c => c.Entry).ThenInclude(e => e.StatusHistory)
            .Include(c => c.Entry).ThenInclude(e => e.TimelineEvents)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        await RequireInspectorAssignmentAsync(container, user.Id, cancellationToken);

        if (container.Entry.Status != EntryStatus.ForInspection)
        {
            throw new ClientPortalException("ENTRY_NOT_FOR_INSPECTION", "Container inspection can only be completed while the entry is for inspection.");
        }

        if (!IsContainerUploadComplete(container))
        {
            throw new ClientPortalException("PHOTOS_INCOMPLETE", "All required container photos must be uploaded before submitting the inspection outcome.");
        }

        if (container.InspectionPhotos.Any(photo => photo.ReviewDecision == EvaluationDecision.Pending))
        {
            throw new ClientPortalException("PHOTOS_PENDING_REVIEW", "Review every container photo before submitting the inspection outcome.");
        }

        var allPhotosApproved = IsContainerApproved(container);
        if (decision == EvaluationDecision.Approved && !allPhotosApproved)
        {
            throw new ClientPortalException("CANNOT_APPROVE", "All container photos must be approved before approving this container.");
        }

        if (decision == EvaluationDecision.RevisionRequired && allPhotosApproved)
        {
            throw new ClientPortalException("INVALID_DECISION", "Revision required is only available when one or more photos were rejected.");
        }

        if (decision == EvaluationDecision.Rejected && !allPhotosApproved)
        {
            throw new ClientPortalException("INVALID_DECISION", "Reject the container only after all photos have been reviewed and approved.");
        }

        var entry = container.Entry;
        var comment = request.Comment?.Trim();
        var now = DateTime.UtcNow;

        if (decision == EvaluationDecision.Approved)
        {
            container.Status = ContainerStatus.ReadyForTransport;
            container.UpdatedAt = now;
            entry.TimelineEvents.Add(new TimelineEvent
            {
                EventType = "container_inspection_approved",
                Title = $"Container {container.ContainerNumber} approved",
                Description = comment ?? "All container inspection photos were approved.",
                ActorUserId = user.Id,
                CreatedAt = now,
            });
            await _db.SaveChangesAsync(cancellationToken);
            await _workflow.TryMarkContainerReadyForTransportAsync(container.Id, cancellationToken);

            await _notifications.NotifyAsync(
                entry.UserId,
                "container_inspection",
                "Container inspection approved",
                $"Container {container.ContainerNumber} for entry {entry.ReferenceNo} was approved and is ready for transport.",
                "Entry",
                entry.Uuid.ToString(),
                cancellationToken);
        }
        else
        {
            container.Status = ContainerStatus.Pending;
            container.UpdatedAt = now;
            var title = decision == EvaluationDecision.RevisionRequired
                ? $"Container {container.ContainerNumber} needs photo revision"
                : $"Container {container.ContainerNumber} rejected";
            var description = comment ?? (decision == EvaluationDecision.RevisionRequired
                ? "One or more container photos were rejected. Please re-upload the required photos."
                : "The container inspection was rejected.");

            entry.TimelineEvents.Add(new TimelineEvent
            {
                EventType = decision == EvaluationDecision.RevisionRequired
                    ? "container_inspection_revision_required"
                    : "container_inspection_rejected",
                Title = title,
                Description = description,
                ActorUserId = user.Id,
                CreatedAt = now,
            });
            await _db.SaveChangesAsync(cancellationToken);

            await _notifications.NotifyAsync(
                entry.UserId,
                "container_inspection",
                title,
                description,
                "Entry",
                entry.Uuid.ToString(),
                cancellationToken);
        }

        CompleteActiveInspectorAssignment(container);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetContainerInspectionDetailAsync(containerUuid, cancellationToken);
    }

    public async Task<StoredFileDownload> DownloadPhotoForAgencyAsync(Guid photoUuid, CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var photo = await _db.ContainerInspectionPhotos
            .Include(p => p.Container).ThenInclude(c => c.Entry)
            .FirstOrDefaultAsync(p => p.Uuid == photoUuid && p.Container.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Photo not found.");

        return BuildPhotoDownload(photo);
    }

    public async Task<StoredFileDownload> DownloadPhotoForClientAsync(
        Guid entryUuid,
        Guid photoUuid,
        CancellationToken cancellationToken = default)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        var photo = await _db.ContainerInspectionPhotos
            .Include(p => p.Container).ThenInclude(c => c.Entry)
            .FirstOrDefaultAsync(
                p => p.Uuid == photoUuid &&
                     p.Container.Entry.Uuid == entryUuid &&
                     p.Container.Entry.UserId == user.Id,
                cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Photo not found.");

        return BuildPhotoDownload(photo);
    }

    private StoredFileDownload BuildPhotoDownload(ContainerInspectionPhoto photo)
    {
        var folder = $"container-inspection/{photo.Container.Uuid}";
        return new StoredFileDownload(
            _fileStorage.GetPhysicalPath(folder, photo.StoredFileName),
            photo.ContentType,
            photo.OriginalFileName);
    }

    private static bool IsContainerUploadComplete(Container container) =>
        EntryWorkflowService.RequiredPhotoTypes.All(type =>
            container.InspectionPhotos.Any(photo => photo.PhotoType == type));

    private static bool IsContainerApproved(Container container) =>
        IsContainerUploadComplete(container) &&
        EntryWorkflowService.RequiredPhotoTypes.All(type =>
            container.InspectionPhotos.Any(photo =>
                photo.PhotoType == type && photo.ReviewDecision == EvaluationDecision.Approved));

    private static bool NeedsInspectorOutcomeSubmission(Container container)
    {
        if (!IsContainerUploadComplete(container))
        {
            return false;
        }

        if (container.InspectionPhotos.Any(photo => photo.ReviewDecision == EvaluationDecision.Pending))
        {
            return false;
        }

        if (container.Status == ContainerStatus.ReadyForTransport)
        {
            return false;
        }

        var (outcome, _, _) = ResolveContainerInspectionOutcome(container);
        return outcome is null;
    }

    private static (string? Outcome, string? Comment, DateTime? CompletedAt) ResolveContainerInspectionOutcome(Container container)
    {
        var latest = container.Entry.TimelineEvents
            .Where(eventItem => eventItem.EventType is "container_inspection_approved"
                or "container_inspection_revision_required"
                or "container_inspection_rejected")
            .Where(eventItem => eventItem.Title.Contains(container.ContainerNumber, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(eventItem => eventItem.CreatedAt)
            .FirstOrDefault();

        if (latest is null)
        {
            if (container.Status == ContainerStatus.ReadyForTransport && IsContainerApproved(container))
            {
                return ("Approved", null, container.UpdatedAt);
            }

            return (null, null, null);
        }

        var outcome = latest.EventType switch
        {
            "container_inspection_approved" => "Approved",
            "container_inspection_revision_required" => "RevisionRequired",
            "container_inspection_rejected" => "Rejected",
            _ => null,
        };

        return (outcome, latest.Description, latest.CreatedAt);
    }

    private static DateTime? ResolveContainerSubmittedAt(Container container)
    {
        if (!IsContainerUploadComplete(container))
        {
            return null;
        }

        return container.InspectionPhotos.Max(photo => photo.CreatedAt);
    }

    private Task RequireInspectorAssignmentAsync(Container container, long userId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsInRole("ROLE_ADMIN"))
        {
            return Task.CompletedTask;
        }

        if (!ContainerInspectionQueueHelper.IsAssignedTo(container, userId))
        {
            if (ContainerInspectionQueueHelper.HasActiveAssignment(container))
            {
                throw new ClientPortalException("NOT_ASSIGNED", "This container is assigned to another inspector.");
            }

            throw new ClientPortalException("NOT_ASSIGNED", "Claim this container before reviewing it.");
        }

        return Task.CompletedTask;
    }

    private static void CompleteActiveInspectorAssignment(Container container)
    {
        var assignment = ContainerInspectionQueueHelper.GetActiveAssignment(container);
        if (assignment is null)
        {
            return;
        }

        assignment.Status = AssignmentStatus.Completed;
        assignment.CompletedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;
    }

    private static AgencyContainerInspectionQueueItemDto MapQueueItem(
        Container container,
        bool isComplete,
        bool isApproved,
        int pendingPhotoCount,
        long currentUserId)
    {
        var entry = container.Entry;
        var applicant = entry.User.Profile is not null
            ? $"{entry.User.Profile.FirstName} {entry.User.Profile.LastName}".Trim()
            : entry.User.Email;
        var activeAssignment = ContainerInspectionQueueHelper.GetActiveAssignment(container);

        return new AgencyContainerInspectionQueueItemDto(
            container.Uuid,
            container.ContainerNumber,
            entry.Uuid,
            entry.ReferenceNo,
            applicant,
            entry.EntryType.ToString(),
            pendingPhotoCount,
            isComplete,
            isApproved,
            ContainerInspectionQueueHelper.ResolveSubmittedAt(container),
            ContainerInspectionQueueHelper.IsAssignedTo(container, currentUserId),
            ContainerInspectionQueueHelper.ResolveInspectorName(activeAssignment));
    }

    private static AgencyContainerInspectionEntryContextDto MapEntryContext(Entry entry, Certificate? certificate)
    {
        var applicant = entry.User.Profile is not null
            ? $"{entry.User.Profile.FirstName} {entry.User.Profile.LastName}".Trim()
            : entry.User.Email;

        return new AgencyContainerInspectionEntryContextDto(
            entry.Uuid,
            entry.ReferenceNo,
            entry.EntryType.ToString(),
            entry.Status.ToString(),
            applicant,
            AgencyEntryPresentationHelper.ResolveCompanyName(entry),
            entry.Detail?.CommodityName,
            entry.Detail?.Description,
            entry.Detail?.Quantity,
            entry.Detail?.Unit,
            entry.Detail?.OriginCountry,
            entry.Detail?.DestinationCountry,
            entry.Detail?.PortOfEntry,
            entry.Agency?.Code ?? string.Empty,
            certificate?.Uuid,
            certificate?.CertificateNumber,
            certificate?.Title);
    }

    private static ClientContainerInspectionStatusDto MapStatus(Container container)
    {
        var photos = container.InspectionPhotos
            .OrderBy(p => p.PhotoType)
            .Select(p => MapPhoto(p, container.Uuid))
            .ToList();
        var complete = IsContainerUploadComplete(container);
        var approved = IsContainerApproved(container);
        return new ClientContainerInspectionStatusDto(container.Uuid, container.ContainerNumber, photos, complete, approved);
    }

    private static string ResolveReviewerName(ContainerInspectionPhoto photo)
    {
        if (photo.ReviewedBy is null)
        {
            return string.Empty;
        }

        if (photo.ReviewedBy.Profile is not null)
        {
            return $"{photo.ReviewedBy.Profile.FirstName} {photo.ReviewedBy.Profile.LastName}".Trim();
        }

        return photo.ReviewedBy.Email;
    }

    private static string ResolvePhotoTypeLabel(ContainerInspectionPhotoType photoType) => photoType switch
    {
        ContainerInspectionPhotoType.ActualItem => "Actual item photo",
        ContainerInspectionPhotoType.LabelImage => "Label image",
        ContainerInspectionPhotoType.XrayImage => "X-ray image",
        ContainerInspectionPhotoType.ExaminationImage => "Examination image",
        ContainerInspectionPhotoType.ReportImage => "Report image",
        ContainerInspectionPhotoType.RequestForInspection => "Request for inspection",
        _ => photoType.ToString(),
    };

    private static IReadOnlyList<AgencyContainerInspectionHistoryItemDto> BuildContainerInspectionHistory(
        Container container,
        DateTime? submittedAt)
    {
        var history = new List<AgencyContainerInspectionHistoryItemDto>();

        foreach (var item in container.Entry.StatusHistory.OrderBy(h => h.CreatedAt))
        {
            history.Add(new AgencyContainerInspectionHistoryItemDto(
                item.ToStatus.ToString(),
                item.Comment,
                item.CreatedAt,
                null));
        }

        foreach (var photo in container.InspectionPhotos.OrderBy(p => p.CreatedAt))
        {
            var label = ResolvePhotoTypeLabel(photo.PhotoType);
            history.Add(new AgencyContainerInspectionHistoryItemDto(
                "PhotoUploaded",
                $"{label} uploaded",
                photo.CreatedAt,
                null));

            if (photo.ReviewDecision != EvaluationDecision.Pending && photo.ReviewedAt.HasValue)
            {
                history.Add(new AgencyContainerInspectionHistoryItemDto(
                    photo.ReviewDecision.ToString(),
                    photo.ReviewComment ?? $"{label} {photo.ReviewDecision.ToString().ToLowerInvariant()}",
                    photo.ReviewedAt.Value,
                    ResolveReviewerName(photo)));
            }
        }

        if (submittedAt.HasValue)
        {
            history.Add(new AgencyContainerInspectionHistoryItemDto(
                "Submitted",
                $"Container {container.ContainerNumber} photos submitted for review",
                submittedAt.Value,
                container.Entry.User.Profile is not null
                    ? $"{container.Entry.User.Profile.FirstName} {container.Entry.User.Profile.LastName}".Trim()
                    : container.Entry.User.Email));
        }

        var (outcome, outcomeComment, completedAt) = ResolveContainerInspectionOutcome(container);
        if (outcome is not null && completedAt.HasValue)
        {
            history.Add(new AgencyContainerInspectionHistoryItemDto(
                outcome,
                outcomeComment ?? $"Container inspection outcome: {outcome}",
                completedAt.Value,
                null));
        }

        return history
            .OrderBy(item => item.CreatedAt)
            .ToList();
    }

    private static ClientContainerInspectionPhotoDto MapPhoto(ContainerInspectionPhoto photo, Guid containerUuid)
    {
        var reviewerName = ResolveReviewerName(photo);
        return new ClientContainerInspectionPhotoDto(
            photo.Uuid,
            containerUuid,
            photo.PhotoType.ToString(),
            photo.OriginalFileName,
            photo.ReviewDecision.ToString(),
            photo.ReviewComment,
            photo.CreatedAt,
            photo.ReviewedAt,
            string.IsNullOrWhiteSpace(reviewerName) ? null : reviewerName);
    }
}

public interface ITransportTagService
{
    Task<TransportTagQueuesDto> ListTransportTagQueuesAsync(CancellationToken cancellationToken = default);
    Task<TransportTagContainerDetailDto> GetTransportTagContainerDetailAsync(Guid containerUuid, CancellationToken cancellationToken = default);
    Task<AddTransportTagResultDto> AddTransportTagAsync(AddTransportTagRequest request, CancellationToken cancellationToken = default);
    Task<AddTransportTagResultDto> GetTransportTagAsync(Guid tagUuid, CancellationToken cancellationToken = default);
}

public class TransportTagService : ITransportTagService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEntryWorkflowService _workflow;
    private readonly IConfiguration _configuration;
    private readonly IContainerInspectionWorkflowService _inspectionService;

    public TransportTagService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IEntryWorkflowService workflow,
        IConfiguration configuration,
        IContainerInspectionWorkflowService inspectionService)
    {
        _db = db;
        _currentUser = currentUser;
        _workflow = workflow;
        _configuration = configuration;
        _inspectionService = inspectionService;
    }

    public async Task<TransportTagQueuesDto> ListTransportTagQueuesAsync(
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var readyContainers = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.TransportTags)
            .Where(c => c.Entry.AgencyId == agency.Id && c.Status == ContainerStatus.ReadyForTransport)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

        var taggedContainers = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.TransportTags)
            .Where(c => c.Entry.AgencyId == agency.Id && c.Status == ContainerStatus.AwaitingConfirmation)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

        var ready = readyContainers.Select(container => new TransportTagQueueItemDto(
            container.Uuid,
            container.ContainerNumber,
            container.Entry.Uuid,
            container.Entry.ReferenceNo,
            container.Status.ToString(),
            container.TransportTags.Count > 0,
            container.UpdatedAt)).ToList();

        var tagged = taggedContainers
            .Select(container =>
            {
                var tag = container.TransportTags.OrderByDescending(t => t.TaggedAt).FirstOrDefault();
                if (tag is null)
                {
                    return null;
                }

                return new TaggedTransportQueueItemDto(
                    container.Uuid,
                    container.ContainerNumber,
                    container.Entry.Uuid,
                    container.Entry.ReferenceNo,
                    container.Status.ToString(),
                    tag.Uuid,
                    tag.ScheduledWarehouseDate.HasValue
                        ? DateOnly.FromDateTime(tag.ScheduledWarehouseDate.Value)
                        : null,
                    tag.TaggedAt,
                    container.UpdatedAt);
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderByDescending(item => item.TaggedAt)
            .ToList();

        return new TransportTagQueuesDto(ready, tagged);
    }

    public async Task<TransportTagContainerDetailDto> GetTransportTagContainerDetailAsync(
        Guid containerUuid,
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.TransportTags)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid && c.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        var containerDetail = await _inspectionService.GetContainerDetailForAgencyTransportAsync(containerUuid, cancellationToken);
        var tag = container.TransportTags.OrderByDescending(t => t.TaggedAt).FirstOrDefault();
        TransportTagSummaryDto? tagSummary = tag is null
            ? null
            : new TransportTagSummaryDto(
                tag.Uuid,
                tag.TransportType,
                tag.ScheduledWarehouseDate.HasValue
                    ? DateOnly.FromDateTime(tag.ScheduledWarehouseDate.Value)
                    : null,
                tag.TaggedAt,
                tag.QrCodeData);

        return new TransportTagContainerDetailDto(containerDetail, tagSummary, container.UpdatedAt);
    }

    public async Task<AddTransportTagResultDto> AddTransportTagAsync(
        AddTransportTagRequest request,
        CancellationToken cancellationToken = default)
    {
        var (user, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == request.ContainerUuid && c.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (container.Status != ContainerStatus.ReadyForTransport)
        {
            throw new ClientPortalException("NOT_READY", "Container must be ready for transport before tagging.");
        }

        if (await _db.ContainerTransportTags.AnyAsync(t => t.ContainerId == container.Id, cancellationToken))
        {
            throw new ClientPortalException("ALREADY_TAGGED", "Container is already tagged for transport.");
        }

        if (request.ScheduledWarehouseDate == default)
        {
            throw new ClientPortalException("SCHEDULE_REQUIRED", "Scheduled warehouse inspection date is required.");
        }

        var tag = new ContainerTransportTag
        {
            Uuid = Guid.NewGuid(),
            ContainerId = container.Id,
            EntryId = container.EntryId,
            TransportType = "warehouse_nmis",
            ScheduledWarehouseDate = request.ScheduledWarehouseDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            TaggedByUserId = user.Id,
            TaggedAt = DateTime.UtcNow,
        };

        var qrPayload = BuildQrPayload(tag, container, container.Entry);
        tag.QrPayload = qrPayload;
        tag.QrCodeData = QrCodeGenerator.ToBase64Png(qrPayload);

        _db.ContainerTransportTags.Add(tag);
        container.Status = ContainerStatus.AwaitingConfirmation;
        await _db.SaveChangesAsync(cancellationToken);
        await _workflow.SyncEntryTransportStatusAsync(container.EntryId, user.Id, cancellationToken);

        return MapTransportTagResult(tag, container, container.Entry);
    }

    public async Task<AddTransportTagResultDto> GetTransportTagAsync(
        Guid tagUuid,
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);
        var tag = await _db.ContainerTransportTags
            .Include(t => t.Container).ThenInclude(c => c.Entry)
            .FirstOrDefaultAsync(t => t.Uuid == tagUuid && t.Container.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Transport tag not found.");

        return MapTransportTagResult(tag, tag.Container, tag.Container.Entry);
    }

    private string BuildQrPayload(ContainerTransportTag tag, Container container, Entry entry)
    {
        var signingKey = TransportQrSigningKeyResolver.Resolve(_configuration);
        return TransportQrCodec.Encode(
            new TransportQrPayload(
                tag.Uuid,
                container.Uuid,
                container.ContainerNumber,
                entry.ReferenceNo,
                tag.TransportType,
                tag.ScheduledWarehouseDate.HasValue
                    ? DateOnly.FromDateTime(tag.ScheduledWarehouseDate.Value)
                    : null,
                tag.TaggedAt),
            signingKey);
    }

    private static AddTransportTagResultDto MapTransportTagResult(
        ContainerTransportTag tag,
        Container container,
        Entry entry) =>
        new(
            tag.Uuid,
            container.Uuid,
            container.ContainerNumber,
            entry.ReferenceNo,
            tag.TransportType,
            tag.ScheduledWarehouseDate.HasValue
                ? DateOnly.FromDateTime(tag.ScheduledWarehouseDate.Value)
                : null,
            container.Status.ToString(),
            tag.QrPayload ?? string.Empty,
            tag.QrCodeData,
            tag.TaggedAt);
}

public class OperatorOpsService : IOperatorOpsService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAgriTrackPushService _push;
    private readonly IConfiguration _configuration;

    public OperatorOpsService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IAgriTrackPushService push,
        IConfiguration configuration)
    {
        _db = db;
        _currentUser = currentUser;
        _push = push;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<ContainerListItemDto>> ListClaimableContainersAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        await RequireOperatorAsync(cancellationToken);
        var query = _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .Where(c => c.Status == ContainerStatus.AwaitingConfirmation && c.ClaimedByUserId == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.ContainerNumber.Contains(term) ||
                c.Entry.ReferenceNo.Contains(term));
        }

        var containers = await query
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
        return containers.Select(OpsDtoMapper.MapContainer).ToList();
    }

    public async Task<IReadOnlyList<ContainerListItemDto>> ListClaimedContainersAsync(CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        var containers = await OperatorContainerQuery(_db.Containers)
            .Where(c => c.ClaimedByUserId == user.Id)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
        return containers.Select(c => OpsDtoMapper.MapContainer(c, includeAssignment: true)).ToList();
    }

    public async Task<ContainerListItemDto> ClaimContainerAsync(Guid containerUuid, CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        return await ClaimContainerInternalAsync(user, containerUuid, cancellationToken);
    }

    public async Task<ContainerListItemDto> ProvisionRouteTestContainerAsync(CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("AgriTrack:AllowOperatorRouteTestProvisioning", true))
        {
            throw new ClientPortalException("FORBIDDEN", "Route test provisioning is disabled on this server.");
        }

        await RequireOperatorAsync(cancellationToken);

        var templateEntry = await _db.Entries
            .AsNoTracking()
            .OrderByDescending(e => e.SubmittedAt ?? e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ClientPortalException("ROUTE_TEST_UNAVAILABLE", "No import entry found to create a test container.");

        var now = DateTime.UtcNow;
        var prefixes = new[] { "AXEU", "TCLU", "MSKU", "HLXU", "CGMU", "FCIU" };
        string containerNumber;
        do
        {
            var prefix = prefixes[Random.Shared.Next(prefixes.Length)];
            var suffix = Random.Shared.Next(1_000_000, 9_999_999);
            containerNumber = $"{prefix}{suffix}";
        }
        while (await _db.Containers.AnyAsync(c => c.ContainerNumber == containerNumber, cancellationToken));

        var referenceSuffix = Random.Shared.Next(1000, 9999);
        var entry = new Entry
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = $"RT-{now:yyyyMMddHHmmss}-{referenceSuffix}",
            UserId = templateEntry.UserId,
            AgencyId = templateEntry.AgencyId,
            EntryType = templateEntry.EntryType,
            Status = EntryStatus.AwaitingTransport,
            SubmittedAt = now,
            PaymentStatus = PaymentStatus.Paid,
            PaymentAmount = 0m,
            FormDataJson = """{"source":"operator-route-test","note":"Auto provisioned for AgriTrack map test"}""",
        };
        _db.Entries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        var container = new Container
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            SequenceNumber = 1,
            ContainerNumber = containerNumber,
            ContainerType = "40FT",
            Status = ContainerStatus.AwaitingConfirmation,
        };
        _db.Containers.Add(container);
        await _db.SaveChangesAsync(cancellationToken);

        container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstAsync(c => c.Id == container.Id, cancellationToken);

        return OpsDtoMapper.MapContainer(container);
    }

    public async Task<ContainerListItemDto> ClaimContainerByQrAsync(
        ScanTransportQrRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.QrData))
        {
            throw new ClientPortalException("QR_REQUIRED", "QR data is required.");
        }

        TransportQrPayload payload;
        try
        {
            payload = TransportQrCodec.Verify(request.QrData.Trim(), TransportQrSigningKeyResolver.Resolve(_configuration));
        }
        catch (InvalidOperationException ex)
        {
            throw new ClientPortalException("INVALID_QR", ex.Message);
        }

        var tag = await _db.ContainerTransportTags
            .FirstOrDefaultAsync(t => t.Uuid == payload.TagUuid, cancellationToken)
            ?? throw new ClientPortalException("INVALID_QR", "Transport tag was not found.");

        if (!string.Equals(tag.QrPayload, request.QrData.Trim(), StringComparison.Ordinal))
        {
            throw new ClientPortalException("INVALID_QR", "Transport QR payload does not match the issued tag.");
        }

        return await ClaimContainerInternalAsync(user, payload.ContainerUuid, cancellationToken);
    }

    private async Task<ContainerListItemDto> ClaimContainerInternalAsync(
        User user,
        Guid containerUuid,
        CancellationToken cancellationToken)
    {
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (container.Status != ContainerStatus.AwaitingConfirmation)
        {
            throw new ClientPortalException("INVALID_STATUS", "Container is not awaiting confirmation.");
        }

        if (container.ClaimedByUserId is not null && container.ClaimedByUserId != user.Id)
        {
            throw new ClientPortalException("ALREADY_CLAIMED", "Container was already claimed by another operator.");
        }

        container.ClaimedByUserId = user.Id;
        await _db.SaveChangesAsync(cancellationToken);
        return OpsDtoMapper.MapContainer(container, includeAssignment: true);
    }

    public async Task<ContainerListItemDto> AssignDriverAsync(
        Guid containerUuid,
        AssignDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        var driverProfile = await _db.DriverProfiles
            .Include(p => p.User).ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(p => p.User.Uuid == request.DriverUserUuid, cancellationToken)
            ?? throw new ClientPortalException("DRIVER_NOT_FOUND", "Driver not found.");

        if (driverProfile.OperatorUserId != user.Id && !_currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("DRIVER_NOT_IN_FLEET", "Driver is not registered under your fleet.");
        }

        var container = await OperatorContainerQuery(_db.Containers)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (container.ClaimedByUserId != user.Id && !_currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "Only the claiming operator can assign a driver.");
        }

        OperatorVehicle? vehicle = null;
        if (request.VehicleUuid.HasValue)
        {
            vehicle = await _db.OperatorVehicles
                .FirstOrDefaultAsync(
                    v => v.Uuid == request.VehicleUuid.Value &&
                         v.OperatorUserId == user.Id &&
                         v.IsActive,
                    cancellationToken)
                ?? throw new ClientPortalException("VEHICLE_NOT_FOUND", "Vehicle not found in your fleet.");
        }

        container.AssignedDriverUserId = driverProfile.UserId;
        container.AssignedOperatorVehicleId = vehicle?.Id;
        container.Status = ContainerStatus.Assigned;
        await _db.SaveChangesAsync(cancellationToken);
        await _push.NotifyDriverAssignmentAsync(driverProfile.UserId, container.ContainerNumber, container.Uuid, cancellationToken);

        container = await OperatorContainerQuery(_db.Containers)
            .FirstAsync(c => c.Id == container.Id, cancellationToken);
        return OpsDtoMapper.MapContainer(container, includeAssignment: true);
    }

    public async Task<OperatorDeliveryStopsDto> GetDeliveryStopsAsync(
        Guid containerUuid,
        CancellationToken cancellationToken = default)
    {
        var container = await RequireOperatorContainerAsync(containerUuid, cancellationToken);
        return await MapDeliveryStopsAsync(container, cancellationToken);
    }

    public async Task<OperatorDeliveryStopsDto> BookDeliveryStopsAsync(
        Guid containerUuid,
        BookOperatorDeliveryStopsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("AgriTrack:AllowOperatorDeliveryStopBooking", true))
        {
            throw new ClientPortalException("FORBIDDEN", "Operator route booking is disabled on this server.");
        }

        var user = await RequireOperatorAsync(cancellationToken);
        var container = await RequireOperatorContainerAsync(containerUuid, cancellationToken);
        if (container.ClaimedByUserId is null)
        {
            if (container.Status != ContainerStatus.AwaitingConfirmation)
            {
                throw new ClientPortalException("INVALID_STATUS", "Container is not available for route booking.");
            }

            container.ClaimedByUserId = user.Id;
        }
        else if (container.ClaimedByUserId != user.Id && !_currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "Only the claiming operator can book delivery stops.");
        }

        if (!AllowsOperatorStopBooking(container.Status))
        {
            throw new ClientPortalException("INVALID_STATUS", "Delivery stops cannot be changed for this container status.");
        }

        var pickupLabel = request.PickupLabel?.Trim();
        var destinationLabel = request.DestinationLabel?.Trim();
        if (string.IsNullOrWhiteSpace(pickupLabel) || string.IsNullOrWhiteSpace(destinationLabel))
        {
            throw new ClientPortalException("VALIDATION", "Pickup and warehouse labels are required.");
        }

        ValidateCoordinates(request.PickupLatitude, request.PickupLongitude, "Pickup");
        ValidateCoordinates(request.DestinationLatitude, request.DestinationLongitude, "Warehouse");

        ContainerDeliveryPointHelper.SetPickup(container, pickupLabel, request.PickupLatitude, request.PickupLongitude);
        ContainerDeliveryPointHelper.SetDestination(
            container,
            destinationLabel,
            request.DestinationLatitude,
            request.DestinationLongitude);

        await _db.SaveChangesAsync(cancellationToken);
        return await MapDeliveryStopsAsync(container, cancellationToken);
    }

    public async Task<IReadOnlyList<OperatorDriverListItemDto>> ListDriversAsync(CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        var profiles = await _db.DriverProfiles
            .Where(p => p.OperatorUserId == user.Id)
            .Include(p => p.User).ThenInclude(u => u.Profile)
            .OrderBy(p => p.User.Email)
            .ToListAsync(cancellationToken);

        return profiles.Select(MapOperatorDriver).ToList();
    }

    public async Task<IReadOnlyList<OperatorVehicleListItemDto>> ListVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        var vehicles = await _db.OperatorVehicles
            .Where(v => v.OperatorUserId == user.Id)
            .Include(v => v.DefaultDriver).ThenInclude(d => d!.Profile)
            .OrderBy(v => v.PlateNumber)
            .ToListAsync(cancellationToken);

        return vehicles.Select(MapOperatorVehicle).ToList();
    }

    public async Task<OperatorVehicleListItemDto> CreateVehicleAsync(
        CreateOperatorVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.PlateNumber) || string.IsNullOrWhiteSpace(request.VehicleType))
        {
            throw new ClientPortalException("VALIDATION", "Plate number and vehicle type are required.");
        }

        long? defaultDriverUserId = null;
        if (request.DefaultDriverUuid.HasValue)
        {
            var driverProfile = await _db.DriverProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.Uuid == request.DefaultDriverUuid.Value, cancellationToken)
                ?? throw new ClientPortalException("DRIVER_NOT_FOUND", "Default driver not found.");

            if (driverProfile.OperatorUserId != user.Id)
            {
                throw new ClientPortalException("DRIVER_NOT_IN_FLEET", "Default driver is not registered under your fleet.");
            }

            defaultDriverUserId = driverProfile.UserId;
        }

        var plate = request.PlateNumber.Trim().ToUpperInvariant();
        if (await _db.OperatorVehicles.AnyAsync(
                v => v.OperatorUserId == user.Id && v.PlateNumber == plate,
                cancellationToken))
        {
            throw new ClientPortalException("DUPLICATE_PLATE", "A vehicle with this plate number already exists.");
        }

        var vehicle = new OperatorVehicle
        {
            OperatorUserId = user.Id,
            PlateNumber = plate,
            VehicleType = request.VehicleType.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DefaultDriverUserId = defaultDriverUserId,
            IsActive = true,
        };
        _db.OperatorVehicles.Add(vehicle);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(vehicle).Reference(v => v.DefaultDriver).LoadAsync(cancellationToken);
        if (vehicle.DefaultDriver is not null)
        {
            await _db.Entry(vehicle.DefaultDriver).Reference(u => u.Profile).LoadAsync(cancellationToken);
        }

        return MapOperatorVehicle(vehicle);
    }

    public async Task<IReadOnlyList<OperatorInviteCodeListItemDto>> ListInviteCodesAsync(CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        await AgriCheck.Infrastructure.Persistence.Seeding.OperatorInviteCodeSeeder.EnsureSeedDataAsync(_db, cancellationToken: cancellationToken);

        var codes = await _db.OperatorInviteCodes
            .Where(i => i.OperatorUserId == user.Id)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        if (codes.Count == 0)
        {
            codes.Add(await CreateInviteCodeEntityAsync(user.Id, "Default fleet invite", cancellationToken));
        }

        return codes.Select(MapInviteCode).ToList();
    }

    public async Task<OperatorInviteCodeListItemDto> CreateInviteCodeAsync(
        CreateOperatorInviteCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        await AgriCheck.Infrastructure.Persistence.Seeding.OperatorInviteCodeSeeder.EnsureSeedDataAsync(_db, cancellationToken: cancellationToken);
        var entity = await CreateInviteCodeEntityAsync(user.Id, request.Label, cancellationToken);
        return MapInviteCode(entity);
    }

    private async Task<OperatorInviteCode> CreateInviteCodeEntityAsync(
        long operatorUserId,
        string? label,
        CancellationToken cancellationToken)
    {
        var code = await GenerateUniqueInviteCodeAsync(cancellationToken);
        var entity = new OperatorInviteCode
        {
            Code = code,
            OperatorUserId = operatorUserId,
            Label = string.IsNullOrWhiteSpace(label) ? "Driver registration invite" : label.Trim(),
            MaxUses = 0,
            IsActive = true,
        };
        _db.OperatorInviteCodes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private async Task<string> GenerateUniqueInviteCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var suffix = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..6];
            var candidate = $"AT-{suffix}".ToUpperInvariant();
            if (!await _db.OperatorInviteCodes.AnyAsync(i => i.Code == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new ClientPortalException("INVITE_CODE_FAILED", "Unable to generate a unique invite code.");
    }

    private static OperatorInviteCodeListItemDto MapInviteCode(OperatorInviteCode invite) =>
        new(invite.Code, invite.Label, invite.MaxUses, invite.UsedCount, invite.ExpiresAt, invite.IsActive, invite.CreatedAt);

    private async Task<Container> RequireOperatorContainerAsync(Guid containerUuid, CancellationToken cancellationToken)
    {
        await RequireOperatorAsync(cancellationToken);
        return await OperatorContainerQuery(_db.Containers)
            .Include(c => c.Entry).ThenInclude(e => e.Detail)
            .Include(c => c.Entry).ThenInclude(e => e.FinalWarehouseFacility)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");
    }

    private async Task<OperatorDeliveryStopsDto> MapDeliveryStopsAsync(
        Container container,
        CancellationToken cancellationToken)
    {
        var pickup = ContainerDeliveryPointHelper.ResolvePickup(container, container.Entry);
        var destination = await ResolveOperatorDestinationAsync(container, cancellationToken);
        return new OperatorDeliveryStopsDto(
            container.Uuid,
            container.ContainerNumber,
            new ContainerDeliveryStopDto(pickup.Label, pickup.Latitude, pickup.Longitude),
            new ContainerDeliveryStopDto(destination.Label, destination.Latitude, destination.Longitude));
    }

    private async Task<(string Label, decimal Latitude, decimal Longitude)> ResolveOperatorDestinationAsync(
        Container container,
        CancellationToken cancellationToken)
    {
        var stored = ContainerDeliveryPointHelper.TryGetStoredDestination(container);
        if (stored is not null)
        {
            return stored.Value;
        }

        var finalFacility = container.Entry.FinalWarehouseFacility;
        if (finalFacility?.Latitude is not null && finalFacility.Longitude is not null)
        {
            return (finalFacility.Name, finalFacility.Latitude.Value, finalFacility.Longitude.Value);
        }

        var booking = await _db.WarehouseBookings
            .Include(b => b.WarehouseFacility)
            .Where(b => b.UserId == container.Entry.UserId)
            .Where(b => b.ContainerReference == container.ContainerNumber || b.ContainerReference == string.Empty)
            .OrderByDescending(b => b.ScheduledDate)
            .FirstOrDefaultAsync(cancellationToken);

        var facility = booking?.WarehouseFacility;
        if (facility?.Latitude is null || facility.Longitude is null)
        {
            facility = await _db.WarehouseFacilities
                .Where(w => w.IsActive && w.Latitude != null && w.Longitude != null)
                .OrderBy(w => w.Code == "WH-MNL" ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (facility?.Latitude is null || facility.Longitude is null)
        {
            return ("Warehouse (tap map)", 14.5547m, 120.9872m);
        }

        return (facility.Name, facility.Latitude.Value, facility.Longitude.Value);
    }

    private static bool AllowsOperatorStopBooking(ContainerStatus status) =>
        status is not ContainerStatus.Inspected and not ContainerStatus.Released;

    private static void ValidateCoordinates(decimal latitude, decimal longitude, string label)
    {
        if (latitude is < -90m or > 90m || longitude is < -180m or > 180m)
        {
            throw new ClientPortalException("VALIDATION", $"{label} coordinates are out of range.");
        }
    }

    private static IQueryable<Container> OperatorContainerQuery(IQueryable<Container> query) =>
        query
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .Include(c => c.AssignedDriver).ThenInclude(d => d!.Profile)
            .Include(c => c.AssignedOperatorVehicle);

    private static OperatorDriverListItemDto MapOperatorDriver(DriverProfile profile)
    {
        var user = profile.User;
        var name = OpsDtoMapper.FormatUserName(user.Profile, user.Email);
        return new OperatorDriverListItemDto(
            user.Uuid,
            name,
            user.Email,
            profile.PhoneNumber,
            profile.VehicleType,
            profile.VehicleRegistration,
            profile.CompletionPercentage >= 80);
    }

    private static OperatorVehicleListItemDto MapOperatorVehicle(OperatorVehicle vehicle)
    {
        Guid? defaultDriverUuid = null;
        string? defaultDriverName = null;
        if (vehicle.DefaultDriver is not null)
        {
            defaultDriverUuid = vehicle.DefaultDriver.Uuid;
            defaultDriverName = OpsDtoMapper.FormatUserName(vehicle.DefaultDriver.Profile, vehicle.DefaultDriver.Email);
        }

        return new OperatorVehicleListItemDto(
            vehicle.Uuid,
            vehicle.PlateNumber,
            vehicle.VehicleType,
            vehicle.Description,
            defaultDriverUuid,
            defaultDriverName,
            vehicle.IsActive);
    }

    private async Task<User> RequireOperatorAsync(CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        if (!_currentUser.IsInRole("ROLE_OPERATOR") && !_currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "Operator access required.");
        }

        return user;
    }
}

public class DoctorInspectionService : IDoctorInspectionService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IAgriTrackPushService _push;

    public DoctorInspectionService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IConfiguration configuration,
        IAgriTrackPushService push)
    {
        _db = db;
        _currentUser = currentUser;
        _configuration = configuration;
        _push = push;
    }

    public async Task<IReadOnlyList<ContainerListItemDto>> ListInspectableContainersAsync(CancellationToken cancellationToken = default)
    {
        await RequireDoctorAsync(cancellationToken);
        var containers = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .Where(c => c.Status == ContainerStatus.InTransit || c.Status == ContainerStatus.UnderInspection)
            .OrderByDescending(c => c.ArrivalTime ?? c.UpdatedAt)
            .ToListAsync(cancellationToken);
        return containers.Select(OpsDtoMapper.MapContainer).ToList();
    }

    public async Task<ContainerListItemDto> ScanDeliveryHandoffQrAsync(
        ScanTransportQrRequest request,
        CancellationToken cancellationToken = default)
    {
        var doctor = await RequireDoctorAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.QrData))
        {
            throw new ClientPortalException("QR_REQUIRED", "QR data is required.");
        }

        DeliveryHandoffQrPayload payload;
        try
        {
            payload = DeliveryHandoffQrCodec.Verify(
                request.QrData.Trim(),
                TransportQrSigningKeyResolver.Resolve(_configuration));
        }
        catch (InvalidOperationException ex)
        {
            throw new ClientPortalException("INVALID_QR", ex.Message);
        }

        var token = await _db.ContainerHandoffTokens
            .Include(t => t.Container).ThenInclude(c => c.Entry)
            .FirstOrDefaultAsync(t => t.Nonce == payload.Nonce, cancellationToken)
            ?? throw new ClientPortalException("INVALID_QR", "Handoff QR is not recognized. Ask the driver to refresh the code.");

        if (!string.Equals(token.QrPayload, request.QrData.Trim(), StringComparison.Ordinal))
        {
            throw new ClientPortalException("INVALID_QR", "Handoff QR payload does not match the active code.");
        }

        if (token.ConsumedAt is not null)
        {
            _db.DeliveryComplianceEvents.Add(new DeliveryComplianceEvent
            {
                ContainerId = token.ContainerId,
                EventType = DeliveryComplianceEventTypes.HandoffQrReplay,
                Details = "Replay of consumed handoff QR",
                RecordedAt = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(cancellationToken);
            throw new ClientPortalException("QR_ALREADY_USED", "This QR code was already scanned and cannot be used again.");
        }

        if (token.ExpiresAt < DateTime.UtcNow)
        {
            throw new ClientPortalException("QR_EXPIRED", "Handoff QR has expired. Ask the driver to open AgriTrack and show a fresh code.");
        }

        var container = token.Container;
        if (container.Uuid != payload.ContainerUuid)
        {
            throw new ClientPortalException("INVALID_QR", "Handoff QR does not match this container.");
        }

        if (container.Status is not ContainerStatus.InTransit and not ContainerStatus.PickedUp)
        {
            throw new ClientPortalException("INVALID_STATUS", "Container is not awaiting warehouse handoff.");
        }

        if (DeliveryComplianceHoldHelper.HasActiveHold(container))
        {
            throw new ClientPortalException(
                "COMPLIANCE_HOLD",
                "NMIS/Doctor cannot end this trip while it is flagged for misroute or wrong warehouse. DA authority must clear it first.");
        }

        if (container.WarehouseHandoffEligibleAt is null)
        {
            throw new ClientPortalException("HANDOFF_NOT_READY", "Driver has not confirmed arrival at the assigned warehouse.");
        }

        token.ConsumedAt = DateTime.UtcNow;
        token.ConsumedByUserId = doctor.Id;
        container.Status = ContainerStatus.UnderInspection;
        container.ArrivalTime ??= DateTime.UtcNow;
        if (container.Entry.CompliancePhase < EntryCompliancePhase.AtWarehouse)
        {
            container.Entry.CompliancePhase = EntryCompliancePhase.AtWarehouse;
        }

        var active = await _db.ContainerDoctorInspections
            .FirstOrDefaultAsync(i => i.ContainerId == container.Id && i.Status == DoctorInspectionStatus.InProgress, cancellationToken);
        if (active is null)
        {
            _db.ContainerDoctorInspections.Add(new ContainerDoctorInspection
            {
                Uuid = Guid.NewGuid(),
                ContainerId = container.Id,
                DoctorUserId = doctor.Id,
                Status = DoctorInspectionStatus.InProgress,
                ClaimedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _push.NotifyWarehouseArrivalAsync(
            container.ContainerNumber,
            container.Uuid,
            "Assigned warehouse",
            cancellationToken);

        return OpsDtoMapper.MapContainer(container);
    }

    public async Task<ContainerListItemDto> ClaimContainerAsync(Guid containerUuid, CancellationToken cancellationToken = default)
    {
        var doctor = await RequireDoctorAsync(cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (DeliveryComplianceHoldHelper.HasActiveHold(container))
        {
            throw new ClientPortalException(
                "COMPLIANCE_HOLD",
                "This delivery is on DA compliance hold (misroute / wrong warehouse). Clearance is required before inspection.");
        }

        if (container.Status is not ContainerStatus.InTransit and not ContainerStatus.UnderInspection)
        {
            throw new ClientPortalException("INVALID_STATUS", "Container is not available for doctor inspection.");
        }

        var active = await _db.ContainerDoctorInspections
            .FirstOrDefaultAsync(i => i.ContainerId == container.Id && i.Status == DoctorInspectionStatus.InProgress, cancellationToken);

        if (active is null)
        {
            _db.ContainerDoctorInspections.Add(new ContainerDoctorInspection
            {
                Uuid = Guid.NewGuid(),
                ContainerId = container.Id,
                DoctorUserId = doctor.Id,
                Status = DoctorInspectionStatus.InProgress,
                ClaimedAt = DateTime.UtcNow
            });
        }

        container.Status = ContainerStatus.UnderInspection;
        container.ArrivalTime ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return OpsDtoMapper.MapContainer(container);
    }

    public async Task<ContainerListItemDto> CompleteInspectionAsync(
        Guid containerUuid,
        CompleteDoctorInspectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var doctor = await RequireDoctorAsync(cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        var inspection = await _db.ContainerDoctorInspections
            .Where(i => i.ContainerId == container.Id)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Doctor inspection record not found.");

        if (!string.Equals(request.Decision, "Approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Decision, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            throw new ClientPortalException("INVALID_DECISION", "Decision must be Approved or Rejected.");
        }

        var approved = string.Equals(request.Decision, "Approved", StringComparison.OrdinalIgnoreCase);
        inspection.Status = approved ? DoctorInspectionStatus.Approved : DoctorInspectionStatus.Rejected;
        inspection.Findings = request.Findings?.Trim();
        inspection.CompletedAt = DateTime.UtcNow;
        inspection.DoctorUserId = doctor.Id;
        container.Status = approved ? ContainerStatus.Inspected : ContainerStatus.InTransit;
        await _db.SaveChangesAsync(cancellationToken);
        return OpsDtoMapper.MapContainer(container);
    }

    private async Task<User> RequireDoctorAsync(CancellationToken cancellationToken)
    {
        var user = await UserContextHelper.RequireUserAsync(_db, _currentUser, cancellationToken);
        if (!_currentUser.IsInRole("ROLE_DOCTOR") && !_currentUser.IsInRole("ROLE_INSPECTOR") && !_currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "Doctor access required.");
        }

        return user;
    }
}
