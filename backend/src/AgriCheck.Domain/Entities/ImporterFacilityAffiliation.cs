using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

/// <summary>
/// Links an importer account to a DA-licensed warehouse they use for RSAS/destination compliance.
/// </summary>
public class ImporterFacilityAffiliation : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public long WarehouseFacilityId { get; set; }

    public User User { get; set; } = null!;
    public WarehouseFacility WarehouseFacility { get; set; } = null!;
}
