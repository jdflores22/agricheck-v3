namespace AgriCheck.Infrastructure.Helpers;

public static class RsasRenewalHelper
{
    /// <summary>DA MC 03 — annual renewal window April through June (UTC calendar).</summary>
    public static bool IsRenewalWindowOpen(DateTime utcNow)
    {
        var month = utcNow.Month;
        return month is >= 4 and <= 6;
    }

    public static bool IsRenewalDue(DateTime? expiresAt, DateTime utcNow, int withinDays = 120) =>
        expiresAt is not null && expiresAt.Value <= utcNow.AddDays(withinDays);
}
