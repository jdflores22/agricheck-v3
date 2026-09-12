using SkiaSharp;

namespace AgriCheck.Infrastructure.Services;

internal static class RotatedTextPngGenerator
{
    private const float MmToPx = 300f / 25.4f;

    public static RotatedTextRenderResult Generate(
        string text,
        float widthMm,
        float heightMm,
        int fontSizePt,
        int rotationDeg,
        string? fontFamily,
        string? fontWeight,
        string? fontStyle,
        string? textAlign,
        string? textColor)
    {
        var bounds = RotatedBoundsCalculator.Compute(widthMm, heightMm, rotationDeg);
        var widthPx = Math.Max(1, (int)Math.Ceiling(bounds.BboxWidthMm * MmToPx));
        var heightPx = Math.Max(1, (int)Math.Ceiling(bounds.BboxHeightMm * MmToPx));
        var elemWidthPx = widthMm * MmToPx;
        var elemHeightPx = heightMm * MmToPx;

        var info = new SKImageInfo(widthPx, heightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info)
            ?? throw new InvalidOperationException("Unable to create Skia surface for certificate text.");
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var bold = fontWeight?.Equals("bold", StringComparison.OrdinalIgnoreCase) == true;
        var italic = fontStyle?.Equals("italic", StringComparison.OrdinalIgnoreCase) == true;
        var typeface = ResolveTypeface(fontFamily, bold, italic);
        try
        {
            using var paint = new SKPaint
            {
                Color = ParseColor(textColor),
                IsAntialias = true,
                Typeface = typeface,
                TextSize = fontSizePt * 96f / 72f,
                TextAlign = MapAlignment(textAlign),
            };

            canvas.Translate(widthPx / 2f, heightPx / 2f);
            canvas.RotateDegrees(rotationDeg);
            canvas.Translate(-elemWidthPx / 2f, -elemHeightPx / 2f);
            canvas.ClipRect(new SKRect(0, 0, elemWidthPx, elemHeightPx));

            var metrics = paint.FontMetrics;
            var x = paint.TextAlign switch
            {
                SKTextAlign.Center => elemWidthPx / 2f,
                SKTextAlign.Right => elemWidthPx,
                _ => 0f,
            };
            var baseline = (elemHeightPx - metrics.Ascent - metrics.Descent) / 2f;
            canvas.DrawText(text ?? string.Empty, x, baseline, paint);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100)
                ?? throw new InvalidOperationException("Unable to encode rotated certificate text as PNG.");
            return new RotatedTextRenderResult(data.ToArray(), bounds);
        }
        finally
        {
            if (!ReferenceEquals(typeface, SKTypeface.Default))
            {
                typeface.Dispose();
            }
        }
    }

    private static SKTextAlign MapAlignment(string? textAlign) =>
        textAlign?.ToLowerInvariant() switch
        {
            "center" => SKTextAlign.Center,
            "right" => SKTextAlign.Right,
            _ => SKTextAlign.Left,
        };

    private static SKTypeface ResolveTypeface(string? fontFamily, bool bold, bool italic)
    {
        var weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        var slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
        var style = new SKFontStyle(weight, SKFontStyleWidth.Normal, slant);
        var candidates = (fontFamily ?? "helvetica").ToLowerInvariant() switch
        {
            "times" => new[] { "Times New Roman", "Liberation Serif", "DejaVu Serif" },
            "courier" => new[] { "Courier New", "Liberation Mono", "DejaVu Sans Mono" },
            "dejavusans" => new[] { "DejaVu Sans", "Liberation Sans", "Arial" },
            _ => new[] { "Arial", "Liberation Sans", "DejaVu Sans" },
        };

        foreach (var name in candidates)
        {
            var typeface = SKTypeface.FromFamilyName(name, style);
            if (typeface is not null
                && !ReferenceEquals(typeface, SKTypeface.Default)
                && typeface.GlyphCount > 0
                && name.Equals(typeface.FamilyName, StringComparison.OrdinalIgnoreCase))
            {
                return typeface;
            }

            typeface?.Dispose();
        }

        return SKTypeface.Default;
    }

    private static SKColor ParseColor(string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || hexColor.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            return SKColors.Black;
        }

        var value = hexColor.TrimStart('#');
        if (value.Length != 6
            || !byte.TryParse(value[..2], System.Globalization.NumberStyles.HexNumber, null, out var r)
            || !byte.TryParse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)
            || !byte.TryParse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return SKColors.Black;
        }

        return new SKColor(r, g, b);
    }
}

internal sealed record RotatedTextRenderResult(byte[] PngBytes, RotatedElementBounds Bounds);

internal static class RotatedBoundsCalculator
{
    public static RotatedElementBounds Compute(float widthMm, float heightMm, int rotationDeg)
    {
        var halfW = widthMm / 2f;
        var halfH = heightMm / 2f;
        var radians = rotationDeg * Math.PI / 180d;
        var cos = Math.Abs(Math.Cos(radians));
        var sin = Math.Abs(Math.Sin(radians));

        var bboxWidth = (float)(widthMm * cos + heightMm * sin);
        var bboxHeight = (float)(widthMm * sin + heightMm * cos);

        var corners = new[]
        {
            RotateCorner(-halfW, -halfH, radians),
            RotateCorner(halfW, -halfH, radians),
            RotateCorner(halfW, halfH, radians),
            RotateCorner(-halfW, halfH, radians),
        };

        var minX = corners.Min(c => c.X);
        var minY = corners.Min(c => c.Y);

        return new RotatedElementBounds(bboxWidth, bboxHeight, minX, minY);
    }

    private static (float X, float Y) RotateCorner(float x, float y, double radians)
    {
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return ((float)(x * cos + y * sin), (float)(-x * sin + y * cos));
    }
}

internal sealed record RotatedElementBounds(float BboxWidthMm, float BboxHeightMm, float OffsetXMm, float OffsetYMm);
