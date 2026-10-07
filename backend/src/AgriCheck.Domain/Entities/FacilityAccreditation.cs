using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

/// <summary>
/// CSW (cold storage warehouse) accreditation under TRA — Phase 2 after RSAS registration.
/// </summary>
public class FacilityAccreditation : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long FacilityRegistrationId { get; set; }
    public long WarehouseFacilityId { get; set; }
    public FacilityAccreditationStatus Status { get; set; } = FacilityAccreditationStatus.Pending;
    public string? CertificateNumber { get; set; }
    public long? LeadAgencyId { get; set; }
    public DateTime? AccreditedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public FacilityRegistration FacilityRegistration { get; set; } = null!;
    public WarehouseFacility WarehouseFacility { get; set; } = null!;
    public Agency? LeadAgency { get; set; }
}
