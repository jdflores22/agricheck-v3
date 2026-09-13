using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgriCheck.Application.AdminPortal;
using AgriCheck.Application.AdminPortal.Dtos;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Helpers;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AgriCheck.Infrastructure.Services;

public class FormBuilderService : IFormBuilderService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FormBuilderService(AgriCheckDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FormTemplateListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var templates = await _db.FormTemplates
            .Include(t => t.Versions)
            .Include(t => t.AgencyTags)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);

        return templates.Select(MapListItem).ToList();
    }

    public async Task<FormTemplateDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var template = await _db.FormTemplates
            .Include(t => t.Versions)
            .Include(t => t.AgencyTags)
            .FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken);
        return template is null ? null : Map(template);
    }

    public async Task<FormTemplateDetailDto> SaveAsync(Guid? uuid, SaveFormTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        FormTemplate template;
        if (uuid is null)
        {
            template = new FormTemplate
            {
                Uuid = Guid.NewGuid(),
                Name = request.Name.Trim(),
                FormType = request.FormType.Trim().ToUpperInvariant(),
                Status = request.Publish ? FormTemplateStatus.Published : FormTemplateStatus.Draft
            };
            _db.FormTemplates.Add(template);
        }
        else
        {
            template = await _db.FormTemplates.Include(t => t.Versions).Include(t => t.AgencyTags)
                .FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
                ?? throw new ClientPortalException("NOT_FOUND", "Form template not found.");
            template.Name = request.Name.Trim();
            template.FormType = request.FormType.Trim().ToUpperInvariant();
            template.Status = request.Publish ? FormTemplateStatus.Published : FormTemplateStatus.Draft;
            template.AgencyTags.Clear();
        }

        var nextVersion = template.Versions.Any() ? template.Versions.Max(v => v.VersionNumber) + 1 : 1;
        template.Versions.Add(new FormTemplateVersion
        {
            VersionNumber = nextVersion,
            SchemaJson = request.SchemaJson,
            IsPublished = request.Publish
        });

        foreach (var agencyId in request.AgencyIds.Distinct())
        {
            template.AgencyTags.Add(new FormAgencyTag { AgencyId = agencyId });
        }

        AuditLogHelper.Write(_db, admin.Id, "form_template_saved", "form_template", template.Uuid.ToString());
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await _db.FormTemplates.Include(t => t.Versions).Include(t => t.AgencyTags).FirstAsync(t => t.Id == template.Id, cancellationToken));
    }

    public async Task<FormTemplateDetailDto> SetActiveAsync(Guid uuid, bool isActive, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var template = await _db.FormTemplates
            .Include(t => t.Versions)
            .Include(t => t.AgencyTags)
            .FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Form template not found.");

        if (isActive)
        {
            var latest = template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()
                ?? throw new ClientPortalException("EMPTY_FORM", "Add fields and save the template before activating it.");

            if (CountFields(latest.SchemaJson) == 0)
            {
                throw new ClientPortalException("EMPTY_FORM", "Add fields and save the template before activating it.");
            }

            if (!template.Versions.Any(v => v.IsPublished))
            {
                latest.IsPublished = true;
            }

            template.Status = FormTemplateStatus.Published;
            template.IsActive = true;
        }
        else
        {
            template.IsActive = false;
        }

        AuditLogHelper.Write(
            _db,
            admin.Id,
            isActive ? "form_template_activated" : "form_template_deactivated",
            "form_template",
            template.Uuid.ToString());
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await _db.FormTemplates.Include(t => t.Versions).Include(t => t.AgencyTags).FirstAsync(t => t.Id == template.Id, cancellationToken));
    }

    public async Task<FormTemplateDetailDto> CloneAsync(Guid uuid, CloneFormTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var source = await GetAsync(uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Form template not found.");

        var name = string.IsNullOrWhiteSpace(request.Name) ? $"{source.Name} (Copy)" : request.Name.Trim();
        var cloned = await SaveAsync(null, new SaveFormTemplateRequest(
            name,
            source.FormType,
            source.SchemaJson,
            source.AgencyIds,
            false), cancellationToken);

        var entity = await _db.FormTemplates.FirstAsync(t => t.Uuid == cloned.Uuid, cancellationToken);
        entity.IsActive = false;
        entity.Status = FormTemplateStatus.Draft;
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(cloned.Uuid, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var template = await _db.FormTemplates
            .Include(t => t.Versions)
            .Include(t => t.AgencyTags)
            .FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Form template not found.");

        if (template.IsActive && template.Versions.Any(v => v.IsPublished))
        {
            throw new ClientPortalException(
                "CANNOT_DELETE_LIVE",
                "Cannot delete a live form template. Deactivate or unpublish it first.");
        }

        _db.FormTemplates.Remove(template);
        AuditLogHelper.Write(_db, admin.Id, "form_template_deleted", "form_template", template.Uuid.ToString());
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> ExportAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var template = await _db.FormTemplates
            .Include(t => t.Versions)
            .Include(t => t.AgencyTags)
            .FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Form template not found.");

        var version = template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        var schema = string.IsNullOrWhiteSpace(version?.SchemaJson) ? "[]" : version!.SchemaJson;
        using var schemaDoc = JsonDocument.Parse(schema);

        var export = new JsonObject
        {
            ["format"] = "agricheck-form-template",
            ["formatVersion"] = "3.0",
            ["exportedAt"] = DateTime.UtcNow,
            ["name"] = template.Name,
            ["formType"] = template.FormType,
            ["status"] = template.Status.ToString(),
            ["isActive"] = template.IsActive,
            ["agencyIds"] = new JsonArray(template.AgencyTags.Select(tag => JsonValue.Create(tag.AgencyId)).ToArray()),
            ["schema"] = JsonNode.Parse(schemaDoc.RootElement.GetRawText())
        };

        return export.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<FormTemplateDetailDto> ImportAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);

        string name;
        string formType;
        string schemaJson;
        IReadOnlyList<long> agencyIds;

        if (payload.TryGetProperty("format", out var formatProp)
            && formatProp.GetString() == "agricheck-form-template")
        {
            name = payload.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Imported Template" : "Imported Template";
            formType = payload.TryGetProperty("formType", out var typeProp) ? typeProp.GetString() ?? "ENTRY" : "ENTRY";
            schemaJson = payload.TryGetProperty("schema", out var schemaProp)
                ? schemaProp.GetRawText()
                : "[]";
            agencyIds = payload.TryGetProperty("agencyIds", out var agencyProp)
                ? agencyProp.EnumerateArray().Select(item => item.GetInt64()).Distinct().ToList()
                : Array.Empty<long>();
        }
        else if (payload.TryGetProperty("template", out var templateProp) && payload.TryGetProperty("fields", out var fieldsProp))
        {
            name = templateProp.TryGetProperty("name", out var v2Name) ? v2Name.GetString() ?? "Imported Template" : "Imported Template";
            formType = ResolveV2FormType(templateProp);
            schemaJson = V2FormSchemaConverter.ConvertFieldsToSchemaJson(fieldsProp);
            agencyIds = await ResolveV2AgencyIdsAsync(payload, cancellationToken);
        }
        else
        {
            throw new ClientPortalException("INVALID_IMPORT", "Unrecognized import format. Upload a V3 export file or compatible V2 JSON export.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ClientPortalException("INVALID_IMPORT", "Template name is required.");
        }

        ValidateSchemaJson(schemaJson);

        var imported = await SaveAsync(null, new SaveFormTemplateRequest(
            name.Trim(),
            formType.Trim().ToUpperInvariant(),
            schemaJson,
            agencyIds,
            false), cancellationToken);

        var entity = await _db.FormTemplates.FirstAsync(t => t.Uuid == imported.Uuid, cancellationToken);
        entity.IsActive = false;
        entity.Status = FormTemplateStatus.Draft;
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(imported.Uuid, cancellationToken))!;
    }

    private async Task<IReadOnlyList<long>> ResolveV2AgencyIdsAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        if (!payload.TryGetProperty("agencies", out var agenciesProp))
        {
            return Array.Empty<long>();
        }

        var codes = agenciesProp.EnumerateArray()
            .Select(item => item.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (codes.Count == 0)
        {
            return Array.Empty<long>();
        }

        return await _db.Agencies
            .Where(agency => codes.Contains(agency.Code))
            .Select(agency => agency.Id)
            .ToListAsync(cancellationToken);
    }

    private static string ResolveV2FormType(JsonElement templateProp)
    {
        if (templateProp.TryGetProperty("form_type", out var snakeCase))
        {
            return snakeCase.GetString() ?? "ACCREDITATION";
        }

        if (templateProp.TryGetProperty("formType", out var camelCase))
        {
            return camelCase.GetString() ?? "ACCREDITATION";
        }

        return "ACCREDITATION";
    }

    private static void ValidateSchemaJson(string schemaJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(schemaJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ClientPortalException("INVALID_IMPORT", "Form schema must be a JSON array.");
            }
        }
        catch (JsonException ex)
        {
            throw new ClientPortalException("INVALID_IMPORT", $"Invalid schema JSON: {ex.Message}");
        }
    }

    private static FormTemplateVersion? ResolveDisplayVersion(FormTemplate template) =>
        template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault(v => v.IsPublished)
        ?? template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

    private static FormTemplateListItemDto MapListItem(FormTemplate t)
    {
        var version = ResolveDisplayVersion(t);
        var publishedVersion = t.Versions.Where(v => v.IsPublished).OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        return new FormTemplateListItemDto(
            t.Uuid,
            t.Name,
            t.FormType,
            t.Status.ToString(),
            t.IsActive,
            publishedVersion?.VersionNumber ?? version?.VersionNumber ?? 0,
            t.Versions.Any(v => v.IsPublished),
            t.AgencyTags.Select(a => a.AgencyId).ToList(),
            CountFields(version?.SchemaJson),
            t.CreatedAt,
            0);
    }

    private static int CountFields(string? schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            return 0;
        }

        try
        {
            using var doc = JsonDocument.Parse(schemaJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return 0;
            }

            var count = 0;
            foreach (var field in doc.RootElement.EnumerateArray())
            {
                if (TryGetFieldName(field, out var name) && !string.IsNullOrWhiteSpace(name))
                {
                    count++;
                }
            }

            return count;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static bool TryGetFieldName(JsonElement field, out string? name)
    {
        if (field.TryGetProperty("name", out var camel) && camel.ValueKind == JsonValueKind.String)
        {
            name = camel.GetString();
            return true;
        }

        if (field.TryGetProperty("Name", out var pascal) && pascal.ValueKind == JsonValueKind.String)
        {
            name = pascal.GetString();
            return true;
        }

        name = null;
        return false;
    }

    private static FormTemplateDetailDto Map(FormTemplate t)
    {
        var version = ResolveDisplayVersion(t);
        return new FormTemplateDetailDto(
            t.Uuid,
            t.Name,
            t.FormType,
            t.Status.ToString(),
            t.IsActive,
            version?.SchemaJson ?? "[]",
            version?.VersionNumber ?? 0,
            t.AgencyTags.Select(a => a.AgencyId).ToList(),
            t.CreatedAt,
            t.UpdatedAt,
            t.Versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new FormTemplateVersionSummaryDto(v.VersionNumber, v.IsPublished, v.CreatedAt))
                .ToList());
    }
}

public class CertificateTemplateService : ICertificateTemplateService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _configuration;
    private readonly string _storageRoot;

    public CertificateTemplateService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _storageRoot = UploadStorage.ResolveRoot(configuration, environment);
    }

    public async Task<IReadOnlyList<CertificateTemplateListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var rows = await QueryGraph()
            .Include(t => t.ProcessAssignments)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(t =>
        {
            var version = t.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            return new CertificateTemplateListItemDto(
                t.Uuid,
                t.Name,
                t.Description,
                t.Agency?.Code,
                t.IsActive,
                version?.VersionNumber ?? 0,
                t.Versions.Any(v => v.IsPublished),
                version?.Elements.Count ?? 0,
                t.ProcessAssignments
                    .Where(a => a.IsActive)
                    .Select(a => a.ProcessType.ToString())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                t.CreatedAt);
        }).ToList();
    }

    public async Task<CertificateTemplateDetailDto?> GetAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var template = await QueryGraph().FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken);
        return template is null ? null : Map(template);
    }

    public async Task<CertificateTemplateDetailDto> SaveAsync(Guid? uuid, SaveCertificateTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        CertificateTemplate template;
        if (uuid is null)
        {
            template = new CertificateTemplate
            {
                Uuid = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Description = request.Description,
                AgencyId = request.AgencyId,
                IsActive = true
            };
            _db.CertificateTemplates.Add(template);
        }
        else
        {
            template = await QueryGraph().FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
                ?? throw new ClientPortalException("NOT_FOUND", "Certificate template not found.");
            template.Name = request.Name.Trim();
            template.Description = request.Description;
            template.AgencyId = request.AgencyId;
            if (request.IsActive.HasValue)
            {
                template.IsActive = request.IsActive.Value;
            }
        }

        if (request.ProcessTypes is not null)
        {
            template.ProcessAssignments.Clear();
            foreach (var processType in request.ProcessTypes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse<CertificateProcessType>(processType, true, out var parsed))
                {
                    continue;
                }

                var agencyIds = template.AgencyId.HasValue
                    ? new long[] { template.AgencyId.Value }
                    : (await _db.Agencies.Where(a => a.IsActive).Select(a => a.Id).ToListAsync(cancellationToken)).ToArray();

                foreach (var agencyId in agencyIds)
                {
                    template.ProcessAssignments.Add(new CertificateProcessAssignment
                    {
                        AgencyId = agencyId,
                        ProcessType = parsed,
                        IsActive = true,
                    });
                }
            }
        }

        var shouldCreateVersion = request.Elements.Count > 0 || !string.IsNullOrWhiteSpace(request.LayoutJson);
        if (shouldCreateVersion)
        {
            var nextVersion = template.Versions.Any() ? template.Versions.Max(v => v.VersionNumber) + 1 : 1;
            var version = new CertificateTemplateVersion
            {
                VersionNumber = nextVersion,
                IsPublished = request.Publish,
                LayoutJson = request.LayoutJson,
            };

            foreach (var element in request.Elements.OrderBy(e => e.SortOrder))
            {
                if (!Enum.TryParse<CertificateElementType>(element.ElementType, true, out var type))
                {
                    throw new ClientPortalException("INVALID_ELEMENT", $"Invalid element type {element.ElementType}.");
                }

                version.Elements.Add(new CertificateElement
                {
                    ElementType = type,
                    Label = element.Label,
                    ConfigJson = element.ConfigJson,
                    SortOrder = element.SortOrder
                });
            }

            template.Versions.Add(version);
        }
        AuditLogHelper.Write(_db, admin.Id, "certificate_template_saved", "certificate_template", template.Uuid.ToString());
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await QueryGraph().FirstAsync(t => t.Id == template.Id, cancellationToken));
    }

    public async Task<CertificateTemplateDetailDto> CloneAsync(Guid uuid, CloneFormTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var source = await GetAsync(uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Certificate template not found.");

        var name = string.IsNullOrWhiteSpace(request.Name) ? $"{source.Name} (Copy)" : request.Name.Trim();
        var elements = source.Elements
            .Select(e => new CertificateElementInput(e.ElementType, e.Label, e.ConfigJson, e.SortOrder))
            .ToList();

        var cloned = await SaveAsync(null, new SaveCertificateTemplateRequest(
            name,
            source.Description,
            source.AgencyId,
            elements,
            source.ProcessTypes.ToList(),
            false,
            false,
            source.LayoutJson), cancellationToken);

        return cloned;
    }

    public async Task<CertificateTemplateDetailDto> SetActiveAsync(Guid uuid, bool isActive, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var template = await QueryGraph().FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Certificate template not found.");

        if (isActive)
        {
            var latest = template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()
                ?? throw new ClientPortalException("EMPTY_TEMPLATE", "Add elements and save the template before activating it.");

            if (latest.Elements.Count == 0)
            {
                throw new ClientPortalException("EMPTY_TEMPLATE", "Add elements and save the template before activating it.");
            }

            if (!template.Versions.Any(v => v.IsPublished))
            {
                latest.IsPublished = true;
            }

            template.IsActive = true;
        }
        else
        {
            template.IsActive = false;
        }

        AuditLogHelper.Write(
            _db,
            admin.Id,
            isActive ? "certificate_template_activated" : "certificate_template_deactivated",
            "certificate_template",
            template.Uuid.ToString());
        await _db.SaveChangesAsync(cancellationToken);
        return Map(template);
    }

    public async Task DeleteAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var template = await QueryGraph().FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Certificate template not found.");

        var hasPublishedVersion = template.Versions.Any(v => v.IsPublished);
        if (template.IsActive && hasPublishedVersion)
        {
            throw new ClientPortalException(
                "CANNOT_DELETE_LIVE",
                "Cannot delete a live certificate template. Deactivate it first.");
        }

        var versionIds = template.Versions.Select(v => v.Id).ToList();
        if (versionIds.Count > 0)
        {
            var issuedCount = await _db.Certificates
                .CountAsync(
                    c => c.TemplateVersionId != null && versionIds.Contains(c.TemplateVersionId.Value),
                    cancellationToken);
            if (issuedCount > 0)
            {
                throw new ClientPortalException(
                    "CANNOT_DELETE_HAS_CERTIFICATES",
                    issuedCount == 1
                        ? "Cannot delete this template because 1 certificate was issued from it."
                        : $"Cannot delete this template because {issuedCount} certificates were issued from it.");
            }
        }

        _db.CertificateTemplates.Remove(template);
        AuditLogHelper.Write(_db, admin.Id, "certificate_template_deleted", "certificate_template", template.Uuid.ToString());
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> ExportAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var template = await QueryGraph().FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Certificate template not found.");

        var version = template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        var elements = new JsonArray(
            (version?.Elements.OrderBy(e => e.SortOrder) ?? Enumerable.Empty<CertificateElement>())
            .Select(e => new JsonObject
            {
                ["elementType"] = e.ElementType.ToString(),
                ["label"] = e.Label,
                ["configJson"] = e.ConfigJson,
                ["sortOrder"] = e.SortOrder,
            })
            .ToArray<JsonNode?>());

        var export = new JsonObject
        {
            ["format"] = "agricheck-certificate-template",
            ["formatVersion"] = "3.0",
            ["exportedAt"] = DateTime.UtcNow,
            ["name"] = template.Name,
            ["description"] = template.Description,
            ["agencyId"] = template.AgencyId,
            ["agencyCode"] = template.Agency?.Code,
            ["isActive"] = template.IsActive,
            ["processTypes"] = new JsonArray(
                template.ProcessAssignments
                    .Where(a => a.IsActive)
                    .Select(a => a.ProcessType.ToString())
                    .Distinct()
                    .Select(value => JsonValue.Create(value))
                    .ToArray<JsonNode?>()),
            ["layoutJson"] = version?.LayoutJson,
            ["isPublished"] = version?.IsPublished ?? false,
            ["elements"] = elements,
        };

        return export.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<CertificateTemplateDetailDto> ImportAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);

        string name;
        string? description;
        long? agencyId;
        IReadOnlyList<string> processTypes;
        string? layoutJson;
        IReadOnlyList<CertificateElementInput> elements;

        if (payload.TryGetProperty("format", out var formatProp)
            && formatProp.GetString() == "agricheck-certificate-template")
        {
            name = payload.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Imported Template" : "Imported Template";
            description = payload.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
            agencyId = payload.TryGetProperty("agencyId", out var agencyIdProp) && agencyIdProp.ValueKind != JsonValueKind.Null
                ? agencyIdProp.GetInt64()
                : null;
            layoutJson = payload.TryGetProperty("layoutJson", out var layoutProp) ? layoutProp.GetString() : null;
            processTypes = payload.TryGetProperty("processTypes", out var processProp)
                ? processProp.EnumerateArray().Select(item => item.GetString() ?? string.Empty).Where(x => x.Length > 0).Distinct().ToList()
                : new List<string> { "ImportEntry" };
            elements = ParseImportedElements(payload);
        }
        else if (payload.TryGetProperty("template", out var templateProp))
        {
            name = templateProp.TryGetProperty("name", out var v2Name) ? v2Name.GetString() ?? "Imported Template" : "Imported Template";
            description = templateProp.TryGetProperty("description", out var v2Desc) ? v2Desc.GetString() : null;
            layoutJson = BuildLayoutJsonFromV2Template(templateProp);
            processTypes = ResolveV2CertificateProcessTypes(templateProp, payload);
            agencyId = await ResolveV2CertificateAgencyIdAsync(templateProp, payload, cancellationToken);
            elements = payload.TryGetProperty("elements", out var elementsProp)
                ? ParseV2CertificateElements(elementsProp)
                : Array.Empty<CertificateElementInput>();
        }
        else
        {
            throw new ClientPortalException("INVALID_IMPORT", "Unrecognized import format. Upload a V3 export file or compatible V2 JSON export.");
        }

        if (agencyId is null && payload.TryGetProperty("agencyCode", out var agencyCodeProp))
        {
            var agencyCode = agencyCodeProp.GetString();
            if (!string.IsNullOrWhiteSpace(agencyCode))
            {
                agencyId = await _db.Agencies
                    .Where(a => a.Code == agencyCode.Trim().ToUpperInvariant())
                    .Select(a => (long?)a.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ClientPortalException("INVALID_IMPORT", "Template name is required.");
        }

        if (processTypes.Count == 0)
        {
            processTypes = new List<string> { "ImportEntry" };
        }

        var imported = await SaveAsync(null, new SaveCertificateTemplateRequest(
            name.Trim(),
            description,
            agencyId,
            elements,
            processTypes,
            false,
            false,
            layoutJson), cancellationToken);

        return imported;
    }

    public async Task<byte[]> PreviewAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var template = await QueryGraph().FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Certificate template not found.");

        var version = template.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()
            ?? throw new ClientPortalException("NO_VERSION", "Template has no version to preview.");

        var elements = version.Elements.OrderBy(e => e.SortOrder).ToList();
        var publicBase = _configuration["App:PublicBaseUrl"] ?? "http://localhost:5173";
        var verifyUrl = $"{publicBase.TrimEnd('/')}/verify?code=PREVIEW-12345";
        var qrData = QrCodeGenerator.ToBase64Png(verifyUrl);
        var storageRoot = _storageRoot;
        var variables = BuildPreviewVariables(template);

        return CertificateTemplatePdfGenerator.Generate(version, elements, variables, verifyUrl, qrData, storageRoot);
    }

    public async Task<string> UploadImageAsync(Guid uuid, Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        _ = await QueryGraph().FirstOrDefaultAsync(t => t.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Certificate template not found.");

        var (storedFileName, _) = await _fileStorage.SaveAsync(
            fileStream,
            "certificate-templates/assets",
            fileName,
            cancellationToken);

        return $"certificate-templates/assets/{storedFileName}";
    }

    private static Dictionary<string, object?> BuildPreviewVariables(CertificateTemplate template)
    {
        var processTypes = template.ProcessAssignments
            .Where(a => a.IsActive)
            .Select(a => a.ProcessType)
            .Distinct()
            .ToList();

        if (processTypes.Contains(CertificateProcessType.ImportEntry) ||
            processTypes.Contains(CertificateProcessType.ExportEntry))
        {
            return EntryCertificateVariableBuilder.BuildPreviewVariables();
        }

        return BuildAccreditationPreviewVariables();
    }

    private static Dictionary<string, object?> BuildAccreditationPreviewVariables() => new()
    {
        ["certificate"] = new Dictionary<string, object?>
        {
            ["number"] = "CERT-PREVIEW-00001",
            ["issued_at"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["expires_at"] = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
        },
        ["company"] = new Dictionary<string, object?>
        {
            ["name"] = "Sample Company Inc.",
            ["address"] = "block 1, lot 1, Pulang Lupa Uno, Las Piñas, Metro Manila, National Capital Region (NCR), 1742",
            ["registration_number"] = "REG-123456",
            ["tin"] = "258-695-852-000",
            ["business_type"] = "Corporation",
            ["nature_of_business"] = "Importation",
        },
        ["accreditation"] = new Dictionary<string, object?>
        {
            ["type"] = "New Accreditation",
            ["number"] = "DA-2026-NIMP-00001",
            ["status"] = "Approved",
        },
        ["officer"] = new Dictionary<string, object?>
        {
            ["name"] = "Juan Dela Cruz",
            ["title"] = "Accreditation Officer",
        },
        ["date"] = new Dictionary<string, object?>
        {
            ["issued"] = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
            ["expires"] = DateTime.UtcNow.AddYears(1).ToString("MMMM dd, yyyy"),
        },
    };

    private static IReadOnlyList<CertificateElementInput> ParseImportedElements(JsonElement payload)
    {
        if (!payload.TryGetProperty("elements", out var elementsProp) || elementsProp.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<CertificateElementInput>();
        }

        return elementsProp.EnumerateArray()
            .Select((element, index) => new CertificateElementInput(
                element.TryGetProperty("elementType", out var typeProp) ? typeProp.GetString() ?? "Text" : "Text",
                element.TryGetProperty("label", out var labelProp) ? labelProp.GetString() ?? $"Element {index + 1}" : $"Element {index + 1}",
                element.TryGetProperty("configJson", out var configProp) ? configProp.GetString() : null,
                element.TryGetProperty("sortOrder", out var sortProp) ? sortProp.GetInt32() : index + 1))
            .ToList();
    }

    private static IReadOnlyList<CertificateElementInput> ParseV2CertificateElements(JsonElement elementsProp)
    {
        var sortOrder = 1;
        return elementsProp.EnumerateArray()
            .Select(element =>
            {
                var elementType = element.TryGetProperty("element_type", out var snakeType)
                    ? snakeType.GetString() ?? "TEXT"
                    : element.TryGetProperty("elementType", out var camelType)
                        ? camelType.GetString() ?? "TEXT"
                        : "TEXT";

                var content = element.TryGetProperty("content", out var contentProp) ? contentProp.GetString() : null;
                var config = new Dictionary<string, object?>
                {
                    ["content"] = content,
                    ["x"] = ReadCertificateJsonNumber(element, "x_position", "x"),
                    ["y"] = ReadCertificateJsonNumber(element, "y_position", "y"),
                    ["width"] = ReadCertificateJsonNumber(element, "width"),
                    ["height"] = ReadCertificateJsonNumber(element, "height"),
                    ["fontSize"] = ReadCertificateJsonNumber(element, "font_size", "fontSize"),
                    ["fontWeight"] = ReadJsonString(element, "font_weight", "fontWeight"),
                    ["fontStyle"] = ReadJsonString(element, "font_style", "fontStyle"),
                    ["textAlign"] = ReadJsonString(element, "text_align", "textAlign"),
                    ["textColor"] = ReadJsonString(element, "text_color", "textColor"),
                    ["imagePath"] = ReadJsonString(element, "image_path", "imagePath"),
                    ["zIndex"] = ReadCertificateJsonNumber(element, "z_index", "zIndex", sortOrder),
                    ["displayOrder"] = ReadCertificateJsonNumber(element, "display_order", "displayOrder", sortOrder),
                };

                var label = !string.IsNullOrWhiteSpace(content)
                    ? (content!.Length <= 64 ? content : $"{content[..61]}...")
                    : elementType.Equals("QR_CODE", StringComparison.OrdinalIgnoreCase) ? "Verification QR" : "Element";

                return new CertificateElementInput(
                    MapV2ElementType(elementType),
                    label,
                    JsonSerializer.Serialize(config),
                    sortOrder++);
            })
            .ToList();
    }

    private static string MapV2ElementType(string elementType) => elementType.ToUpperInvariant() switch
    {
        "TEXT" => "Text",
        "IMAGE" => "Image",
        "QR_CODE" => "QrCode",
        "SHAPE" => "Shape",
        "LINE" => "Line",
        _ => "Text",
    };

    private static string? BuildLayoutJsonFromV2Template(JsonElement templateProp)
    {
        var layout = new Dictionary<string, object?>
        {
            ["paperSize"] = ReadJsonString(templateProp, "paper_size", "paperSize") ?? "A4",
            ["orientation"] = ReadJsonString(templateProp, "orientation") ?? "PORTRAIT",
            ["marginTop"] = ReadCertificateJsonNumber(templateProp, "margin_top", "marginTop", fallback: 10),
            ["marginRight"] = ReadCertificateJsonNumber(templateProp, "margin_right", "marginRight", fallback: 10),
            ["marginBottom"] = ReadCertificateJsonNumber(templateProp, "margin_bottom", "marginBottom", fallback: 10),
            ["marginLeft"] = ReadCertificateJsonNumber(templateProp, "margin_left", "marginLeft", fallback: 10),
            ["backgroundColor"] = ReadJsonString(templateProp, "background_color", "backgroundColor") ?? "#ffffff",
            ["backgroundImage"] = ReadJsonString(templateProp, "background_image", "backgroundImage"),
        };

        return JsonSerializer.Serialize(layout);
    }

    private static IReadOnlyList<string> ResolveV2CertificateProcessTypes(JsonElement templateProp, JsonElement payload)
    {
        if (payload.TryGetProperty("processTypes", out var processTypesProp) && processTypesProp.ValueKind == JsonValueKind.Array)
        {
            return processTypesProp.EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();
        }

        if (payload.TryGetProperty("process_assignments", out var assignmentsProp) && assignmentsProp.ValueKind == JsonValueKind.Array)
        {
            return assignmentsProp.EnumerateArray()
                .Select(item => item.TryGetProperty("process_type", out var typeProp) ? typeProp.GetString() : null)
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .Select(MapV2ProcessType)
                .Distinct()
                .ToList();
        }

        if (templateProp.TryGetProperty("processTypes", out var templateProcessProp) && templateProcessProp.ValueKind == JsonValueKind.Array)
        {
            return templateProcessProp.EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();
        }

        return new List<string> { "ImportEntry" };
    }

    private static string MapV2ProcessType(string? processType) => processType?.ToUpperInvariant() switch
    {
        "ENTRY_SUBMISSION" => "ImportEntry",
        "ACCREDITATION" => "Accreditation",
        "MAV" => "Accreditation",
        _ => processType ?? "ImportEntry",
    };

    private async Task<long?> ResolveV2CertificateAgencyIdAsync(
        JsonElement templateProp,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        if (templateProp.TryGetProperty("agency_id", out var snakeAgency) && snakeAgency.ValueKind == JsonValueKind.Number)
        {
            return snakeAgency.GetInt64();
        }

        if (templateProp.TryGetProperty("agencyId", out var camelAgency) && camelAgency.ValueKind == JsonValueKind.Number)
        {
            return camelAgency.GetInt64();
        }

        string? agencyCode = null;
        if (payload.TryGetProperty("agency", out var agencyProp))
        {
            agencyCode = agencyProp.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;
        }

        if (string.IsNullOrWhiteSpace(agencyCode))
        {
            return null;
        }

        return await _db.Agencies
            .Where(a => a.Code == agencyCode.Trim().ToUpperInvariant())
            .Select(a => (long?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? ReadJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static int ReadCertificateJsonNumber(
        JsonElement element,
        string primaryName,
        string? secondaryName = null,
        int fallback = 0)
    {
        foreach (var propertyName in new[] { primaryName, secondaryName })
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                continue;
            }

            if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number)
            {
                return value.TryGetInt32(out var numberValue) ? numberValue : (int)value.GetDouble();
            }
        }

        return fallback;
    }

    private IQueryable<CertificateTemplate> QueryGraph() =>
        _db.CertificateTemplates
            .Include(t => t.Agency)
            .Include(t => t.ProcessAssignments)
            .Include(t => t.Versions).ThenInclude(v => v.Elements);

    private static CertificateTemplateDetailDto Map(CertificateTemplate t)
    {
        var version = t.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        var versions = t.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new CertificateTemplateVersionSummaryDto(v.VersionNumber, v.IsPublished, v.CreatedAt))
            .ToList();
        return new CertificateTemplateDetailDto(
            t.Uuid,
            t.Name,
            t.Description,
            t.AgencyId,
            t.IsActive,
            version?.VersionNumber ?? 0,
            version?.IsPublished ?? false,
            version?.LayoutJson,
            t.ProcessAssignments
                .Where(a => a.IsActive)
                .Select(a => a.ProcessType.ToString())
                .Distinct()
                .OrderBy(x => x)
                .ToList(),
            version?.Elements.OrderBy(e => e.SortOrder).Select(e => new CertificateElementDto(
                e.Id, e.ElementType.ToString(), e.Label, e.ConfigJson, e.SortOrder)).ToList()
            ?? new List<CertificateElementDto>(),
            t.CreatedAt,
            t.UpdatedAt,
            versions);
    }
}

public class CertificateIssuanceService : ICertificateIssuanceService
{
    private readonly AgriCheckDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _configuration;
    private readonly IEntryCertificateGenerationService _entryCertificateGeneration;

    public CertificateIssuanceService(
        AgriCheckDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IConfiguration configuration,
        IEntryCertificateGenerationService entryCertificateGeneration)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _entryCertificateGeneration = entryCertificateGeneration;
    }

    public async Task<PagedResult<AdminCertificateListItemDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var query = _db.Certificates.Include(c => c.User).ThenInclude(u => u.Profile).Include(c => c.Entry);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(c => c.IssuedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => MapAdmin(c)).ToListAsync(cancellationToken);
        return new PagedResult<AdminCertificateListItemDto>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AdminApprovedEntryDto>> ListApprovedEntriesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var query = _db.Entries
            .Include(e => e.User).ThenInclude(u => u.Profile)
            .Include(e => e.Agency)
            .Where(e => EntryStatusRules.CanIssueCertificate(e.Status));
        var total = await query.CountAsync(cancellationToken);
        var entryIdsWithCerts = _db.Certificates
            .Where(c => c.Status == CertificateStatus.Active)
            .Select(c => c.EntryId);
        var items = await query.OrderByDescending(e => e.SubmittedAt ?? e.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new AdminApprovedEntryDto(
                e.Uuid,
                e.ReferenceNo,
                e.User.Profile != null ? e.User.Profile.FirstName + " " + e.User.Profile.LastName : e.User.Email,
                e.Agency.Code,
                entryIdsWithCerts.Contains(e.Id)))
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminApprovedEntryDto>(items, page, pageSize, total);
    }

    public async Task<AdminCertificateListItemDto> IssueAsync(IssueCertificateRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var entry = await _db.Entries
            .Include(e => e.User).ThenInclude(u => u.Profile)
            .Include(e => e.Agency)
            .Include(e => e.Detail)
            .FirstOrDefaultAsync(e => e.Uuid == request.EntryUuid, cancellationToken)
            ?? throw new ClientPortalException("ENTRY_NOT_FOUND", "Entry not found.");

        if (entry.Status != EntryStatus.Approved && !EntryStatusRules.CanIssueCertificate(entry.Status))
        {
            throw new ClientPortalException("ENTRY_NOT_APPROVED", "Entry must pass evaluation and DA billing before certificate issuance.");
        }

        if (await _db.Certificates.AnyAsync(c => c.EntryId == entry.Id && c.Status == CertificateStatus.Active, cancellationToken))
        {
            throw new ClientPortalException("CERT_EXISTS", "An active certificate already exists for this entry.");
        }

        var version = await _entryCertificateGeneration.ResolveTemplateVersionAsync(
            entry,
            request.TemplateId,
            cancellationToken);

        var verificationCode = Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
        var publicBase = _configuration["App:PublicBaseUrl"] ?? "http://localhost:5173";
        var verifyUrl = $"{publicBase.TrimEnd('/')}/verify?code={verificationCode}";
        var qrData = QrCodeGenerator.ToBase64Png(verifyUrl);
        var holderName = entry.User.Profile is not null ? $"{entry.User.Profile.FirstName} {entry.User.Profile.LastName}" : entry.User.Email;
        var entryLabel = entry.EntryType == EntryType.Export ? "Export" : "Import";
        var title = $"{entryLabel} Certificate - {entry.ReferenceNo}";
        var issuedAt = DateTime.UtcNow;
        var expiresAt = request.ExpiresAt ?? issuedAt.AddYears(1);
        var certificateNumber = await ReferenceNumberGenerator.CertificateAsync(_db, cancellationToken);

        var certificate = new Certificate
        {
            Uuid = Guid.NewGuid(),
            UserId = entry.UserId,
            EntryId = entry.Id,
            AgencyId = entry.AgencyId,
            TemplateVersionId = version?.Id,
            IssuedByUserId = admin.Id,
            CertificateNumber = certificateNumber,
            VerificationCode = verificationCode,
            Title = title,
            Status = CertificateStatus.Active,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            QrCodeData = qrData,
            SummaryJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                processType = entry.EntryType == EntryType.Export
                    ? CertificateProcessType.ExportEntry.ToString()
                    : CertificateProcessType.ImportEntry.ToString(),
                entry.ReferenceNo,
                entry.Agency.Code,
                entry.Detail?.CommodityName,
                holderName
            })
        };

        var pdfBytes = await _entryCertificateGeneration.GeneratePdfAsync(
            entry,
            certificate.CertificateNumber,
            title,
            verificationCode,
            verifyUrl,
            qrData,
            issuedAt,
            expiresAt,
            admin.Id,
            request.TemplateId,
            cancellationToken);
        await using var pdfStream = new MemoryStream(pdfBytes);
        var (storedFileName, _) = await _fileStorage.SaveAsync(pdfStream, $"certificates/{certificate.Uuid}", $"{certificate.CertificateNumber}.pdf", cancellationToken);
        certificate.PdfStoredFileName = storedFileName;

        entry.TimelineEvents.Add(new TimelineEvent
        {
            EventType = "certificate_issued",
            Title = "Certificate issued",
            Description = $"Certificate {certificate.CertificateNumber} issued.",
            ActorUserId = admin.Id
        });

        _db.Certificates.Add(certificate);
        AuditLogHelper.Write(_db, admin.Id, "certificate_issued", "certificate", certificate.Uuid.ToString(), new { certificate.CertificateNumber, entry.ReferenceNo });
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdmin(await _db.Certificates.Include(c => c.User).ThenInclude(u => u.Profile).Include(c => c.Entry).FirstAsync(c => c.Id == certificate.Id, cancellationToken));
    }

    public async Task<AdminCertificateListItemDto> RevokeAsync(Guid uuid, RevokeCertificateRequest request, CancellationToken cancellationToken = default)
    {
        var admin = await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var cert = await _db.Certificates.Include(c => c.User).ThenInclude(u => u.Profile).Include(c => c.Entry)
            .FirstOrDefaultAsync(c => c.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Certificate not found.");

        if (cert.Status == CertificateStatus.Revoked)
        {
            throw new ClientPortalException("ALREADY_REVOKED", "Certificate is already revoked.");
        }

        cert.Status = CertificateStatus.Revoked;
        cert.RevokedAt = DateTime.UtcNow;
        cert.RevokedReason = request.Reason;
        AuditLogHelper.Write(_db, admin.Id, "certificate_revoked", "certificate", cert.Uuid.ToString(), new { request.Reason });
        await _db.SaveChangesAsync(cancellationToken);
        return MapAdmin(cert);
    }

    public async Task<byte[]?> DownloadPdfAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await AdminContextHelper.RequireAdminAsync(_db, _currentUser, cancellationToken);
        var cert = await _db.Certificates.FirstOrDefaultAsync(c => c.Uuid == uuid, cancellationToken)
            ?? throw new ClientPortalException("NOT_FOUND", "Certificate not found.");
        if (string.IsNullOrWhiteSpace(cert.PdfStoredFileName)) return null;
        var path = _fileStorage.GetPhysicalPath($"certificates/{cert.Uuid}", cert.PdfStoredFileName);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }

    private static AdminCertificateListItemDto MapAdmin(Certificate c) => new(
        c.Uuid, c.CertificateNumber, c.Title, c.Status.ToString(),
        c.User.Profile is not null ? $"{c.User.Profile.FirstName} {c.User.Profile.LastName}" : c.User.Email,
        c.Entry?.ReferenceNo, c.IssuedAt, c.ExpiresAt, c.RevokedAt);
}
