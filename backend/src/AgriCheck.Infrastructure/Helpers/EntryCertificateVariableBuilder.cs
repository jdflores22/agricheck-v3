using System.Text.Json;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;

namespace AgriCheck.Infrastructure.Helpers;

public static class EntryCertificateVariableBuilder
{
    public static Dictionary<string, object?> Build(
        Entry entry,
        User entryUser,
        string certificateNumber,
        DateTime issuedAt,
        DateTime? expiresAt,
        User? officer = null,
        AgencyBilling? billing = null)
    {
        var formData = ParseFormData(entry.FormDataJson);
        var profile = entryUser.Profile;

        var companyName = !string.IsNullOrWhiteSpace(profile?.CompanyName)
            ? profile.CompanyName
            : profile is not null
                ? $"{profile.FirstName} {profile.LastName}".Trim()
                : entryUser.Email;

        var commodity = entry.Detail?.CommodityName
            ?? GetFormValue(formData, "commodity", "txt_commodity", "commodity_name")
            ?? "N/A";

        var quantity = entry.Detail is not null
            ? entry.Detail.Quantity.ToString("0.##")
            : GetFormValue(formData, "volume", "txt_volume_weight", "quantity") ?? "N/A";

        var unit = entry.Detail?.Unit
            ?? GetFormValue(formData, "unit", "txt_unit") ?? "kg";

        var hsCode = GetFormValue(formData, "hs_code", "txt_hs_code") ?? "N/A";
        var entryTypeLabel = entry.EntryType == EntryType.Export ? "Export" : "Import";

        var officerName = officer?.Profile is not null
            ? $"{officer.Profile.FirstName} {officer.Profile.LastName}".Trim()
            : billing?.VerifiedBy?.Profile is not null
                ? $"{billing.VerifiedBy.Profile.FirstName} {billing.VerifiedBy.Profile.LastName}".Trim()
                : "Authorized Officer";

        return new Dictionary<string, object?>
        {
            ["certificate"] = new Dictionary<string, object?>
            {
                ["number"] = certificateNumber,
                ["issued_at"] = issuedAt.ToString("yyyy-MM-dd"),
                ["expires_at"] = expiresAt?.ToString("yyyy-MM-dd"),
            },
            ["company"] = new Dictionary<string, object?>
            {
                ["name"] = companyName,
                ["address"] = profile?.Address ?? GetFormValue(formData, "address", "txt_address") ?? "N/A",
                ["tin"] = GetFormValue(formData, "tin", "txt_tin") ?? "N/A",
                ["registration_number"] = GetFormValue(formData, "registration_number", "txt_registration_number") ?? "N/A",
                ["business_type"] = !string.IsNullOrWhiteSpace(profile?.CompanyName) ? "Business" : "Individual",
            },
            ["entry"] = new Dictionary<string, object?>
            {
                ["reference"] = entry.ReferenceNo,
                ["number"] = entry.ReferenceNo,
                ["type"] = entryTypeLabel,
                ["commodity"] = commodity,
                ["quantity"] = quantity,
                ["unit"] = unit,
                ["hs_code"] = hsCode,
                ["submitted_at"] = (entry.SubmittedAt ?? entry.CreatedAt).ToString("MMMM dd, yyyy"),
            },
            ["agency"] = new Dictionary<string, object?>
            {
                ["name"] = entry.Agency?.Name ?? "N/A",
                ["code"] = entry.Agency?.Code ?? "N/A",
            },
            ["billing"] = billing is null
                ? null
                : new Dictionary<string, object?>
                {
                    ["number"] = billing.BillNumber,
                    ["total_amount"] = billing.Amount.ToString("N2"),
                    ["paid_at"] = billing.PaidAt?.ToString("MMMM dd, yyyy"),
                },
            ["officer"] = new Dictionary<string, object?>
            {
                ["name"] = officerName,
                ["title"] = "Authorized Officer",
            },
            ["date"] = new Dictionary<string, object?>
            {
                ["issued"] = issuedAt.ToString("MMMM dd, yyyy"),
                ["expires"] = expiresAt?.ToString("MMMM dd, yyyy"),
            },
        };
    }

    public static Dictionary<string, object?> BuildPreviewVariables() => new()
    {
        ["certificate"] = new Dictionary<string, object?>
        {
            ["number"] = "CERT-ENT-2026-00001",
            ["issued_at"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["expires_at"] = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
        },
        ["company"] = new Dictionary<string, object?>
        {
            ["name"] = "Sample Agriculture Corporation",
            ["address"] = "123 Farm Road, Agricultural District, Sample City",
            ["tin"] = "123-456-789-000",
            ["registration_number"] = "REG-SAMPLE-12345",
            ["business_type"] = "Corporation",
        },
        ["entry"] = new Dictionary<string, object?>
        {
            ["reference"] = "ENTRY-2026-00045",
            ["number"] = "ENTRY-2026-00045",
            ["type"] = "Import",
            ["commodity"] = "Frozen Buffalo Meat",
            ["quantity"] = "10,000",
            ["unit"] = "kg",
            ["hs_code"] = "0201.30.00",
            ["submitted_at"] = DateTime.UtcNow.AddDays(-7).ToString("MMMM dd, yyyy"),
        },
        ["agency"] = new Dictionary<string, object?>
        {
            ["name"] = "Bureau of Animal Industry",
            ["code"] = "BAI",
        },
        ["billing"] = new Dictionary<string, object?>
        {
            ["number"] = "BAI-BILL-2026-0001",
            ["total_amount"] = "1,900.00",
            ["paid_at"] = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
        },
        ["officer"] = new Dictionary<string, object?>
        {
            ["name"] = "Juan Dela Cruz",
            ["title"] = "Authorized Officer",
        },
        ["date"] = new Dictionary<string, object?>
        {
            ["issued"] = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
            ["expires"] = DateTime.UtcNow.AddYears(1).ToString("MMMM dd, yyyy"),
        },
    };

    private static Dictionary<string, string> ParseFormData(string? formDataJson)
    {
        if (string.IsNullOrWhiteSpace(formDataJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(formDataJson)
                ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

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
