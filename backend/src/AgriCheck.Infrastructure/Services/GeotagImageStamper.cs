using System.Globalization;
using SkiaSharp;

namespace AgriCheck.Infrastructure.Services;

public static class GeotagImageStamper
{
    private static readonly TimeZoneInfo PhilippinesTimeZone = ResolvePhilippinesTimeZone();

    public static byte[] StampJpeg(byte[] jpegBytes, decimal latitude, decimal longitude, DateTime capturedAtUtc)
    {
        using var bitmap = SKBitmap.Decode(jpegBytes)
            ?? throw new InvalidOperationException("Unable to decode geotag photo.");

        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
            ?? throw new InvalidOperationException("Unable to create image surface for geotag stamp.");

        var canvas = surface.Canvas;
        canvas.DrawBitmap(bitmap, 0, 0);

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(capturedAtUtc, DateTimeKind.Utc),
            PhilippinesTimeZone);
        var line1 = $"Lat {latitude:F6}  Lng {longitude:F6}";
        var line2 = localTime.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " (PH time)";
        var line3 = "AgriTrack geotag proof";

        var barHeight = bitmap.Height * 0.14f;
        using var barPaint = new SKPaint { Color = new SKColor(0, 0, 0, 190), IsAntialias = true };
        canvas.DrawRect(0, bitmap.Height - barHeight, bitmap.Width, barHeight, barPaint);

        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = Math.Max(22f, bitmap.Width * 0.028f),
            Typeface = SKTypeface.Default,
        };

        var y = bitmap.Height - barHeight + textPaint.TextSize + 8f;
        canvas.DrawText(line1, 16f, y, textPaint);
        y += textPaint.TextSize + 6f;
        canvas.DrawText(line2, 16f, y, textPaint);
        y += textPaint.TextSize + 4f;
        textPaint.TextSize *= 0.85f;
        canvas.DrawText(line3, 16f, y, textPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static TimeZoneInfo ResolvePhilippinesTimeZone()
    {
        foreach (var id in new[] { "Asia/Manila", "Singapore Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
