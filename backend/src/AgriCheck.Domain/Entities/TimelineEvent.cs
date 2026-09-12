using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class TimelineEvent : BaseEntity
{
    public long EntryId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? ActorUserId { get; set; }

    public Entry Entry { get; set; } = null!;
    public User? ActorUser { get; set; }
}
