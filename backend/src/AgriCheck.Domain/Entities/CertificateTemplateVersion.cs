using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class CertificateTemplateVersion : BaseEntity
{
    public long TemplateId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public bool IsPublished { get; set; }
    public string? LayoutJson { get; set; }

    public CertificateTemplate Template { get; set; } = null!;
    public ICollection<CertificateElement> Elements { get; set; } = new List<CertificateElement>();
}
