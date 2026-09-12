using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class EntryStatusHistory : BaseEntity
{
    public long EntryId { get; set; }
    public EntryStatus FromStatus { get; set; }
    public EntryStatus ToStatus { get; set; }
    public long? ChangedByUserId { get; set; }
    public string? Comment { get; set; }

    public Entry Entry { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
