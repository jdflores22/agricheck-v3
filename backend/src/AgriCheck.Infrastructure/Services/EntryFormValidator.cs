using System.Text.Json;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public static class EntryFormValidator
{
    public static async Task ValidateDraftCompletenessAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken = default)
    {
        var template = await db.FormTemplates
            .AsNoTracking()
            .Include(t => t.Versions)
            .Include(t => t.AgencyTags)
            .Where(t => t.IsActive
                && t.FormType == "ENTRY"
                && t.Status == FormTemplateStatus.Published
                && t.AgencyTags.Any(tag => tag.AgencyId == entry.AgencyId))
            .OrderBy(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return;
        }

        var version = template.Versions
            .Where(v => v.IsPublished)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (version is null || string.IsNullOrWhiteSpace(version.SchemaJson))
        {
            return;
        }

        var formValues = ParseFormDataJson(entry.FormDataJson);
        var uploadedDocumentTypes = entry.Files
            .Where(file => !string.IsNullOrWhiteSpace(file.DocumentType))
            .Select(file => file.DocumentType!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(version.SchemaJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var missingLabels = new List<string>();
        foreach (var field in document.RootElement.EnumerateArray())
        {
            if (!field.TryGetProperty("required", out var requiredProp) || requiredProp.ValueKind != JsonValueKind.True)
            {
                continue;
            }

            if (!field.TryGetProperty("type", out var typeProp))
            {
                continue;
            }

            var fieldType = typeProp.GetString()?.Trim().ToLowerInvariant() ?? "text";
            if (fieldType is "section")
            {
                continue;
            }

            var label = field.TryGetProperty("label", out var labelProp) ? labelProp.GetString() : null;
            var name = field.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (fieldType is "file" or "geotag_photo")
            {
                var hasFile = uploadedDocumentTypes.Contains(name)
                    || (formValues.TryGetValue(name, out var fileName) && !string.IsNullOrWhiteSpace(fileName))
                    || (formValues.TryGetValue($"{name}_file_uuid", out var fileUuid) && !string.IsNullOrWhiteSpace(fileUuid));

                if (!hasFile)
                {
                    missingLabels.Add(label ?? name);
                }

                continue;
            }

            if (!formValues.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
            {
                missingLabels.Add(label ?? name);
            }
        }

        if (missingLabels.Count > 0)
        {
            throw new ClientPortalException(
                "ENTRY_INCOMPLETE",
                $"Complete all required fields and upload required documents before submitting. Missing: {string.Join(", ", missingLabels)}.");
        }
    }

    private static Dictionary<string, string> ParseFormDataJson(string? formDataJson)
    {
        if (string.IsNullOrWhiteSpace(formDataJson) || formDataJson.Trim() is "{}" or "null")
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(formDataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return document.RootElement.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                        JsonValueKind.Number => property.Value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => property.Value.GetRawText(),
                    },
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
