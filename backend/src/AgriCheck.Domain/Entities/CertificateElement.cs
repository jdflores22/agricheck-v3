using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class CertificateElement : BaseEntity
{
    public long VersionId { get; set; }
    public CertificateElementType ElementType { get; set; } = CertificateElementType.Text;
    public string Label { get; set; } = string.Empty;
    public string? ConfigJson { get; set; }
    public int SortOrder { get; set; }

    public CertificateTemplateVersion Version { get; set; } = null!;
}
