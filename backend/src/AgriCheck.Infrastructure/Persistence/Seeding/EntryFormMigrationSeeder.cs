using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class EntryFormMigrationSeeder
{
    private const string MigrationSettingKey = "entry_forms_v2_migrated";

    public static async Task MigrateFromV2Async(
        AgriCheckDbContext db,
        IConfiguration configuration,
        ILogger logger,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (!force && await db.SystemSettings.AnyAsync(s => s.SettingKey == MigrationSettingKey, cancellationToken))
        {
            logger.LogInformation("Entry form V2 migration already completed; skipping. Run EntryFormMigrationRunner with --force to re-import.");
            return;
        }

        var v2ConnectionString = configuration.GetConnectionString("V2Connection")
            ?? "Server=localhost;Port=3306;Database=agricheck_dev;User=root;Password=;";

        await using var connection = new MySqlConnection(v2ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to connect to V2 database for entry form migration.");
            return;
        }

        var templates = await ReadEntryTemplatesAsync(connection, cancellationToken);
        if (templates.Count == 0)
        {
            logger.LogWarning("No active ENTRY form templates found in V2.");
            return;
        }

        var agencyCodeMap = await db.Agencies
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Code, a => a.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var migratedCount = 0;
        foreach (var template in templates)
        {
            var agencyIds = template.AgencyCodes
                .Select(code => agencyCodeMap.TryGetValue(code, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (agencyIds.Count == 0)
            {
                logger.LogWarning(
                    "Skipping V2 entry form {V2Id} ({Name}) — no mapped agencies ({Agencies}).",
                    template.Id,
                    template.Name,
                    string.Join(", ", template.AgencyCodes));
                continue;
            }

            var fields = await ReadFieldsAsync(connection, template.Id, cancellationToken);
            if (fields.Count == 0)
            {
                logger.LogWarning("Skipping V2 entry form {V2Id} ({Name}) — no fields.", template.Id, template.Name);
                continue;
            }

            var schemaJson = V2FormSchemaConverter.ConvertFieldsToSchemaJson(fields);
            await UpsertTemplateAsync(db, template, agencyIds, schemaJson, cancellationToken);
            migratedCount++;

            logger.LogInformation(
                "Migrated V2 entry form {V2Id} ({Name}) with {FieldCount} fields for agencies {Agencies}.",
                template.Id,
                template.Name,
                fields.Count,
                string.Join(", ", template.AgencyCodes));
        }

        if (migratedCount == 0)
        {
            logger.LogWarning("Entry form V2 migration finished without importing any templates.");
            return;
        }

        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == MigrationSettingKey, cancellationToken);
        if (setting is null)
        {
            db.SystemSettings.Add(new SystemSetting
            {
                SettingKey = MigrationSettingKey,
                SettingValue = DateTime.UtcNow.ToString("O"),
            });
        }
        else
        {
            setting.SettingValue = DateTime.UtcNow.ToString("O");
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Entry form V2 migration completed for {Count} template(s).", migratedCount);
    }

    private static async Task UpsertTemplateAsync(
        AgriCheckDbContext db,
        V2EntryTemplate template,
        IReadOnlyList<long> agencyIds,
        string schemaJson,
        CancellationToken cancellationToken)
    {
        var existing = await db.FormTemplates
            .Include(t => t.Versions)
            .Include(t => t.AgencyTags)
            .Where(t => t.FormType == "ENTRY")
            .Where(t => t.AgencyTags.Any(tag => agencyIds.Contains(tag.AgencyId)))
            .OrderByDescending(t => t.Versions.Count(v => v.IsPublished))
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            existing = new FormTemplate
            {
                Uuid = Guid.NewGuid(),
                Name = template.Name,
                FormType = "ENTRY",
                Status = FormTemplateStatus.Published,
                IsActive = template.IsActive,
            };
            db.FormTemplates.Add(existing);
        }
        else if (string.IsNullOrWhiteSpace(existing.Name))
        {
            existing.Name = template.Name;
        }

        foreach (var version in existing.Versions.Where(v => v.IsPublished))
        {
            version.IsPublished = false;
        }

        var nextVersionNumber = existing.Versions.Count == 0
            ? 1
            : existing.Versions.Max(v => v.VersionNumber) + 1;

        existing.Versions.Add(new FormTemplateVersion
        {
            VersionNumber = nextVersionNumber,
            SchemaJson = schemaJson,
            IsPublished = true,
        });

        existing.Status = FormTemplateStatus.Published;
        existing.IsActive = template.IsActive;
        existing.AgencyTags.Clear();

        foreach (var agencyId in agencyIds)
        {
            existing.AgencyTags.Add(new FormAgencyTag { AgencyId = agencyId });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<V2EntryTemplate>> ReadEntryTemplatesAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        var templates = new List<V2EntryTemplate>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, is_active
            FROM form_templates
            WHERE form_type = 'ENTRY' AND is_active = 1
            ORDER BY id
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            templates.Add(new V2EntryTemplate(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                Array.Empty<string>()));
        }

        await reader.CloseAsync();

        foreach (var template in templates)
        {
            template.AgencyCodes.AddRange(await ReadAgencyCodesAsync(connection, template.Id, cancellationToken));
        }

        return templates;
    }

    private static async Task<List<string>> ReadAgencyCodesAsync(
        MySqlConnection connection,
        int templateId,
        CancellationToken cancellationToken)
    {
        var codes = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT a.code
            FROM form_agency_tags fat
            INNER JOIN agencies a ON a.id = fat.agency_id
            WHERE fat.form_template_id = @id
            ORDER BY a.code
            """;
        command.Parameters.AddWithValue("@id", templateId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            codes.Add(reader.GetString(0));
        }

        return codes;
    }

    private static async Task<List<V2FormField>> ReadFieldsAsync(
        MySqlConnection connection,
        int templateId,
        CancellationToken cancellationToken)
    {
        var fields = new List<V2FormField>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT field_name, field_label, field_type, placeholder, help_text, tooltip,
                   is_required, display_order, column_width, validation_rules, conditional_logic, options
            FROM form_fields
            WHERE form_template_id = @id
            ORDER BY display_order, id
            """;
        command.Parameters.AddWithValue("@id", templateId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            fields.Add(new V2FormField(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                !reader.IsDBNull(6) && reader.GetBoolean(6),
                reader.GetInt32(7),
                reader.IsDBNull(8) ? 12 : reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11)));
        }

        return fields;
    }

    private sealed class V2EntryTemplate
    {
        public V2EntryTemplate(int id, string name, bool isActive, IEnumerable<string> agencyCodes)
        {
            Id = id;
            Name = name;
            IsActive = isActive;
            AgencyCodes = agencyCodes.ToList();
        }

        public int Id { get; }
        public string Name { get; }
        public bool IsActive { get; }
        public List<string> AgencyCodes { get; }
    }
}
