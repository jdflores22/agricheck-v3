using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class CertificateProcessAssignment : BaseEntity
{
    public long AgencyId { get; set; }
    public CertificateProcessType ProcessType { get; set; }
    public long TemplateId { get; set; }
    public bool IsActive { get; set; } = true;

    public Agency Agency { get; set; } = null!;
    public CertificateTemplate Template { get; set; } = null!;
}
