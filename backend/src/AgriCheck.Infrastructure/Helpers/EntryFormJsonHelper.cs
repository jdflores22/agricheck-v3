using System.Text.Json;

namespace AgriCheck.Infrastructure.Helpers;

internal static class EntryFormJsonHelper
{
    public static Dictionary<string, string> ParseFlatValues(string? formDataJson)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var doc = JsonDocument.Parse(formDataJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => prop.Value.GetRawText(),
                };
            }

            return dict;
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static string? FindValue(Dictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            {
                return v;
            }
        }

        return null;
    }

    public static string? FindSuffix(Dictionary<string, string> values, string suffix)
    {
        foreach (var (key, value) in values)
        {
            if (key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public static Guid? FindGuid(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Guid.TryParse(raw, out var guid) ? guid : null;
    }

    public static Guid? FindGuidSuffix(Dictionary<string, string> values, string suffix)
    {
        foreach (var (key, value) in values)
        {
            if (!key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Guid.TryParse(value, out var guid))
            {
                return guid;
            }
        }

        return null;
    }
}
