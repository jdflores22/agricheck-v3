using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class Certificate : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public long? EntryId { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public CertificateStatus Status { get; set; } = CertificateStatus.Active;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string? QrCodeData { get; set; }
    public string? SummaryJson { get; set; }
    public long? TemplateVersionId { get; set; }
    public long? AgencyId { get; set; }
    public long? IssuedByUserId { get; set; }
    public string? PdfStoredFileName { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    public User User { get; set; } = null!;
    public Entry? Entry { get; set; }
    public CertificateTemplateVersion? TemplateVersion { get; set; }
    public Agency? Agency { get; set; }
    public User? IssuedBy { get; set; }
}
