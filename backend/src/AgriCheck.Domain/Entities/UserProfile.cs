using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class UserProfile : BaseEntity
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? Address { get; set; }

    public User User { get; set; } = null!;
}
