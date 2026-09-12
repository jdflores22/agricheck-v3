using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class MavNotificationPreference : BaseEntity
{
    public long UserId { get; set; }
    public bool ApplicationSubmitted { get; set; } = true;
    public bool ApplicationApproved { get; set; } = true;
    public bool ApplicationRejected { get; set; } = true;
    public bool LicenseIssued { get; set; } = true;
    public bool LicenseExpiring { get; set; } = true;
    public bool MicIssued { get; set; } = true;
    public bool MicExpiring { get; set; } = true;

    public User User { get; set; } = null!;
}
