namespace AgriCheck.Infrastructure.Helpers;

public static class DriverIncidentCategories
{
    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["accident"] = "Road accident / collision",
        ["flat_tire"] = "Flat tire",
        ["engine_problem"] = "Engine problem",
        ["mechanical_breakdown"] = "Mechanical breakdown",
        ["road_blocked"] = "Road blocked / obstruction",
        ["weather_hazard"] = "Weather hazard",
        ["cargo_shift"] = "Cargo shift / load issue",
        ["medical_emergency"] = "Medical emergency",
        ["traffic_enforcement"] = "Traffic enforcement / checkpoint delay",
        ["fuel_issue"] = "Fuel issue",
        ["other"] = "Other incident",
    };

    public static bool TryResolve(string? code, out string normalized, out string label)
    {
        normalized = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized) || !Labels.TryGetValue(normalized, out label!))
        {
            label = string.Empty;
            return false;
        }

        return true;
    }

    public static string FormatComplianceDetails(string categoryCode, string categoryLabel, string? notes)
    {
        var trimmedNotes = notes?.Trim();
        var baseText = $"category={categoryCode}|label={categoryLabel}";
        return string.IsNullOrWhiteSpace(trimmedNotes) ? baseText : $"{baseText}|notes={trimmedNotes}";
    }
}
