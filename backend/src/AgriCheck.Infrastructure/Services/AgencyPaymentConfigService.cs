using AgriCheck.Application.AdminPortal.Dtos;
using AgriCheck.Application.AgencyPortal;
using AgriCheck.Application.AgencyPortal.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public class AgencyPaymentConfigService : IAgencyPaymentConfigService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IEntryWorkflowService _workflow;

    public AgencyPaymentConfigService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        INotificationService notifications,
        IEntryWorkflowService workflow)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
        _workflow = workflow;
    }

    public async Task<AgencyPaymentSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyAdminAsync(_db, _currentUser, cancellationToken);
        return await MapSettingsAsync(agency, cancellationToken);
    }

    public async Task<AgencyPaymentSettingsDto> UpdateSettingsAsync(
        UpdateAgencyPaymentSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyAdminAsync(_db, _currentUser, cancellationToken);

        var settings = await _db.AgencyPaymentSettings.FirstOrDefaultAsync(s => s.AgencyId == agency.Id, cancellationToken);
        if (settings is null)
        {
            settings = new AgencyPaymentSettings { AgencyId = agency.Id };
            _db.AgencyPaymentSettings.Add(settings);
        }

        if (request.PayMongoEnabled)
        {
            var hasExistingKey = !string.IsNullOrWhiteSpace(settings.PayMongoApiKey);
            var hasIncomingKey = !string.IsNullOrWhiteSpace(request.PayMongoApiKey) && request.PayMongoApiKey != "********";
            if (!hasExistingKey && !hasIncomingKey)
            {
                throw new ClientPortalException(
                    "PAYMONGO_KEY_REQUIRED",
                    "A PayMongo API key is required when PayMongo is enabled for this agency.");
            }
        }

        settings.PayMongoEnabled = request.PayMongoEnabled;
        settings.CashPaymentEnabled = request.CashPaymentEnabled;
        settings.CashPaymentInstructions = request.CashPaymentInstructions?.Trim();

        if (!string.IsNullOrWhiteSpace(request.PayMongoApiKey) && request.PayMongoApiKey != "********")
        {
            settings.PayMongoApiKey = request.PayMongoApiKey.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.PayMongoWebhookSecret) && request.PayMongoWebhookSecret != "********")
        {
            settings.PayMongoWebhookSecret = request.PayMongoWebhookSecret.Trim();
        }

        if (request.PayMongoPublicKey is not null)
        {
            settings.PayMongoPublicKey = request.PayMongoPublicKey.Trim();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MapSettingsAsync(agency, cancellationToken);
    }

    public async Task<IReadOnlyList<AgencyPendingCashPaymentDto>> ListPendingCashPaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        var (_, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);

        return await _db.ClientBillPayments.AsNoTracking()
            .Where(p =>
                p.Status == "awaiting_verification" &&
                p.PaymentMethod == "cash" &&
                p.ClientBill.AgencyBillingId != null &&
                p.ClientBill.Entry!.AgencyId == agency.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AgencyPendingCashPaymentDto(
                p.ClientBill.Uuid,
                p.ClientBill.BillNumber,
                p.ClientBill.Entry!.ReferenceNo,
                p.ClientBill.User.Profile != null
                    ? (p.ClientBill.User.Profile.FirstName + " " + p.ClientBill.User.Profile.LastName).Trim()
                    : p.ClientBill.User.Email,
                p.Amount,
                p.PaymentMethod,
                p.ExternalReference,
                p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task VerifyCashPaymentAsync(
        Guid billUuid,
        VerifyAgencyCashPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var (agencyUser, agency) = await AgencyContextHelper.RequireAgencyStaffAsync(_db, _currentUser, cancellationToken);

        var bill = await _db.ClientBills
            .Include(b => b.Payments)
            .Include(b => b.Entry)
            .FirstOrDefaultAsync(
                b => b.Uuid == billUuid &&
                     b.AgencyBillingId != null &&
                     b.Entry != null &&
                     b.Entry.AgencyId == agency.Id,
                cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Bill not found.");

        var pending = bill.Payments.FirstOrDefault(p => p.Status == "awaiting_verification" && p.PaymentMethod == "cash")
            ?? throw new ClientPortalException("INVALID_STATUS", "No pending cash payment to verify.");

        if (!request.Approved)
        {
            pending.Status = "rejected";
            bill.Status = ClientBillStatus.Unpaid;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (bill.Status == ClientBillStatus.Paid)
        {
            throw new ClientPortalException("ALREADY_PAID", "Bill is already paid.");
        }

        bill.Status = ClientBillStatus.Paid;
        bill.PaidAt = DateTime.UtcNow;
        pending.Status = "completed";

        Entry? paidEntry = null;
        if (bill.EntryId is not null)
        {
            paidEntry = bill.Entry ?? await _db.Entries.FirstAsync(e => e.Id == bill.EntryId, cancellationToken);
            paidEntry.PaymentStatus = PaymentStatus.Paid;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyAsync(
            bill.UserId,
            "payment_completed",
            "Payment received",
            $"Payment for bill {bill.BillNumber} has been completed.",
            "Bill",
            bill.Uuid.ToString(),
            cancellationToken);

        if (paidEntry is not null)
        {
            if (bill.AgencyBillingId is not null)
            {
                await AgencyBillingClientBillHelper.TryCompleteAgencyBillingFromClientBillAsync(
                    _db,
                    _workflow,
                    bill,
                    agencyUser.Id,
                    cancellationToken);
            }
            else
            {
                await AgencyEvaluatorNotificationHelper.NotifyEntryReadyForEvaluationAsync(
                    _db,
                    _notifications,
                    paidEntry,
                    cancellationToken);
            }
        }
    }

    private async Task<AgencyPaymentSettingsDto> MapSettingsAsync(Agency agency, CancellationToken cancellationToken)
    {
        var settings = await PaymentSettingsReader.TryGetAgencySettingsAsync(_db, agency.Id, cancellationToken);

        var apiKey = settings?.PayMongoApiKey;
        var webhookSecret = settings?.PayMongoWebhookSecret;
        var enabled = settings?.PayMongoEnabled ?? false;
        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
        var mode = enabled && hasApiKey ? "paymongo" : "cash_only";

        return new AgencyPaymentSettingsDto(
            agency.Id,
            agency.Code,
            agency.Name,
            new PaymentGatewaySettingsDto(
                enabled && hasApiKey,
                mode,
                hasApiKey,
                MaskSecret(apiKey),
                !string.IsNullOrWhiteSpace(webhookSecret),
                settings?.PayMongoPublicKey),
            settings?.CashPaymentEnabled ?? true,
            settings?.CashPaymentInstructions);
    }

    private static string MaskSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (value.Length <= 8) return "********";
        return $"{value[..4]}...{value[^4..]}";
    }
}
