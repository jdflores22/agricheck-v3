namespace AgriCheck.Infrastructure.Helpers;

internal static class ContainerLocationTimestampHelper
{
    public static DateTime NormalizeRecordedAt(DateTime? recordedAt)
    {
        if (recordedAt is null)
        {
            return DateTime.UtcNow;
        }

        var utc = recordedAt.Value.Kind switch
        {
            DateTimeKind.Utc => recordedAt.Value,
            DateTimeKind.Local => recordedAt.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(recordedAt.Value, DateTimeKind.Utc)
        };

        var now = DateTime.UtcNow;
        if (utc > now.AddMinutes(10))
        {
            return now;
        }

        if (utc < now.AddDays(-30))
        {
            return now;
        }

        return utc;
    }
}
