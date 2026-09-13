using System.Text.Json;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.Infrastructure.Helpers;

internal static class EntryEvaluationValidator
{
    public static async Task EnsureReadyForOutcomeAsync(
        AgriCheckDbContext db,
        Entry entry,
        long evaluatorUserId,
        CancellationToken cancellationToken)
    {
        var pendingCompliance = await db.EntryComplianceResults
            .AnyAsync(r => r.EntryId == entry.Id && r.Status == ComplianceResultStatus.Pending, cancellationToken);

        if (pendingCompliance)
        {
            throw new ClientPortalException(
                "COMPLIANCE_INCOMPLETE",
                "Complete all compliance checklist items before submitting the entry outcome.");
        }

        var files = await db.EntryFiles
            .Include(f => f.Evaluations)
            .Where(f => f.EntryId == entry.Id)
            .ToListAsync(cancellationToken);

        var requiredFileUuids = await GetRequiredDocumentFileUuidsAsync(db, entry, cancellationToken);
        if (requiredFileUuids.Count == 0)
        {
            throw new ClientPortalException(
                "NO_REQUIRED_DOCUMENTS",
                "No required documents were found for this entry.");
        }

        foreach (var fileUuid in requiredFileUuids)
        {
            var file = files.FirstOrDefault(f => f.Uuid == fileUuid)
                ?? throw new ClientPortalException(
                    "DOCUMENT_NOT_FOUND",
                    "A required document is missing from this entry.");

            var evaluation = file.Evaluations.FirstOrDefault(e => e.EvaluatorUserId == evaluatorUserId);
            if (evaluation is null || evaluation.Decision == EvaluationDecision.Pending)
            {
                throw new ClientPortalException(
                    "DOCUMENTS_NOT_EVALUATED",
                    "Review all required documents before submitting the entry outcome.");
            }
        }
    }

    private static async Task<IReadOnlyList<Guid>> GetRequiredDocumentFileUuidsAsync(
        AgriCheckDbContext db,
        Entry entry,
        CancellationToken cancellationToken)
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
            return Array.Empty<Guid>();
        }

        var version = template.Versions
            .Where(v => v.IsPublished)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (version is null || string.IsNullOrWhiteSpace(version.SchemaJson))
        {
            return Array.Empty<Guid>();
        }

        var formValues = ParseFormDataJson(entry.FormDataJson);
        var fileUuids = new List<Guid>();

        using var document = JsonDocument.Parse(version.SchemaJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return fileUuids;
        }

        foreach (var field in document.RootElement.EnumerateArray())
        {
            if (!field.TryGetProperty("type", out var typeProp))
            {
                continue;
            }

            var fieldType = typeProp.GetString()?.Trim().ToLowerInvariant() ?? "text";
            if (fieldType is not ("file" or "geotag_photo"))
            {
                continue;
            }

            if (!field.TryGetProperty("name", out var nameProp))
            {
                continue;
            }

            var name = nameProp.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!formValues.TryGetValue(name, out var fileName) || string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            if (!formValues.TryGetValue($"{name}_file_uuid", out var fileUuidValue)
                || !Guid.TryParse(fileUuidValue, out var fileUuid))
            {
                continue;
            }

            fileUuids.Add(fileUuid);
        }

        return fileUuids;
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
