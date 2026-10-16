using System.Text;

namespace AgriCheck.Infrastructure.Services;

public static class GeotagStampHelper
{
    public const string ProofMarker = "AgriTrack geotag proof";
    private static readonly byte[] ProofMarkerBytes = Encoding.UTF8.GetBytes(ProofMarker);

    public static byte[] PrepareDriverGeotagJpeg(
        byte[] jpegBytes,
        decimal latitude,
        decimal longitude,
        DateTime capturedAtUtc)
    {
        if (ContainsProofMarker(jpegBytes))
        {
            return jpegBytes;
        }

        return GeotagImageStamper.StampJpeg(jpegBytes, latitude, longitude, capturedAtUtc);
    }

    public static bool ContainsProofMarker(byte[] jpegBytes)
    {
        if (jpegBytes.Length < ProofMarkerBytes.Length)
        {
            return false;
        }

        var start = Math.Max(0, jpegBytes.Length - 64_000);
        return IndexOf(jpegBytes, ProofMarkerBytes, start) >= 0;
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int start)
    {
        for (var i = start; i <= haystack.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }
}
