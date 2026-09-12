using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Infrastructure.Services;

internal static class AgencyBillingStatusMapper
{
    public static string GetClientDisplayStatus(AgencyBilling billing) => billing.Status switch
    {
        AgencyBillingStatus.Draft => "Draft",
        AgencyBillingStatus.Issued => "Awaiting Payment Proof",
        AgencyBillingStatus.PaymentPending when billing.PaymentUploadedAt.HasValue => "Payment Submitted",
        AgencyBillingStatus.PaymentPending => "Payment Pending",
        AgencyBillingStatus.Paid => "Payment Verified",
        AgencyBillingStatus.Cancelled => "Cancelled",
        _ => billing.Status.ToString()
    };

    public static string GetAgencyDisplayStatus(AgencyBilling billing) => billing.Status switch
    {
        AgencyBillingStatus.Draft => "Draft",
        AgencyBillingStatus.Issued => "Issued",
        AgencyBillingStatus.PaymentPending when billing.PaymentUploadedAt.HasValue => "Pending Verification",
        AgencyBillingStatus.PaymentPending => "Payment Pending",
        AgencyBillingStatus.Paid => "Paid",
        AgencyBillingStatus.Cancelled => "Cancelled",
        _ => billing.Status.ToString()
    };
}
