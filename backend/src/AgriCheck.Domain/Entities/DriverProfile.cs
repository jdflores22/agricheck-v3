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
    public int CompletionPercentage { get; set; }
    public bool FaceVerified { get; set; }
    public DateTime? FaceVerifiedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<DriverDocument> Documents { get; set; } = new List<DriverDocument>();
    public ICollection<DriverProfileHistory> History { get; set; } = new List<DriverProfileHistory>();
}
