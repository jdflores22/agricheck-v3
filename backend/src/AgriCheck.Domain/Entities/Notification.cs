using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityUuid { get; set; }
    public bool IsRead { get; set; }

    public User User { get; set; } = null!;
}
