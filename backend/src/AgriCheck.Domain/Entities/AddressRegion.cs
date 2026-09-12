using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class AddressRegion : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<AddressProvince> Provinces { get; set; } = new List<AddressProvince>();
}
