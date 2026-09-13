using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class DriverProfile : BaseEntity
{
    public long UserId { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }
    public string? VehicleType { get; set; }
    public string? VehicleRegistration { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Address { get; set; }
    public DateTime? BirthDate { get; set; }
    public long? RegionId { get; set; }
    public long? ProvinceId { get; set; }
    public long? CityId { get; set; }
    public long? BarangayId { get; set; }
    public string? ZipCode { get; set; }
    public string? StreetAddress { get; set; }
    public long? OperatorUserId { get; set; }
    public int CompletionPercentage { get; set; }
    public bool FaceVerified { get; set; }
    public DateTime? FaceVerifiedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public User User { get; set; } = null!;
    public User? OperatorUser { get; set; }
    public ICollection<DriverDocument> Documents { get; set; } = new List<DriverDocument>();
    public ICollection<DriverProfileHistory> History { get; set; } = new List<DriverProfileHistory>();
}
