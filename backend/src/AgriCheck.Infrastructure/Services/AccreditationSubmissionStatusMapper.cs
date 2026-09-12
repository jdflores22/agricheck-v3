using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Infrastructure.Services;

internal static class AccreditationSubmissionStatusMapper
{
    public static string GetDisplayStatus(AccreditationSubmission submission)
    {
        return submission.Status switch
        {
            AccreditationSubmissionStatus.RevisionRequired => "Revision Required",
            AccreditationSubmissionStatus.UnderReview => GetUnderReviewDisplayStatus(submission),
            AccreditationSubmissionStatus.Approved => "Approved",
            AccreditationSubmissionStatus.Rejected => "Rejected",
            AccreditationSubmissionStatus.Submitted => "Submitted",
            AccreditationSubmissionStatus.Draft => "Draft",
            _ => submission.Status.ToString(),
        };
    }

    public static string GetStatusChipKey(AccreditationSubmission submission)
    {
        var displayStatus = GetDisplayStatus(submission);
        if (displayStatus == "Resubmitted for Review") return "ResubmittedForReview";
        if (displayStatus == "Revision Required") return "RevisionRequired";
        return submission.Status.ToString();
    }

    private static string GetUnderReviewDisplayStatus(AccreditationSubmission submission)
    {
        var history = submission.History
            .OrderByDescending(h => h.CreatedAt)
            .ToList();

        var latest = history.FirstOrDefault();
        var hadRevisionRequired = history.Any(h => h.Status == AccreditationSubmissionStatus.RevisionRequired);
        var latestIsClientResubmit =
            latest?.Status == AccreditationSubmissionStatus.UnderReview &&
            (latest.Comment?.Contains("resubmitted", StringComparison.OrdinalIgnoreCase) ?? false);

        if (hadRevisionRequired && latestIsClientResubmit)
        {
            return "Resubmitted for Review";
        }

        return "Under Review";
    }
}
