using AgriCheck.Domain.Common;

namespace AgriCheck.Domain.Entities;

public class EntryFile : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EntryId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? DocumentType { get; set; }

    public Entry Entry { get; set; } = null!;
    public ICollection<FileEvaluation> Evaluations { get; set; } = new List<FileEvaluation>();
    public ICollection<EntryFileVersion> Versions { get; set; } = new List<EntryFileVersion>();
}
