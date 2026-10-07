using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class SpsPrerequisiteClearance : BaseEntity
{
    public long EntryId { get; set; }
    public long SpsApplicationId { get; set; }
    public long IssuingAgencyId { get; set; }
    public string PrerequisiteCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ReferenceNumber { get; set; }
    public DateTime? ClearedAt { get; set; }

    public Entry Entry { get; set; } = null!;
    public SpsImportClearanceApplication Application { get; set; } = null!;
    public Agency IssuingAgency { get; set; } = null!;
}
