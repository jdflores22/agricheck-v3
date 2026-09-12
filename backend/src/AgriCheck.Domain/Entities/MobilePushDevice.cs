using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class MobilePushDevice : BaseEntity
{
    public long UserId { get; set; }
    public string ExpoPushToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
