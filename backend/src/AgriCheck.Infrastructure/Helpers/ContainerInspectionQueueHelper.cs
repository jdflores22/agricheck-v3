using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Services;

namespace AgriCheck.Infrastructure.Helpers;

internal static class ContainerInspectionQueueHelper
{
    private static readonly HashSet<string> OutcomeEventTypes = new(StringComparer.Ordinal)
    {
        "container_inspection_approved",
        "container_inspection_revision_required",
        "container_inspection_rejected",
    };

    public static bool IsUploadComplete(Container container) =>
        EntryWorkflowService.RequiredPhotoTypes.All(type =>
            container.InspectionPhotos.Any(photo => photo.PhotoType == type));

    public static bool IsApproved(Container container) =>
        IsUploadComplete(container) &&
        EntryWorkflowService.RequiredPhotoTypes.All(type =>
            container.InspectionPhotos.Any(photo =>
                photo.PhotoType == type && photo.ReviewDecision == EvaluationDecision.Approved));

    public static int CountPendingPhotos(Container container) =>
        container.InspectionPhotos.Count(photo => photo.ReviewDecision == EvaluationDecision.Pending);

    public static DateTime? ResolveSubmittedAt(Container container)
    {
        if (!IsUploadComplete(container))
        {
            return null;
        }

        return container.InspectionPhotos.Max(photo => photo.CreatedAt);
    }

    public static bool NeedsOutcomeSubmission(Container container)
    {
        if (!IsUploadComplete(container))
        {
            return false;
        }

        if (CountPendingPhotos(container) > 0)
        {
            return false;
        }

        if (container.Status == ContainerStatus.ReadyForTransport)
        {
            return false;
        }

        var hasOutcome = container.Entry.TimelineEvents.Any(eventItem =>
            OutcomeEventTypes.Contains(eventItem.EventType) &&
            eventItem.Title.Contains(container.ContainerNumber, StringComparison.OrdinalIgnoreCase));

        return !hasOutcome;
    }

    public static bool NeedsInspectorAction(Container container) =>
        IsUploadComplete(container) && (CountPendingPhotos(container) > 0 || NeedsOutcomeSubmission(container));

    public static InspectorAssignment? GetActiveAssignment(Container container) =>
        container.InspectorAssignments.FirstOrDefault(assignment => assignment.Status == AssignmentStatus.Active);

    public static bool HasActiveAssignment(Container container) => GetActiveAssignment(container) is not null;

    public static bool IsAssignedTo(Container container, long userId) =>
        GetActiveAssignment(container)?.InspectorUserId == userId;

    public static string? ResolveInspectorName(InspectorAssignment? assignment)
    {
        if (assignment?.Inspector is null)
        {
            return null;
        }

        if (assignment.Inspector.Profile is not null)
        {
            return $"{assignment.Inspector.Profile.FirstName} {assignment.Inspector.Profile.LastName}".Trim();
        }

        return assignment.Inspector.Email;
    }
}
