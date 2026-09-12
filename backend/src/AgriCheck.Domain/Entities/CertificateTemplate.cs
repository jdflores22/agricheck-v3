using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class CertificateTemplate : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? AgencyId { get; set; }
    public bool IsActive { get; set; } = true;

    public Agency? Agency { get; set; }
    public ICollection<CertificateTemplateVersion> Versions { get; set; } = new List<CertificateTemplateVersion>();
    public ICollection<CertificateProcessAssignment> ProcessAssignments { get; set; } = new List<CertificateProcessAssignment>();
}
