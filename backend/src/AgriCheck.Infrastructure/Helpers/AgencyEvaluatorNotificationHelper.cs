using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal static class AgencyEvaluatorNotificationHelper
{
    private const string EvaluatorRole = "ROLE_EVALUATOR";

    public static async Task NotifyEntryReadyForEvaluationAsync(
        AgriCheckDbContext db,
        INotificationService notifications,
        Entry entry,
        CancellationToken cancellationToken = default)
    {
        if (entry.PaymentStatus != PaymentStatus.Paid)
        {
            return;
        }

        if (entry.Status is not (EntryStatus.Submitted or EntryStatus.UnderReview))
        {
            return;
        }

        var evaluatorIds = await GetAgencyEvaluatorUserIdsAsync(db, entry.AgencyId, cancellationToken);
        foreach (var evaluatorId in evaluatorIds)
        {
            await notifications.NotifyAsync(
                evaluatorId,
                "evaluation_queue",
                "Entry ready for evaluation",
                $"{entry.ReferenceNo} has been paid and is available in the evaluation queue.",
                "Entry",
                entry.Uuid.ToString(),
                cancellationToken);
        }
    }

    public static async Task NotifyComplianceResubmittedAsync(
        AgriCheckDbContext db,
        INotificationService notifications,
        Entry entry,
        CancellationToken cancellationToken = default)
    {
        var assignedEvaluatorIds = await db.EvaluatorAssignments
            .Where(a => a.EntryId == entry.Id && a.Status == AssignmentStatus.Active)
            .Select(a => a.EvaluatorUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var targetIds = assignedEvaluatorIds.Count > 0
            ? assignedEvaluatorIds
            : await GetAgencyEvaluatorUserIdsAsync(db, entry.AgencyId, cancellationToken);

        foreach (var evaluatorId in targetIds)
        {
            await notifications.NotifyAsync(
                evaluatorId,
                "compliance_resubmitted",
                "Compliance documents resubmitted",
                $"{entry.ReferenceNo} has revised documents ready for your review.",
                "Entry",
                entry.Uuid.ToString(),
                cancellationToken);
        }
    }

    private static async Task<IReadOnlyList<long>> GetAgencyEvaluatorUserIdsAsync(
        AgriCheckDbContext db,
        long agencyId,
        CancellationToken cancellationToken)
    {
        return await db.AgencyMemberships
            .Where(m => m.AgencyId == agencyId)
            .SelectMany(m => m.User.UserRoles)
            .Where(ur => ur.Role.Name == EvaluatorRole)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
