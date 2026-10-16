using AgriCheck.Domain.Entities;

namespace AgriCheck.Infrastructure.Helpers;

internal static class ContainerLocationTrailHelper
{
    /// <summary>Matches live map: no fix recorded for this long between points.</summary>
    public const int GpsSilentGapMinutes = 3;

    /// <summary>
    /// AgriTrack targets ~4 min server upload when online; anything clearly above that is a buffered offline fix.
    /// </summary>
    public const int BufferedSyncDelaySeconds = 4 * 60 + 30;

    public static int? ResolveSyncDelaySeconds(ContainerLocation location)
    {
        var recorded = NormalizeUtc(location.RecordedAt);
        var synced = NormalizeUtc(location.CreatedAt);
        var seconds = (int)Math.Round((synced - recorded).TotalSeconds);
        return seconds < 0 ? 0 : seconds;
    }

    public static bool ResolveCapturedOffline(ContainerLocation location)
    {
        if (location.CapturedOffline)
        {
            return true;
        }

        var delay = ResolveSyncDelaySeconds(location);
        return delay is >= BufferedSyncDelaySeconds;
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
}
