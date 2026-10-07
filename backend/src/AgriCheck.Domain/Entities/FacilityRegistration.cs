using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class FacilityRegistration : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long WarehouseFacilityId { get; set; }
    public long SubmittedByUserId { get; set; }
    public FacilityRegistrationStatus Status { get; set; } = FacilityRegistrationStatus.Draft;
    public string? RsasExternalRef { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public decimal? MaxCapacityKg { get; set; }
    public string? ProductsJson { get; set; }
    public string? FormDataJson { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ReviewComments { get; set; }
    public bool IsBmbeExempt { get; set; }

    public WarehouseFacility WarehouseFacility { get; set; } = null!;
    public User SubmittedByUser { get; set; } = null!;
    public ICollection<FacilityAccreditation> Accreditations { get; set; } = new List<FacilityAccreditation>();
}
