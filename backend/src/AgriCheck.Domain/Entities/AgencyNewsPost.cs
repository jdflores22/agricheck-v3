using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class AgencyNewsPost : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long AgencyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    /// <summary>News, Announcement, or Article</summary>
    public string Category { get; set; } = "News";
    public bool IsRegulatory { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int DisplayOrder { get; set; }

    public Agency Agency { get; set; } = null!;
}
