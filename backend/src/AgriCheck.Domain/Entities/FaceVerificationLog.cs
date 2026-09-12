using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class FaceVerificationLog : BaseEntity
{
    public long UserId { get; set; }
    public bool Success { get; set; }
    public decimal? Confidence { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
}
