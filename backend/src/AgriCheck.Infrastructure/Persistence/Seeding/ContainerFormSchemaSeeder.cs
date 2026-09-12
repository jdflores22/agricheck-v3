using System.Text.Json;
using System.Text.Json.Nodes;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class ContainerFormSchemaSeeder
{
    private const string ContainerTypeFieldName = "select_container_type";
    private const string WarehouseNameFieldName = "select_warehouse_name";
    private const string WarehouseAddressFieldName = "textarea_warehouse_address";
    private const string ContainerTypeMigrationSettingKey = "container_type_field_added";
    private const string WarehouseNameMigrationSettingKey = "warehouse_name_field_added";

    public static async Task EnsureContainerTypeFieldAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.SystemSettings.AnyAsync(s => s.SettingKey == ContainerTypeMigrationSettingKey, cancellationToken))
        {
            return;
        }

        var templates = await db.FormTemplates
            .Include(t => t.Versions)
            .Where(t => t.FormType == "CONTAINER" && t.IsActive)
            .ToListAsync(cancellationToken);

        var updatedCount = 0;
        foreach (var template in templates)
        {
            var published = template.Versions
                .Where(v => v.IsPublished)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();

            if (published is null || string.IsNullOrWhiteSpace(published.SchemaJson))
            {
                continue;
            }

            if (!TryPatchSchema(published.SchemaJson, out var patchedSchema))
            {
                continue;
            }

            foreach (var version in template.Versions.Where(v => v.IsPublished))
            {
                version.IsPublished = false;
            }

            template.Versions.Add(new FormTemplateVersion
            {
                VersionNumber = template.Versions.Count == 0 ? 1 : template.Versions.Max(v => v.VersionNumber) + 1,
                SchemaJson = patchedSchema,
                IsPublished = true,
            });

            template.Status = FormTemplateStatus.Published;
            updatedCount++;
            logger.LogInformation("Added Container Type field to CONTAINER form template {Name}.", template.Name);
        }

        if (updatedCount == 0)
        {
            logger.LogWarning("Container Type field patch did not update any CONTAINER form templates.");
            return;
        }

        db.SystemSettings.Add(new SystemSetting
        {
            SettingKey = ContainerTypeMigrationSettingKey,
            SettingValue = DateTime.UtcNow.ToString("O"),
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Container Type field patch completed for {Count} template(s).", updatedCount);
    }

    public static async Task EnsureWarehouseNameFieldAsync(
        AgriCheckDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.SystemSettings.AnyAsync(s => s.SettingKey == WarehouseNameMigrationSettingKey, cancellationToken))
        {
            return;
        }

        var templates = await db.FormTemplates
            .Include(t => t.Versions)
            .Where(t => t.FormType == "CONTAINER" && t.IsActive)
            .ToListAsync(cancellationToken);

        var updatedCount = 0;
        foreach (var template in templates)
        {
            var published = template.Versions
                .Where(v => v.IsPublished)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefault();

            if (published is null || string.IsNullOrWhiteSpace(published.SchemaJson))
            {
                continue;
            }

            if (!TryPatchWarehouseField(published.SchemaJson, out var patchedSchema))
            {
                continue;
            }

            foreach (var version in template.Versions.Where(v => v.IsPublished))
            {
                version.IsPublished = false;
            }

            template.Versions.Add(new FormTemplateVersion
            {
                VersionNumber = template.Versions.Count == 0 ? 1 : template.Versions.Max(v => v.VersionNumber) + 1,
                SchemaJson = patchedSchema,
                IsPublished = true,
            });

            template.Status = FormTemplateStatus.Published;
            updatedCount++;
            logger.LogInformation("Added Warehouse Name field to CONTAINER form template {Name}.", template.Name);
        }

        if (updatedCount == 0)
        {
            logger.LogWarning("Warehouse Name field patch did not update any CONTAINER form templates.");
            return;
        }

        db.SystemSettings.Add(new SystemSetting
        {
            SettingKey = WarehouseNameMigrationSettingKey,
            SettingValue = DateTime.UtcNow.ToString("O"),
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Warehouse Name field patch completed for {Count} template(s).", updatedCount);
    }

    internal static bool TryPatchWarehouseField(string schemaJson, out string patchedSchema)
    {
        patchedSchema = schemaJson;

        try
        {
            var root = JsonNode.Parse(schemaJson)?.AsArray();
            if (root is null)
            {
                return false;
            }

            var hasWarehouseName = false;
            JsonObject? addressField = null;
            var addressIndex = -1;

            for (var index = 0; index < root.Count; index++)
            {
                if (root[index] is not JsonObject field)
                {
                    continue;
                }

                var name = field["name"]?.GetValue<string>();
                if (string.Equals(name, WarehouseNameFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    hasWarehouseName = true;
                }

                if (addressField is null
                    && (string.Equals(name, WarehouseAddressFieldName, StringComparison.OrdinalIgnoreCase)
                        || field["label"]?.GetValue<string>()?.Contains("Warehouse Address", StringComparison.OrdinalIgnoreCase) == true))
                {
                    addressField = field;
                    addressIndex = index;
                }
            }

            if (hasWarehouseName || addressField is null || addressIndex < 0)
            {
                return false;
            }

            var addressFieldName = addressField["name"]?.GetValue<string>() ?? WarehouseAddressFieldName;
            var nextDisplayOrder = (addressField["displayOrder"]?.GetValue<int>() ?? addressIndex + 1);

            var warehouseField = new JsonObject
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["name"] = WarehouseNameFieldName,
                ["label"] = "Warehouse Name",
                ["type"] = "warehouse",
                ["displayOrder"] = nextDisplayOrder,
                ["columnWidth"] = 6,
                ["required"] = true,
                ["placeholder"] = "Select registered warehouse",
                ["helpText"] = "Choose from DA-registered warehouses. Address auto-fills below.",
                ["validationRules"] = new JsonObject
                {
                    ["autofillTarget"] = addressFieldName,
                },
            };

            addressField["helpText"] = "Auto-filled from selected warehouse.";
            addressField["displayOrder"] = nextDisplayOrder + 1;

            root.Insert(addressIndex, warehouseField);

            for (var index = addressIndex + 1; index < root.Count; index++)
            {
                if (root[index] is JsonObject trailingField && trailingField.ContainsKey("displayOrder"))
                {
                    trailingField["displayOrder"] = trailingField["displayOrder"]!.GetValue<int>() + 1;
                }
            }

            return AssignPatchedSchema(JsonSerializer.Serialize(root), out patchedSchema);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    internal static bool TryPatchSchema(string schemaJson, out string patchedSchema)
    {
        patchedSchema = schemaJson;

        try
        {
            var root = JsonNode.Parse(schemaJson)?.AsArray();
            if (root is null)
            {
                return false;
            }

            var changed = false;
            var hasContainerType = false;
            JsonObject? sizeField = null;

            foreach (var node in root)
            {
                if (node is not JsonObject field)
                {
                    continue;
                }

                var name = field["name"]?.GetValue<string>();
                if (string.Equals(name, ContainerTypeFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    hasContainerType = true;
                }

                if (string.Equals(name, "select_container_size", StringComparison.OrdinalIgnoreCase))
                {
                    sizeField = field;
                    if (NormalizeContainerSizeOptions(field))
                    {
                        changed = true;
                    }
                }
            }

            if (hasContainerType)
            {
                return changed && JsonSerializer.Serialize(root) is { Length: > 0 } serialized
                    ? AssignPatchedSchema(serialized, out patchedSchema)
                    : changed;
            }

            var insertIndex = root.Count;
            var nextDisplayOrder = root.Count + 1;

            for (var index = 0; index < root.Count; index++)
            {
                if (root[index] is not JsonObject field)
                {
                    continue;
                }

                var name = field["name"]?.GetValue<string>();
                if (!string.Equals(name, "select_container_size", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                insertIndex = index + 1;
                nextDisplayOrder = (field["displayOrder"]?.GetValue<int>() ?? index + 1) + 1;
                break;
            }

            var containerTypeField = new JsonObject
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["name"] = ContainerTypeFieldName,
                ["label"] = "Container Type",
                ["type"] = "select",
                ["displayOrder"] = nextDisplayOrder,
                ["columnWidth"] = sizeField?["columnWidth"]?.GetValue<int>() ?? 3,
                ["required"] = true,
                ["placeholder"] = "Container Type",
                ["options"] = new JsonArray(
                    Option("Standard"),
                    Option("High Cube"),
                    Option("Reefer"),
                    Option("Open Top"),
                    Option("Flat Rack")),
                ["validationRules"] = new JsonObject(),
            };

            root.Insert(insertIndex, containerTypeField);

            for (var index = insertIndex + 1; index < root.Count; index++)
            {
                if (root[index] is JsonObject trailingField && trailingField.ContainsKey("displayOrder"))
                {
                    trailingField["displayOrder"] = trailingField["displayOrder"]!.GetValue<int>() + 1;
                }
            }

            return AssignPatchedSchema(JsonSerializer.Serialize(root), out patchedSchema);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool AssignPatchedSchema(string serialized, out string patchedSchema)
    {
        patchedSchema = serialized;
        return true;
    }

    private static JsonObject Option(string value) => new()
    {
        ["label"] = value,
        ["value"] = value,
    };

    private static bool NormalizeContainerSizeOptions(JsonObject sizeField)
    {
        if (sizeField["options"] is not JsonArray options)
        {
            return false;
        }

        var changed = false;
        foreach (var option in options)
        {
            if (option is not JsonObject optionObject)
            {
                continue;
            }

            var label = optionObject["label"]?.GetValue<string>();
            var value = optionObject["value"]?.GetValue<string>();
            if (string.Equals(label, "40", StringComparison.OrdinalIgnoreCase)
                && string.Equals(value, "ft", StringComparison.OrdinalIgnoreCase))
            {
                optionObject["label"] = "40ft";
                optionObject["value"] = "40ft";
                changed = true;
            }
        }

        return changed;
    }
}
