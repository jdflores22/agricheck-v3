using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class ReleaseAuthorization : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EntryId { get; set; }
    public long AuthorizedByUserId { get; set; }
    public DateTime AuthorizedAt { get; set; } = DateTime.UtcNow;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientIdNumber { get; set; } = string.Empty;

    public Entry Entry { get; set; } = null!;
    public User AuthorizedBy { get; set; } = null!;
    public ICollection<ReleaseRecord> ReleaseRecords { get; set; } = new List<ReleaseRecord>();
}
