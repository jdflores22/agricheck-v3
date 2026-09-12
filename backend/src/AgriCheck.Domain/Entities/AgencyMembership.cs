namespace AgriCheck.Domain.Entities;

public class AgencyMembership
{
    public long UserId { get; set; }
    public long AgencyId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Agency Agency { get; set; } = null!;
}
