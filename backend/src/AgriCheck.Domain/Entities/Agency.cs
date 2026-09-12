using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class Agency : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public long? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SettingsJson { get; set; }

    public Agency? Parent { get; set; }
    public ICollection<Agency> Children { get; set; } = new List<Agency>();
    public ICollection<AgencyMembership> Memberships { get; set; } = new List<AgencyMembership>();
    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
}
