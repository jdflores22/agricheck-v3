using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class FormTemplateVersion : BaseEntity
{
    public long TemplateId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string SchemaJson { get; set; } = "[]";
    public bool IsPublished { get; set; }

    public FormTemplate Template { get; set; } = null!;
}
