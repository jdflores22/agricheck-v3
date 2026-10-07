using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;

namespace AgriCheck.Infrastructure.Helpers;

public static class ComplianceTimelineRecorder
{
    public static void Add(AgriCheckDbContext db, long entryId, string eventType, string title, string? description, long? actorUserId)
    {
        db.TimelineEvents.Add(new TimelineEvent
        {
            EntryId = entryId,
            EventType = eventType,
            Title = title,
            Description = description,
            ActorUserId = actorUserId,
        });
    }
}
