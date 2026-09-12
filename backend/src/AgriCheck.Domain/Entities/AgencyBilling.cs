using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class AgencyBilling : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long AgencyId { get; set; }
    public long? EntryId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public AgencyBillingStatus Status { get; set; } = AgencyBillingStatus.Draft;
    public long IssuedByUserId { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentProofStoredFileName { get; set; }
    public string? PaymentProofOriginalFileName { get; set; }
    public string? PaymentProofContentType { get; set; }
    public string? PaymentNotes { get; set; }
    public DateTime? PaymentUploadedAt { get; set; }
    public long? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }

    public Agency Agency { get; set; } = null!;
    public Entry? Entry { get; set; }
    public User IssuedBy { get; set; } = null!;
    public User? VerifiedBy { get; set; }
}
