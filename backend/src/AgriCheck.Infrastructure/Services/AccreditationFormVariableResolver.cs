namespace AgriCheck.Infrastructure.Services;

internal static class AccreditationFormVariableResolver
{
    public static string ResolveTin(IReadOnlyDictionary<string, string> formData) =>
        GetFormValue(
            formData,
            "tin_number",
            "tin",
            "txt_tin",
            "company_tin",
            "tax_identification_number")
        ?? "N/A";

    public static string ResolveAddress(IReadOnlyDictionary<string, string> formData, string? profileAddress)
    {
        if (!string.IsNullOrWhiteSpace(profileAddress))
        {
            return profileAddress.Trim();
        }

        var singleLine = GetFormValue(
            formData,
            "business_address",
            "txt_business_address",
            "company_address",
            "address_line",
            "address");
        if (!string.IsNullOrWhiteSpace(singleLine))
        {
            return singleLine.Trim();
        }

        var parts = new[]
        {
            GetFormValue(formData, "address_street"),
            GetFormValue(formData, "address_barangay_name", "address_barangay"),
            GetFormValue(formData, "address_city_name", "address_city", "address_municipality_name"),
            GetFormValue(formData, "address_province_name", "address_province"),
            GetFormValue(formData, "address_region_name", "address_region"),
        }
        .Where(part => !string.IsNullOrWhiteSpace(part))
        .Select(part => part!.Trim())
        .ToList();

        var zipCode = GetFormValue(formData, "address_zip_code", "address_zip");
        if (!string.IsNullOrWhiteSpace(zipCode))
        {
            parts.Add(zipCode.Trim());
        }

        return parts.Count == 0 ? "N/A" : string.Join(", ", parts);
    }

    public static string ResolveNatureOfBusiness(IReadOnlyDictionary<string, string> formData, string? fallback = null) =>
        GetFormValue(
            formData,
            "nature_of_business",
            "txt_nature_of_business",
            "business_nature",
            "business_activity",
            "line_of_business",
            "business_description")
        ?? fallback
        ?? string.Empty;

    private static string? GetFormValue(IReadOnlyDictionary<string, string> formData, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (formData.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
