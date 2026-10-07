using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

/// <summary>DA AC 06 Art. III §3 attachment codes (A–H).</summary>
public class SpsDocumentChecklistTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;
}
