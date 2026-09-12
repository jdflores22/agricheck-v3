using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class FileEvaluation : BaseEntity
{
    public long EntryFileId { get; set; }
    public long EvaluatorUserId { get; set; }
    public EvaluationDecision Decision { get; set; } = EvaluationDecision.Pending;
    public string? Comment { get; set; }

    public EntryFile EntryFile { get; set; } = null!;
    public User Evaluator { get; set; } = null!;
}
