using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public interface IEntryWorkflowService
{
    Task<AgencyBilling> CreateAndIssueDaBillingAsync(Entry entry, long actorUserId, CancellationToken cancellationToken = default);
    Task TransitionEntryAsync(Entry entry, EntryStatus nextStatus, long? actorUserId, string comment, CancellationToken cancellationToken = default);
    Task SyncEntryTransportStatusAsync(long entryId, long? actorUserId = null, CancellationToken cancellationToken = default);
    Task TryMarkContainerReadyForTransportAsync(long containerId, CancellationToken cancellationToken = default);
    Task OnDaBillingPaidAsync(AgencyBilling billing, long actorUserId, CancellationToken cancellationToken = default);
}

public class EntryWorkflowService : IEntryWorkflowService
{
    public static readonly ContainerInspectionPhotoType[] RequiredPhotoTypes =
    {
        ContainerInspectionPhotoType.ActualItem,
        ContainerInspectionPhotoType.LabelImage,
        ContainerInspectionPhotoType.XrayImage,
        ContainerInspectionPhotoType.ExaminationImage,
        ContainerInspectionPhotoType.ReportImage,
        ContainerInspectionPhotoType.RequestForInspection
    };

    private readonly AgriCheckDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IEntryCertificateAutoIssueService _certificateAutoIssue;

    public EntryWorkflowService(
        AgriCheckDbContext db,
        INotificationService notifications,
        IEntryCertificateAutoIssueService certificateAutoIssue)
    {
        _db = db;
        _notifications = notifications;
        _certificateAutoIssue = certificateAutoIssue;
    }

    public async Task<AgencyBilling> CreateAndIssueDaBillingAsync(
        Entry entry,
        long actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.AgencyBillings
            .FirstOrDefaultAsync(
                b => b.EntryId == entry.Id &&
                     b.Status != AgencyBillingStatus.Cancelled,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var feeConfig = await _db.ProcessingFeeConfigs
            .FirstOrDefaultAsync(
                f => f.AgencyId == entry.AgencyId && f.EntryType == entry.EntryType,
                cancellationToken);

        var amount = feeConfig?.Amount ?? 5000m;

        var billing = new AgencyBilling
        {
            Uuid = Guid.NewGuid(),
            AgencyId = entry.AgencyId,
            EntryId = entry.Id,
            BillNumber = await ReferenceNumberGenerator.AgencyBillAsync(_db, cancellationToken),
            Description = "DA regulatory billing",
            Amount = amount,
            Status = AgencyBillingStatus.Issued,
            IssuedByUserId = actorUserId,
            IssuedAt = DateTime.UtcNow
        };

        billing.Charges.Add(new AgencyBillingCharge
        {
            Uuid = Guid.NewGuid(),
            Description = billing.Description,
            Amount = amount,
            SortOrder = 1
        });

        _db.AgencyBillings.Add(billing);
        await _db.SaveChangesAsync(cancellationToken);
        return billing;
    }

    public async Task TransitionEntryAsync(
        Entry entry,
        EntryStatus nextStatus,
        long? actorUserId,
        string comment,
        CancellationToken cancellationToken = default)
    {
        if (entry.Status == nextStatus)
        {
            return;
        }

        var previous = entry.Status;
        entry.Status = nextStatus;
        entry.StatusHistory.Add(new EntryStatusHistory
        {
            FromStatus = previous,
            ToStatus = nextStatus,
            ChangedByUserId = actorUserId,
            Comment = comment
        });
        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "entry_status",
            Title = "Entry status updated",
            Description = comment,
            ActorUserId = actorUserId
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyAsync(
            entry.UserId,
            "entry_status",
            "Entry status updated",
            $"Your entry {entry.ReferenceNo} is now {nextStatus}.",
            "Entry",
            entry.Uuid.ToString(),
            cancellationToken);
    }

    public async Task OnDaBillingPaidAsync(AgencyBilling billing, long actorUserId, CancellationToken cancellationToken = default)
    {
        if (billing.EntryId is null)
        {
            return;
        }

        var entry = await _db.Entries
            .Include(e => e.User).ThenInclude(u => u.Profile)
            .Include(e => e.Agency)
            .Include(e => e.Detail)
            .Include(e => e.TimelineEvents)
            .Include(e => e.StatusHistory)
            .FirstAsync(e => e.Id == billing.EntryId.Value, cancellationToken);

        if (entry.Status is EntryStatus.ForInspection or EntryStatus.ReadyForTransport
            or EntryStatus.AwaitingTransport or EntryStatus.PartiallyConfirmed or EntryStatus.InTransit)
        {
            await _certificateAutoIssue.TryIssueForEntryAsync(entry, actorUserId, cancellationToken);
            return;
        }

        await TransitionEntryAsync(
            entry,
            EntryStatus.ForInspection,
            actorUserId,
            "DA billing payment verified. Entry is now for inspection.",
            cancellationToken);

        await _certificateAutoIssue.TryIssueForEntryAsync(entry, actorUserId, cancellationToken);
    }

    public async Task TryMarkContainerReadyForTransportAsync(long containerId, CancellationToken cancellationToken = default)
    {
        var container = await _db.Containers
            .Include(c => c.InspectionPhotos)
            .Include(c => c.Entry)
            .FirstOrDefaultAsync(c => c.Id == containerId, cancellationToken);

        if (container is null || container.Entry.Status != EntryStatus.ForInspection)
        {
            return;
        }

        var photos = container.InspectionPhotos.ToList();
        if (photos.Count < RequiredPhotoTypes.Length)
        {
            return;
        }

        if (!RequiredPhotoTypes.All(type =>
                photos.Any(p => p.PhotoType == type && p.ReviewDecision == EvaluationDecision.Approved)))
        {
            return;
        }

        if (container.Status == ContainerStatus.ReadyForTransport)
        {
            return;
        }

        container.Status = ContainerStatus.ReadyForTransport;
        container.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var entry = container.Entry;
        var containers = await _db.Containers.Where(c => c.EntryId == entry.Id).ToListAsync(cancellationToken);
        if (containers.All(c => c.Status == ContainerStatus.ReadyForTransport))
        {
            var entryForTransition = await _db.Entries
                .Include(e => e.StatusHistory)
                .Include(e => e.TimelineEvents)
                .FirstAsync(e => e.Id == entry.Id, cancellationToken);
            await TransitionEntryAsync(
                entryForTransition,
                EntryStatus.ReadyForTransport,
                null,
                "All container inspection photos approved. Entry is ready for transport.",
                cancellationToken);
        }
    }

    public async Task SyncEntryTransportStatusAsync(long entryId, long? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var entry = await _db.Entries
            .Include(e => e.StatusHistory)
            .Include(e => e.TimelineEvents)
            .FirstAsync(e => e.Id == entryId, cancellationToken);

        var containers = await _db.Containers.Where(c => c.EntryId == entryId).ToListAsync(cancellationToken);
        if (containers.Count == 0)
        {
            return;
        }

        if (containers.All(c => c.Status is ContainerStatus.InTransit or ContainerStatus.UnderInspection
                or ContainerStatus.Inspected or ContainerStatus.AtWarehouse or ContainerStatus.Released))
        {
            if (entry.Status != EntryStatus.InTransit)
            {
                await TransitionEntryAsync(entry, EntryStatus.InTransit, actorUserId, "All containers are in transit or beyond.", cancellationToken);
            }

            return;
        }

        var tagged = containers.Count(c => c.Status >= ContainerStatus.AwaitingConfirmation);
        if (tagged == 0)
        {
            return;
        }

        var next = tagged == containers.Count
            ? EntryStatus.AwaitingTransport
            : EntryStatus.PartiallyConfirmed;

        if (entry.Status != next && entry.Status < EntryStatus.InTransit)
        {
            await TransitionEntryAsync(
                entry,
                next,
                actorUserId,
                tagged == containers.Count
                    ? "All containers tagged for transport."
                    : "Some containers tagged for transport.",
                cancellationToken);
        }
    }
}
