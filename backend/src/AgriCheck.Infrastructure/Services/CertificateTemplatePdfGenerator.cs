using System.Text.Json;
using System.Text.RegularExpressions;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgriCheck.Infrastructure.Services;

public static class CertificateTemplatePdfGenerator
{
    private const float MmToPt = 2.83465f;

    static CertificateTemplatePdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(
        CertificateTemplateVersion version,
        IReadOnlyList<CertificateElement> elements,
        IReadOnlyDictionary<string, object?> variables,
        string verifyUrl,
        string? qrBase64,
        string storageRoot)
    {
        var layout = ParseLayout(version.LayoutJson);
        var pageSize = layout.Orientation.Equals("landscape", StringComparison.OrdinalIgnoreCase)
            ? PageSizes.A4.Landscape()
            : PageSizes.A4;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(10));

                if (!string.IsNullOrWhiteSpace(layout.BackgroundColor))
                {
                    page.Background().Background(layout.BackgroundColor);
                }

                page.Content().Layers(layers =>
                {
                    layers.PrimaryLayer().Element(_ => { });

                    foreach (var item in elements
                                 .Select(e => (Element: e, Config: ParseConfig(e.ConfigJson)))
                                 .OrderBy(x => x.Config.ZIndex)
                                 .ThenBy(x => x.Element.SortOrder))
                    {
                        var element = item.Element;
                        var config = item.Config;
                        var width = config.Width ?? 120f;
                        var height = config.Height ?? 24f;

                        layers.Layer().Column(column =>
                        {
                            ApplyPosition(column.Item(), config, width, height)
                                .Element(layer => RenderElement(layer, element, config, variables, verifyUrl, qrBase64, storageRoot));
                        });
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void RenderElement(
        IContainer container,
        CertificateElement element,
        ElementConfig config,
        IReadOnlyDictionary<string, object?> variables,
        string verifyUrl,
        string? qrBase64,
        string storageRoot)
    {
        switch (element.ElementType)
        {
            case CertificateElementType.Text:
            case CertificateElementType.Field:
                RenderTextElement(container, element, config, variables);
                break;

            case CertificateElementType.QrCode:
                if (!string.IsNullOrWhiteSpace(qrBase64) &&
                    qrBase64.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase))
                {
                    var raw = qrBase64["data:image/png;base64,".Length..];
                    var bytes = Convert.FromBase64String(raw);
                    container.Image(bytes).FitArea();
                }
                else
                {
                    container.Text(verifyUrl).FontSize(8);
                }

                break;

            case CertificateElementType.Image:
                var imagePath = ResolveImagePath(config.ImagePath, storageRoot);
                if (imagePath is not null && File.Exists(imagePath))
                {
                    container.Image(imagePath).FitArea();
                }

                break;

            case CertificateElementType.Shape:
                container
                    .Image(SolidColorPngGenerator.Get(ResolveShapeFillColor(config)))
                    .FitUnproportionally();
                break;

            case CertificateElementType.Line:
            {
                var lineColor = config.BorderColor ?? "#000000";
                container
                    .Height(Math.Max(config.BorderWidth, 1))
                    .Image(SolidColorPngGenerator.Get(lineColor))
                    .FitUnproportionally();
                break;
            }
        }
    }

    private static IContainer ApplyPosition(IContainer container, ElementConfig config, float width, float height)
    {
        container = container.Unconstrained();

        if (config.Rotation != 0)
        {
            var bounds = RotatedBoundsCalculator.Compute(width, height, config.Rotation);
            var centerX = config.X + width / 2f;
            var centerY = config.Y + height / 2f;
            return container
                .TranslateX(centerX + bounds.OffsetXMm, Unit.Millimetre)
                .TranslateY(centerY + bounds.OffsetYMm, Unit.Millimetre)
                .Width(bounds.BboxWidthMm, Unit.Millimetre)
                .Height(bounds.BboxHeightMm, Unit.Millimetre);
        }

        return container
            .TranslateX(config.X, Unit.Millimetre)
            .TranslateY(config.Y, Unit.Millimetre)
            .Width(width, Unit.Millimetre)
            .Height(height, Unit.Millimetre);
    }

    private static void RenderTextElement(
        IContainer container,
        CertificateElement element,
        ElementConfig config,
        IReadOnlyDictionary<string, object?> variables)
    {
        var text = ReplaceVariables(config.Content ?? element.Label, variables);

        if (config.Rotation != 0)
        {
            var result = RotatedTextPngGenerator.Generate(
                text,
                config.Width ?? 120f,
                config.Height ?? 24f,
                config.FontSize ?? 12,
                config.Rotation,
                config.FontFamily,
                config.FontWeight,
                config.FontStyle,
                config.TextAlign,
                config.TextColor);
            container.Image(result.PngBytes).FitUnproportionally();
            return;
        }

        var aligned = ApplyTextAlign(container.AlignMiddle(), config.TextAlign);
        var textBlock = aligned.Text(text).FontSize(config.FontSize ?? 12);

        if (config.FontWeight?.Equals("bold", StringComparison.OrdinalIgnoreCase) == true)
        {
            textBlock.Bold();
        }

        if (config.FontStyle?.Equals("italic", StringComparison.OrdinalIgnoreCase) == true)
        {
            textBlock.Italic();
        }

        if (!string.IsNullOrWhiteSpace(config.FontFamily))
        {
            textBlock.FontFamily(MapFontFamily(config.FontFamily));
        }

        if (!string.IsNullOrWhiteSpace(config.TextColor) &&
            !config.TextColor.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            textBlock.FontColor(config.TextColor);
        }
    }

    private static string? ResolveImagePath(string? imagePath, string storageRoot)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        return Path.Combine(storageRoot, imagePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static ElementConfig ParseConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return new ElementConfig();
        }

        try
        {
            using var doc = JsonDocument.Parse(configJson);
            var root = doc.RootElement;
            return new ElementConfig(
                root.TryGetProperty("content", out var content) ? content.GetString() : null,
                ReadFloat(root, "x"),
                ReadFloat(root, "y"),
                ReadNullableFloat(root, "width"),
                ReadNullableFloat(root, "height"),
                ReadNullableNumber(root, "fontSize"),
                root.TryGetProperty("fontFamily", out var fontFamily) ? fontFamily.GetString() : null,
                root.TryGetProperty("fontWeight", out var fontWeight) ? fontWeight.GetString() : null,
                root.TryGetProperty("fontStyle", out var fontStyle) ? fontStyle.GetString() : null,
                root.TryGetProperty("textAlign", out var textAlign) ? textAlign.GetString() : null,
                ReadNumber(root, "rotation"),
                root.TryGetProperty("imagePath", out var imagePath) ? imagePath.GetString() : null,
                root.TryGetProperty("textColor", out var textColor) ? textColor.GetString() : null,
                root.TryGetProperty("backgroundColor", out var backgroundColor) ? backgroundColor.GetString() : null,
                ReadNumber(root, "borderWidth"),
                root.TryGetProperty("borderColor", out var borderColor) ? borderColor.GetString() : null,
                ReadNumber(root, "borderRadius"),
                ReadNumber(root, "zIndex", ReadNumber(root, "displayOrder")),
                root.TryGetProperty("qrLogoPath", out var qrLogoPath) ? qrLogoPath.GetString() : null,
                ReadNumber(root, "qrLogoSize", 20),
                root.TryGetProperty("qrForegroundColor", out var qrForegroundColor) ? qrForegroundColor.GetString() : null,
                root.TryGetProperty("qrBackgroundColor", out var qrBackgroundColor) ? qrBackgroundColor.GetString() : null,
                root.TryGetProperty("qrStyle", out var qrStyle) ? qrStyle.GetString() : null);
        }
        catch (JsonException)
        {
            return new ElementConfig();
        }
    }

    private static float ReadFloat(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return 0f;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => (float)value.GetDouble(),
            JsonValueKind.String when float.TryParse(value.GetString(), out var parsed) => parsed,
            _ => 0f,
        };
    }

    private static float? ReadNullableFloat(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => (float)value.GetDouble(),
            JsonValueKind.String when float.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null,
        };
    }

    private static int ReadNumber(JsonElement root, string property, int defaultValue = 0)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return defaultValue;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out var number) ? number : (int)value.GetDouble(),
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => defaultValue,
        };
    }

    private static int? ReadNullableNumber(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out var number) ? number : (int)value.GetDouble(),
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null,
        };
    }

    private static LayoutConfig ParseLayout(string? layoutJson)
    {
        if (string.IsNullOrWhiteSpace(layoutJson))
        {
            return new LayoutConfig("PORTRAIT", "#ffffff");
        }

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            var root = doc.RootElement;
            var orientation = root.TryGetProperty("orientation", out var o) ? o.GetString() ?? "PORTRAIT" : "PORTRAIT";
            var background = root.TryGetProperty("backgroundColor", out var bg) ? bg.GetString() : "#ffffff";
            return new LayoutConfig(orientation, background);
        }
        catch (JsonException)
        {
            return new LayoutConfig("PORTRAIT", "#ffffff");
        }
    }

    public static string ReplaceVariables(string content, IReadOnlyDictionary<string, object?> data)
    {
        return Regex.Replace(content, @"\{\{\s*([a-zA-Z0-9_.]+)\s*\}\}", match =>
        {
            var path = match.Groups[1].Value;
            var value = ResolvePath(data, path);
            return value ?? match.Value;
        });
    }

    private static string? ResolvePath(IReadOnlyDictionary<string, object?> data, string path)
    {
        var keys = path.Split('.');
        object? current = data;

        foreach (var key in keys)
        {
            if (current is IReadOnlyDictionary<string, object?> dict && dict.TryGetValue(key, out var next))
            {
                current = next;
                continue;
            }

            if (current is Dictionary<string, object?> mutable && mutable.TryGetValue(key, out var mutableNext))
            {
                current = mutableNext;
                continue;
            }

            return null;
        }

        return current switch
        {
            null => null,
            string s => s,
            _ => current.ToString()
        };
    }

    private static string ResolveShapeFillColor(ElementConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.BackgroundColor) &&
            !config.BackgroundColor.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            return config.BackgroundColor;
        }

        if (config.BorderWidth > 0 &&
            !string.IsNullOrWhiteSpace(config.BorderColor) &&
            !config.BorderColor.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            return config.BorderColor;
        }

        return "#d1d5db";
    }

    private static IContainer ApplyTextAlign(IContainer container, string? textAlign)
    {
        return textAlign?.ToLowerInvariant() switch
        {
            "center" => container.AlignCenter(),
            "right" => container.AlignRight(),
            "justify" => container.AlignLeft(),
            _ => container.AlignLeft(),
        };
    }

    private static string MapFontFamily(string fontFamily)
    {
        return fontFamily.ToLowerInvariant() switch
        {
            "times" => "Times New Roman",
            "courier" => "Courier New",
            "dejavusans" => "DejaVu Sans",
            _ => "Helvetica",
        };
    }

    private static string ToInvariant(this float value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string ToInvariant(this int value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private sealed record LayoutConfig(string Orientation, string? BackgroundColor);

    private sealed record ElementConfig(
        string? Content = null,
        float X = 0,
        float Y = 0,
        float? Width = null,
        float? Height = null,
        int? FontSize = null,
        string? FontFamily = null,
        string? FontWeight = null,
        string? FontStyle = null,
        string? TextAlign = null,
        int Rotation = 0,
        string? ImagePath = null,
        string? TextColor = null,
        string? BackgroundColor = null,
        int BorderWidth = 0,
        string? BorderColor = null,
        int BorderRadius = 0,
        int ZIndex = 0,
        string? QrLogoPath = null,
        int QrLogoSize = 20,
        string? QrForegroundColor = null,
        string? QrBackgroundColor = null,
        string? QrStyle = null);
}
