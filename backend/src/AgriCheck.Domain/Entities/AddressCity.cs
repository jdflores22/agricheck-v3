using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class AddressCity : BaseEntity
{
    public long ProvinceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public AddressProvince Province { get; set; } = null!;
    public ICollection<AddressBarangay> Barangays { get; set; } = new List<AddressBarangay>();
}
