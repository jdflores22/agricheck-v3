using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class ErfiSubmission : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EntryId { get; set; }
    public long SpsImportClearanceId { get; set; }
    public long FiledByUserId { get; set; }
    public ErfiSubmissionStatus Status { get; set; } = ErfiSubmissionStatus.Draft;
    public DateTime? FiledAt { get; set; }
    public DateTime? EtaAtPoe { get; set; }
    public string? BolReference { get; set; }
    public string? IfmDestinationJson { get; set; }

    public Entry Entry { get; set; } = null!;
    public SpsImportClearance SpsImportClearance { get; set; } = null!;
    public User FiledByUser { get; set; } = null!;
}
