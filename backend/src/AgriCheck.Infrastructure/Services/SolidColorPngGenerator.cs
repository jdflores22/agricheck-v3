using System.Collections.Concurrent;
using SkiaSharp;

namespace AgriCheck.Infrastructure.Services;

internal static class SolidColorPngGenerator
{
    private static readonly ConcurrentDictionary<string, byte[]> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static byte[] Get(string hexColor)
    {
        var normalized = NormalizeHex(hexColor);
        return Cache.GetOrAdd(normalized, CreatePng);
    }

    private static string NormalizeHex(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || hexColor.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            return "#d1d5db";
        }

        return hexColor.StartsWith('#') ? hexColor : $"#{hexColor}";
    }

    private static byte[] CreatePng(string hexColor)
    {
        var value = hexColor.TrimStart('#');
        if (value.Length != 6)
        {
            value = "d1d5db";
        }

        if (!byte.TryParse(value[..2], System.Globalization.NumberStyles.HexNumber, null, out var r)
            || !byte.TryParse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)
            || !byte.TryParse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            r = 0xd1;
            g = 0xd5;
            b = 0xdb;
        }

        using var bitmap = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        bitmap.SetPixel(0, 0, new SKColor(r, g, b));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
