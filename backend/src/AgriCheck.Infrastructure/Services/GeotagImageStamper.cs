using System.Globalization;
using System.Reflection;
using SkiaSharp;

namespace AgriCheck.Infrastructure.Services;

public static class GeotagImageStamper
{
    private static readonly TimeZoneInfo PhilippinesTimeZone = ResolvePhilippinesTimeZone();
    private static readonly Lazy<SKBitmap?> BrandLogo = new(LoadBrandLogo);

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
        var line3 = GeotagStampHelper.ProofMarker;

        var barHeight = Math.Max(bitmap.Height * 0.12f, 96f);
        var logoColumnWidth = barHeight * 1.15f;
        var textLeft = logoColumnWidth + 14f;
        var barTop = bitmap.Height - barHeight;

        using var barPaint = new SKPaint { Color = new SKColor(0, 0, 0, 200), IsAntialias = true };
        canvas.DrawRect(0, barTop, bitmap.Width, barHeight, barPaint);

        DrawBrandLogoInBar(canvas, barTop, barHeight, logoColumnWidth);

        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = Math.Max(20f, bitmap.Width * 0.026f),
            Typeface = SKTypeface.Default,
        };

        var lineGap = textPaint.TextSize + 5f;
        var textBlockHeight = lineGap * 2 + textPaint.TextSize * 0.85f;
        var y = barTop + (barHeight - textBlockHeight) / 2f + textPaint.TextSize;
        canvas.DrawText(line1, textLeft, y, textPaint);
        y += lineGap;
        canvas.DrawText(line2, textLeft, y, textPaint);
        y += lineGap - 2f;
        textPaint.TextSize *= 0.85f;
        canvas.DrawText(line3, textLeft, y, textPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static void DrawBrandLogoInBar(SKCanvas canvas, float barTop, float barHeight, float logoColumnWidth)
    {
        var logo = BrandLogo.Value;
        if (logo is null)
        {
            return;
        }

        var maxLogoHeight = barHeight * 0.78f;
        var scale = maxLogoHeight / logo.Height;
        var drawWidth = logo.Width * scale;
        var drawHeight = logo.Height * scale;
        var left = (logoColumnWidth - drawWidth) / 2f;
        var top = barTop + (barHeight - drawHeight) / 2f;
        canvas.DrawBitmap(logo, SKRect.Create(left, top, drawWidth, drawHeight));
    }

    private static SKBitmap? LoadBrandLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("agritrack_logo.png", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        return stream is null ? null : SKBitmap.Decode(stream);
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
