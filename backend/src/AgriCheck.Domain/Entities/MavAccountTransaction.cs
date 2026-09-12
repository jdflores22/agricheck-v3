using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class MavAccountTransaction : BaseEntity
{
    public long AccountId { get; set; }
    public MavAccountTransactionType TransactionType { get; set; }
    public decimal Volume { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public long CreatedByUserId { get; set; }

    public MavAccount Account { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}
