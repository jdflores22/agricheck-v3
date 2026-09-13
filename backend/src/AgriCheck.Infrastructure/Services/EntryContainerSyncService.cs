using System.Text.Json;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Services;

public static class EntryContainerSyncService
{
    private static readonly string[] ContainerNumberFieldNames = new[]
    {
        "container_number",
        "containerNumber",
        "txt_container_number",
        "text_container_number",
    };

    private static readonly string[] ContainerTypeFieldNames = new[]
    {
        "select_container_type",
        "container_type",
        "containerType",
    };

    public static async Task SyncAsync(
        AgriCheckDbContext db,
        Entry entry,
        int? numContainers,
        string? containersJson,
        CancellationToken cancellationToken = default)
    {
        if (numContainers is null && string.IsNullOrWhiteSpace(containersJson))
        {
            return;
        }

        var count = Math.Clamp(numContainers ?? 0, 0, 50);
        var payload = ParseContainersJson(containersJson);

        if (count == 0)
        {
            await RemoveAllContainersAsync(db, entry, cancellationToken);
            return;
        }

        if (payload.Count != count)
        {
            throw new ClientPortalException(
                "INVALID_CONTAINERS",
                $"Container count mismatch. Expected {count} container record(s).");
        }

        var existing = await db.Containers
            .Where(c => c.EntryId == entry.Id)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync(cancellationToken);

        for (var index = 0; index < count; index++)
        {
            var sequenceNumber = index + 1;
            var formData = payload[index];
            var serialized = JsonSerializer.Serialize(formData);
            var containerNumber = ResolveContainerNumber(formData)
                ?? await ReferenceNumberGenerator.ContainerAsync(db, cancellationToken);
            var containerType = ResolveContainerType(formData);

            var container = existing.FirstOrDefault(c => c.SequenceNumber == sequenceNumber);
            if (container is null)
            {
                container = new Container
                {
                    Uuid = Guid.NewGuid(),
                    EntryId = entry.Id,
                    SequenceNumber = sequenceNumber,
                    ContainerNumber = containerNumber,
                    ContainerType = containerType,
                    FormDataJson = serialized,
                    Status = ContainerStatus.Pending,
                };
                db.Containers.Add(container);
                continue;
            }

            container.FormDataJson = serialized;
            container.SequenceNumber = sequenceNumber;
            container.ContainerType = containerType;
            if (ResolveContainerNumber(formData) is { } updatedNumber)
            {
                container.ContainerNumber = updatedNumber;
            }
            else if (string.IsNullOrWhiteSpace(container.ContainerNumber))
            {
                container.ContainerNumber = containerNumber;
            }
        }

        var stale = existing.Where(c => c.SequenceNumber > count).ToList();
        if (stale.Count > 0)
        {
            db.Containers.RemoveRange(stale);
        }
    }

    public static async Task ValidateOnSubmitAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken = default)
    {
        var formValues = ParseFormDataObject(entry.FormDataJson);
        var numContainers = ResolveNumContainers(formValues);
        var templateSchema = await LoadContainerTemplateSchemaAsync(db, entry.AgencyId, cancellationToken);
        var containers = await db.Containers
            .Where(c => c.EntryId == entry.Id)
            .OrderBy(c => c.SequenceNumber)
            .ToListAsync(cancellationToken);

        var requiresContainers = templateSchema is not null || numContainers > 0;
        if (!requiresContainers)
        {
            return;
        }

        if (numContainers <= 0 || containers.Count == 0)
        {
            throw new ClientPortalException(
                "CONTAINERS_REQUIRED",
                "Add container details before submitting this entry.");
        }

        if (containers.Count != numContainers)
        {
            throw new ClientPortalException(
                "CONTAINERS_INCOMPLETE",
                $"Provide details for all {numContainers} container(s) before submitting.");
        }

        if (templateSchema is null)
        {
            return;
        }

        var missingLabels = new List<string>();
        var containerNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var container in containers)
        {
            var values = ParseFormDataObject(container.FormDataJson);
            missingLabels.AddRange(GetMissingRequiredLabels(templateSchema, values, container.SequenceNumber));

            var containerNumber = ResolveContainerNumber(values);
            if (!string.IsNullOrWhiteSpace(containerNumber))
            {
                if (!containerNumbers.Add(containerNumber.Trim()))
                {
                    throw new ClientPortalException(
                        "DUPLICATE_CONTAINER_NUMBER",
                        $"Duplicate container number '{containerNumber}' found across containers.");
                }
            }
        }

        if (missingLabels.Count > 0)
        {
            throw new ClientPortalException(
                "CONTAINERS_INCOMPLETE",
                $"Complete all required container fields before submitting. Missing: {string.Join(", ", missingLabels)}.");
        }
    }

    private static async Task RemoveAllContainersAsync(AgriCheckDbContext db, Entry entry, CancellationToken cancellationToken)
    {
        var existing = await db.Containers.Where(c => c.EntryId == entry.Id).ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            db.Containers.RemoveRange(existing);
        }
    }

    private static async Task<string?> LoadContainerTemplateSchemaAsync(
        AgriCheckDbContext db,
        long agencyId,
        CancellationToken cancellationToken)
    {
        var template = await db.FormTemplates
            .AsNoTracking()
            .Include(t => t.Versions)
            .Include(t => t.AgencyTags)
            .Where(t => t.IsActive
                && t.FormType == "CONTAINER"
                && t.Status == FormTemplateStatus.Published
                && t.AgencyTags.Any(tag => tag.AgencyId == agencyId))
            .OrderBy(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return null;
        }

        var version = template.Versions
            .Where(v => v.IsPublished)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (version is null || string.IsNullOrWhiteSpace(version.SchemaJson))
        {
            return null;
        }

        return version.SchemaJson;
    }

    private static List<Dictionary<string, string>> ParseContainersJson(string? containersJson)
    {
        if (string.IsNullOrWhiteSpace(containersJson) || containersJson.Trim() is "[]" or "null")
        {
            return new List<Dictionary<string, string>>();
        }

        try
        {
            using var document = JsonDocument.Parse(containersJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new List<Dictionary<string, string>>();
            }

            var items = new List<Dictionary<string, string>>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    items.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                    continue;
                }

                items.Add(element.EnumerateObject().ToDictionary(
                    property => property.Name,
                    property => property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                        JsonValueKind.Number => property.Value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => property.Value.GetRawText(),
                    },
                    StringComparer.OrdinalIgnoreCase));
            }

            return items;
        }
        catch (JsonException ex)
        {
            throw new ClientPortalException("INVALID_CONTAINERS", $"Invalid container payload: {ex.Message}");
        }
    }

    private static Dictionary<string, string> ParseFormDataObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() is "{}" or "null")
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return document.RootElement.EnumerateObject().ToDictionary(
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

    private static int ResolveNumContainers(Dictionary<string, string> formValues)
    {
        if (formValues.TryGetValue("num_containers", out var raw) && int.TryParse(raw, out var parsed))
        {
            return Math.Clamp(parsed, 0, 50);
        }

        return 0;
    }

    private static string? ResolveContainerNumber(Dictionary<string, string> values)
    {
        foreach (var key in ContainerNumberFieldNames)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? ResolveContainerType(Dictionary<string, string> values)
    {
        foreach (var key in ContainerTypeFieldNames)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static IEnumerable<string> GetMissingRequiredLabels(
        string schemaJson,
        Dictionary<string, string> values,
        int sequenceNumber)
    {
        using var document = JsonDocument.Parse(schemaJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

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
            if (fieldType is "section" or "file" or "geotag_photo")
            {
                continue;
            }

            var label = field.TryGetProperty("label", out var labelProp) ? labelProp.GetString() : null;
            var name = field.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
            {
                yield return $"Container {sequenceNumber}: {label ?? name}";
            }
        }
    }
}
