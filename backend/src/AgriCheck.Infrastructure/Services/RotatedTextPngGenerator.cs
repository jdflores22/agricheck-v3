using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

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

        using var bitmap = new Bitmap(widthPx, heightPx, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        var style = FontStyle.Regular;
        if (fontWeight?.Equals("bold", StringComparison.OrdinalIgnoreCase) == true)
        {
            style |= FontStyle.Bold;
        }

        if (fontStyle?.Equals("italic", StringComparison.OrdinalIgnoreCase) == true)
        {
            style |= FontStyle.Italic;
        }

        using var font = new Font(MapGdiFont(fontFamily), fontSizePt, style, GraphicsUnit.Point);
        using var brush = new SolidBrush(ParseColor(textColor));

        graphics.TranslateTransform(widthPx / 2f, heightPx / 2f);
        graphics.RotateTransform(rotationDeg);
        graphics.TranslateTransform(-elemWidthPx / 2f, -elemHeightPx / 2f);

        using var format = new StringFormat
        {
            Alignment = MapAlignment(textAlign),
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.None,
            FormatFlags = StringFormatFlags.NoWrap,
        };

        graphics.DrawString(text, font, brush, new RectangleF(0, 0, elemWidthPx, elemHeightPx), format);

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return new RotatedTextRenderResult(stream.ToArray(), bounds);
    }

    private static StringAlignment MapAlignment(string? textAlign) =>
        textAlign?.ToLowerInvariant() switch
        {
            "center" => StringAlignment.Center,
            "right" => StringAlignment.Far,
            _ => StringAlignment.Near,
        };

    private static string MapGdiFont(string? fontFamily) =>
        (fontFamily ?? "helvetica").ToLowerInvariant() switch
        {
            "times" => "Times New Roman",
            "courier" => "Courier New",
            "dejavusans" => "DejaVu Sans",
            _ => "Arial",
        };

    private static Color ParseColor(string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || hexColor.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Black;
        }

        var value = hexColor.TrimStart('#');
        if (value.Length != 6)
        {
            return Color.Black;
        }

        var r = Convert.ToByte(value[..2], 16);
        var g = Convert.ToByte(value.Substring(2, 2), 16);
        var b = Convert.ToByte(value.Substring(4, 2), 16);
        return Color.FromArgb(r, g, b);
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
