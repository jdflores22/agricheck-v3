using AgriCheck.Domain.Entities;

namespace AgriCheck.Infrastructure.Helpers;

internal static class ContainerLocationTrailHelper
{
    /// <summary>Matches live map: no fix recorded for this long between points.</summary>
    public const int GpsSilentGapMinutes = 3;

    /// <summary>
    /// AgriTrack uploads about every 4 min when online; longer server insert delay implies buffered offline queue sync.
    /// </summary>
    public const int BufferedSyncDelayMinutes = 5;

    public static bool ResolveCapturedOffline(ContainerLocation location)
    {
        if (location.CapturedOffline)
        {
            return true;
        }

        var recorded = NormalizeUtc(location.RecordedAt);
        var synced = NormalizeUtc(location.CreatedAt);
        return (synced - recorded).TotalMinutes >= BufferedSyncDelayMinutes;
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
}
