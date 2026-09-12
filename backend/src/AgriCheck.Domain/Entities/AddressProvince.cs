using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class AddressProvince : BaseEntity
{
    public long RegionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public AddressRegion Region { get; set; } = null!;
    public ICollection<AddressCity> Cities { get; set; } = new List<AddressCity>();
}
