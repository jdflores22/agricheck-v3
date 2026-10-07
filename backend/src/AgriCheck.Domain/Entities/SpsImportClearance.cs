using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class SpsImportClearance : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EntryId { get; set; }
    public long ApplicationId { get; set; }
    public long IssuingAgencyId { get; set; }
    public string ClearanceNumber { get; set; } = string.Empty;
    public SpsImportClearanceStatus Status { get; set; } = SpsImportClearanceStatus.Issued;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? MustShipOutBy { get; set; }
    public DateTime? MustArriveBy { get; set; }
    public string? PortOfEntry { get; set; }
    public long? FinalWarehouseFacilityId { get; set; }
    public string? CommodityDescription { get; set; }
    public string? DaTradeReference { get; set; }
    public DateTime? RevokedAt { get; set; }

    public Entry Entry { get; set; } = null!;
    public SpsImportClearanceApplication Application { get; set; } = null!;
    public Agency IssuingAgency { get; set; } = null!;
    public WarehouseFacility? FinalWarehouseFacility { get; set; }
}
