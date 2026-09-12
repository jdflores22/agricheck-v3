using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class FormTemplate : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string FormType { get; set; } = "ENTRY";
    public FormTemplateStatus Status { get; set; } = FormTemplateStatus.Draft;
    public bool IsActive { get; set; } = true;

    public ICollection<FormTemplateVersion> Versions { get; set; } = new List<FormTemplateVersion>();
    public ICollection<FormAgencyTag> AgencyTags { get; set; } = new List<FormAgencyTag>();
}
