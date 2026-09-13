using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal static class AgencyBillingClientBillHelper
{
    public static async Task<ClientBill> EnsureClientBillAsync(
        AgriCheckDbContext db,
        AgencyBilling billing,
        CancellationToken cancellationToken = default)
    {
        if (billing.EntryId is null)
        {
            throw new ClientPortalException("ENTRY_REQUIRED", "Agency billing must be linked to an entry.");
        }

        if (billing.Status is AgencyBillingStatus.Draft or AgencyBillingStatus.Cancelled)
        {
            throw new ClientPortalException("INVALID_STATUS", "Only issued agency billings can create client bills.");
        }

        var existing = await db.ClientBills
            .FirstOrDefaultAsync(b => b.AgencyBillingId == billing.Id, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var entry = billing.Entry
            ?? await db.Entries.FirstAsync(e => e.Id == billing.EntryId, cancellationToken);

        var clientBill = new ClientBill
        {
            Uuid = Guid.NewGuid(),
            UserId = entry.UserId,
            EntryId = entry.Id,
            AgencyBillingId = billing.Id,
            BillNumber = billing.BillNumber,
            Description = billing.Description,
            Amount = billing.Amount,
            Status = ClientBillStatus.Unpaid,
            DueDate = DateTime.UtcNow.AddDays(14),
            PaymentLinkToken = Guid.NewGuid().ToString("N")
        };

        db.ClientBills.Add(clientBill);
        await db.SaveChangesAsync(cancellationToken);

        return clientBill;
    }

    public static async Task BackfillMissingClientBillsForUserAsync(
        AgriCheckDbContext db,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var missing = await db.AgencyBillings
            .Include(b => b.Entry)
            .Where(b =>
                b.Entry!.UserId == userId &&
                b.Status != AgencyBillingStatus.Draft &&
                b.Status != AgencyBillingStatus.Cancelled &&
                !db.ClientBills.Any(cb => cb.AgencyBillingId == b.Id))
            .ToListAsync(cancellationToken);

        foreach (var billing in missing)
        {
            await EnsureClientBillAsync(db, billing, cancellationToken);
        }
    }

    public static async Task TryCompleteAgencyBillingFromClientBillAsync(
        AgriCheckDbContext db,
        IEntryWorkflowService workflow,
        ClientBill bill,
        long actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (bill.AgencyBillingId is not long agencyBillingId)
        {
            return;
        }

        var agencyBilling = await db.AgencyBillings
            .Include(b => b.Entry)
            .FirstOrDefaultAsync(b => b.Id == agencyBillingId, cancellationToken);

        if (agencyBilling is null || agencyBilling.Status == AgencyBillingStatus.Paid)
        {
            return;
        }

        agencyBilling.Status = AgencyBillingStatus.Paid;
        agencyBilling.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await workflow.OnDaBillingPaidAsync(agencyBilling, actorUserId, cancellationToken);
    }
}
