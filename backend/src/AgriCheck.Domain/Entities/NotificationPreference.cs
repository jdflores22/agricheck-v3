using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class NotificationPreference : BaseEntity
{
    public long UserId { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;

    public User User { get; set; } = null!;
}
