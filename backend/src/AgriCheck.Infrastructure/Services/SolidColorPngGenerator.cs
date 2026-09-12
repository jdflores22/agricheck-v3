using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;

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

        var r = Convert.ToByte(value[..2], 16);
        var g = Convert.ToByte(value.Substring(2, 2), 16);
        var b = Convert.ToByte(value.Substring(4, 2), 16);

        using var bitmap = new Bitmap(1, 1);
        bitmap.SetPixel(0, 0, Color.FromArgb(r, g, b));
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
