namespace AgriCheck.Domain.Entities;

public class FormAgencyTag
{
    public long TemplateId { get; set; }
    public long AgencyId { get; set; }

    public FormTemplate Template { get; set; } = null!;
    public Agency Agency { get; set; } = null!;
}
