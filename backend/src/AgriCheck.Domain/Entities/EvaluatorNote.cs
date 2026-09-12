using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class EvaluatorNote : BaseEntity
{
    public long EntryId { get; set; }
    public long AuthorUserId { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = true;

    public Entry Entry { get; set; } = null!;
    public User Author { get; set; } = null!;
}
