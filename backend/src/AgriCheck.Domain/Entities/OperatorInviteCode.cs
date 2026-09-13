using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class OperatorInviteCode : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public long OperatorUserId { get; set; }
    public string? Label { get; set; }
    public int MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public User OperatorUser { get; set; } = null!;
}
