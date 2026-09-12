using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class EmailVerificationToken : BaseEntity
{
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public User User { get; set; } = null!;
}
