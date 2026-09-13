using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class Entry : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string ReferenceNo { get; set; } = string.Empty;
    public long UserId { get; set; }
    public long AgencyId { get; set; }
    public EntryType EntryType { get; set; }
    public EntryStatus Status { get; set; } = EntryStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public decimal? PaymentAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public string? Notes { get; set; }
    public string? FormDataJson { get; set; }
    public DateTime? ComplianceDeadlineAt { get; set; }
    public string? MavNo { get; set; }
    public EntryImportTrack ImportTrack { get; set; } = EntryImportTrack.Regular;
    public EntryMavDocumentStatus MavDocumentStatus { get; set; } = EntryMavDocumentStatus.NotProvided;
    public string? MavRemarks { get; set; }
    public long? PrimaryMicId { get; set; }

    public User User { get; set; } = null!;
    public MavImportCertificate? PrimaryMic { get; set; }
    public Agency Agency { get; set; } = null!;
    public EntryDetail? Detail { get; set; }
    public ICollection<EntryStatusHistory> StatusHistory { get; set; } = new List<EntryStatusHistory>();
    public ICollection<TimelineEvent> TimelineEvents { get; set; } = new List<TimelineEvent>();
    public ICollection<EntryFile> Files { get; set; } = new List<EntryFile>();
    public ICollection<ClientBill> Bills { get; set; } = new List<ClientBill>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<EvaluatorAssignment> EvaluatorAssignments { get; set; } = new List<EvaluatorAssignment>();
    public ICollection<EvaluatorNote> EvaluatorNotes { get; set; } = new List<EvaluatorNote>();
    public ICollection<EntryComplianceResult> ComplianceResults { get; set; } = new List<EntryComplianceResult>();
    public ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
    public ICollection<AgencyBilling> AgencyBillings { get; set; } = new List<AgencyBilling>();
    public ICollection<Container> Containers { get; set; } = new List<Container>();
    public ICollection<MicUtilization> MicUtilizations { get; set; } = new List<MicUtilization>();
}
