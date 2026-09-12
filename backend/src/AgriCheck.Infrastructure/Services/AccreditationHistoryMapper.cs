using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Infrastructure.Services;

internal static class AccreditationHistoryMapper
{
    public static AccreditationHistoryDto ForClient(AccreditationHistory history, long applicantUserId) =>
        new(
            history.Status.ToString(),
            GetClientComment(history, applicantUserId),
            history.CreatedAt);

    public static AccreditationHistoryDto ForOfficer(AccreditationHistory history) =>
        new(
            history.Status.ToString(),
            history.Comment,
            history.CreatedAt,
            FormatActorName(history.Actor));

    private static string? GetClientComment(AccreditationHistory history, long applicantUserId)
    {
        var isApplicantAction = history.ActorUserId == applicantUserId;

        if (isApplicantAction)
        {
            return history.Status switch
            {
                AccreditationSubmissionStatus.Submitted => "Submitted for review",
                AccreditationSubmissionStatus.UnderReview => "Revised documents resubmitted",
                _ => history.Comment,
            };
        }

        return history.Status switch
        {
            AccreditationSubmissionStatus.UnderReview => "Under review by accreditation officer",
            AccreditationSubmissionStatus.Submitted => "Returned to review queue",
            AccreditationSubmissionStatus.Approved or AccreditationSubmissionStatus.Rejected or AccreditationSubmissionStatus.RevisionRequired
                => history.Comment,
            _ => "Updated by accreditation officer",
        };
    }

    private static string? FormatActorName(User? actor)
    {
        if (actor is null) return null;
        if (actor.Profile is not null)
        {
            var name = $"{actor.Profile.FirstName} {actor.Profile.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(name)) return name;
        }

        return actor.Email;
    }
}
