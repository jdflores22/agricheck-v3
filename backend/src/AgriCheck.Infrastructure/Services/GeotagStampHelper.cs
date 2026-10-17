namespace AgriCheck.Infrastructure.Services;

/// <summary>
/// Driver geotag JPEGs are stamped on the AgriTrack mobile app before upload.
/// The API stores the uploaded bytes as-is (no server-side overlay).
/// </summary>
public static class GeotagStampHelper
{
    public const string ProofMarker = "AgriTrack geotag proof";

    public static byte[] PrepareDriverGeotagJpeg(
        byte[] jpegBytes,
        decimal latitude,
        decimal longitude,
        DateTime capturedAtUtc) => jpegBytes;
}
