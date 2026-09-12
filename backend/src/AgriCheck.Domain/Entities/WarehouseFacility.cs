using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class WarehouseFacility : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public long? RegionId { get; set; }
    public long? ProvinceId { get; set; }
    public long? CityId { get; set; }
    public long? BarangayId { get; set; }
    public string? StreetAddress { get; set; }
    public string? ZipCode { get; set; }

    public AddressRegion? Region { get; set; }
    public AddressProvince? Province { get; set; }
    public AddressCity? City { get; set; }
    public AddressBarangay? Barangay { get; set; }
    public ICollection<WarehouseBooking> Bookings { get; set; } = new List<WarehouseBooking>();
}
