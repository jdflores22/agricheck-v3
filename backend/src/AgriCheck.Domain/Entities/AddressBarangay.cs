using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class AddressBarangay : BaseEntity
{
    public long CityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public AddressCity City { get; set; } = null!;
}
