using AgriCheck.Domain.Common;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Domain.Entities;

public class ClientBill : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public long? EntryId { get; set; }
    public long? WarehouseBookingId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ClientBillStatus Status { get; set; } = ClientBillStatus.Unpaid;
    public DateTime? DueDate { get; set; }
    public string? PaymentLinkToken { get; set; }
    public DateTime? PaidAt { get; set; }

    public User User { get; set; } = null!;
    public Entry? Entry { get; set; }
    public WarehouseBooking? WarehouseBooking { get; set; }
    public ICollection<ClientBillPayment> Payments { get; set; } = new List<ClientBillPayment>();
}
