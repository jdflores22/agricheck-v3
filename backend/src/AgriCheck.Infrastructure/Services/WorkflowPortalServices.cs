using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
        b.VerificationNotes);
}

public interface IContainerInspectionWorkflowService
{
    Task<IReadOnlyList<ClientContainerInspectionStatusDto>> ListForEntryAsync(Guid entryUuid, CancellationToken cancellationToken = default);
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
}

public class ContainerInspectionWorkflowService : IContainerInspectionWorkflowService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IEntryWorkflowService _workflow;

    public ContainerInspectionWorkflowService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IEntryWorkflowService workflow)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _workflow = workflow;
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
            .FirstOrDefaultAsync(p => p.Uuid == photoUuid && p.Container.Entry.AgencyId == agency.Id, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Photo not found.");

        photo.ReviewDecision = decision;
        photo.ReviewComment = request.Comment?.Trim();
        photo.ReviewedByUserId = user.Id;
        photo.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        if (decision == EvaluationDecision.Approved)
        {
            await _workflow.TryMarkContainerReadyForTransportAsync(photo.ContainerId, cancellationToken);
        }
        else
        {
            photo.Container.Status = ContainerStatus.Pending;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return MapPhoto(photo, photo.Container.Uuid);
    }

    private static ClientContainerInspectionStatusDto MapStatus(Container container)
    {
        var photos = container.InspectionPhotos
            .OrderBy(p => p.PhotoType)
            .Select(p => MapPhoto(p, container.Uuid))
            .ToList();
        var complete = EntryWorkflowService.RequiredPhotoTypes.All(t =>
            container.InspectionPhotos.Any(p => p.PhotoType == t));
        var approved = complete && EntryWorkflowService.RequiredPhotoTypes.All(t =>
            container.InspectionPhotos.Any(p => p.PhotoType == t && p.ReviewDecision == EvaluationDecision.Approved));
        return new ClientContainerInspectionStatusDto(container.Uuid, container.ContainerNumber, photos, complete, approved);
    }

    private static ClientContainerInspectionPhotoDto MapPhoto(ContainerInspectionPhoto photo, Guid containerUuid) => new(
        photo.Uuid,
        containerUuid,
        photo.PhotoType.ToString(),
        photo.OriginalFileName,
        photo.ReviewDecision.ToString(),
        photo.ReviewComment,
        photo.CreatedAt);
}

public interface ITransportTagService
{
    Task<ContainerListItemDto> AddTransportTagAsync(AddTransportTagRequest request, CancellationToken cancellationToken = default);
}

public class TransportTagService : ITransportTagService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEntryWorkflowService _workflow;

    public TransportTagService(AgriCheckDbContext db, ICurrentUserService currentUser, IEntryWorkflowService workflow)
    {
        _db = db;
        _currentUser = currentUser;
        _workflow = workflow;
    }

    public async Task<ContainerListItemDto> AddTransportTagAsync(
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

        _db.ContainerTransportTags.Add(new ContainerTransportTag
        {
            Uuid = Guid.NewGuid(),
            ContainerId = container.Id,
            EntryId = container.EntryId,
            TransportType = string.IsNullOrWhiteSpace(request.TransportType) ? "warehouse_nmis" : request.TransportType.Trim(),
            TaggedByUserId = user.Id
        });

        container.Status = ContainerStatus.AwaitingConfirmation;
        await _db.SaveChangesAsync(cancellationToken);
        await _workflow.SyncEntryTransportStatusAsync(container.EntryId, cancellationToken);

        return OpsDtoMapper.MapContainer(container);
    }
}

public class OperatorOpsService : IOperatorOpsService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAgriTrackPushService _push;

    public OperatorOpsService(AgriCheckDbContext db, ICurrentUserService currentUser, IAgriTrackPushService push)
    {
        _db = db;
        _currentUser = currentUser;
        _push = push;
    }

    public async Task<IReadOnlyList<ContainerListItemDto>> ListClaimableContainersAsync(CancellationToken cancellationToken = default)
    {
        await RequireOperatorAsync(cancellationToken);
        var containers = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .Where(c => c.Status == ContainerStatus.AwaitingConfirmation && c.ClaimedByUserId == null)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
        return containers.Select(OpsDtoMapper.MapContainer).ToList();
    }

    public async Task<ContainerListItemDto> ClaimContainerAsync(Guid containerUuid, CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (container.Status != ContainerStatus.AwaitingConfirmation)
        {
            throw new ClientPortalException("INVALID_STATUS", "Container is not awaiting confirmation.");
        }

        container.ClaimedByUserId = user.Id;
        container.Status = ContainerStatus.Assigned;
        await _db.SaveChangesAsync(cancellationToken);
        return OpsDtoMapper.MapContainer(container);
    }

    public async Task<ContainerListItemDto> AssignDriverAsync(
        Guid containerUuid,
        AssignDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireOperatorAsync(cancellationToken);
        var driver = await _db.Users.FirstOrDefaultAsync(u => u.Uuid == request.DriverUserUuid, cancellationToken)
            ?? throw new ClientPortalException("DRIVER_NOT_FOUND", "Driver not found.");

        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

        if (container.ClaimedByUserId != user.Id && !_currentUser.IsInRole("ROLE_ADMIN"))
        {
            throw new ClientPortalException("FORBIDDEN", "Only the claiming operator can assign a driver.");
        }

        container.AssignedDriverUserId = driver.Id;
        container.Status = ContainerStatus.Assigned;
        await _db.SaveChangesAsync(cancellationToken);
        await _push.NotifyDriverAssignmentAsync(driver.Id, container.ContainerNumber, container.Uuid, cancellationToken);
        return OpsDtoMapper.MapContainer(container);
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

    public DoctorInspectionService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
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

    public async Task<ContainerListItemDto> ClaimContainerAsync(Guid containerUuid, CancellationToken cancellationToken = default)
    {
        var doctor = await RequireDoctorAsync(cancellationToken);
        var container = await _db.Containers
            .Include(c => c.Entry)
            .Include(c => c.Locations)
            .FirstOrDefaultAsync(c => c.Uuid == containerUuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Container not found.");

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
