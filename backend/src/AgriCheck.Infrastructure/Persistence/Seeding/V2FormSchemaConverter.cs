using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class V2FormSchemaConverter
{
    public static string ConvertFieldsToSchemaJson(JsonElement fieldsProp)
    {
        var fields = new List<V2FormField>();
        var order = 0;

        foreach (var field in fieldsProp.EnumerateArray())
        {
            fields.Add(ParseFieldElement(field, order));
            order++;
        }

        return ConvertFieldsToSchemaJson(fields);
    }

    public static string ConvertFieldsToSchemaJson(IReadOnlyList<V2FormField> fields)
    {
        var schema = new JsonArray();

        foreach (var field in fields.OrderBy(f => f.DisplayOrder))
        {
            schema.Add(BuildFieldNode(field));
        }

        return schema.ToJsonString();
    }

    private static V2FormField ParseFieldElement(JsonElement field, int fallbackOrder)
    {
        var name = field.TryGetProperty("field_name", out var nameProp) ? nameProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"field_{fallbackOrder + 1}";
        }

        var label = field.TryGetProperty("field_label", out var labelProp) ? labelProp.GetString() : name;
        var type = field.TryGetProperty("field_type", out var typeProp)
            ? typeProp.GetString()?.Trim().ToLowerInvariant()
            : "text";

        return new V2FormField(
            name,
            label ?? name,
            string.IsNullOrWhiteSpace(type) ? "text" : type,
            field.TryGetProperty("placeholder", out var placeholderProp) && placeholderProp.ValueKind == JsonValueKind.String
                ? placeholderProp.GetString()
                : null,
            field.TryGetProperty("help_text", out var helpProp) && helpProp.ValueKind == JsonValueKind.String
                ? helpProp.GetString()
                : null,
            field.TryGetProperty("tooltip", out var tooltipProp) && tooltipProp.ValueKind == JsonValueKind.String
                ? tooltipProp.GetString()
                : null,
            field.TryGetProperty("is_required", out var requiredProp) && requiredProp.ValueKind == JsonValueKind.True,
            field.TryGetProperty("display_order", out var orderProp) ? orderProp.GetInt32() : fallbackOrder,
            field.TryGetProperty("column_width", out var widthProp) && widthProp.TryGetInt32(out var width) ? width : 12,
            field.TryGetProperty("validation_rules", out var rulesProp) && rulesProp.ValueKind == JsonValueKind.Object
                ? rulesProp.GetRawText()
                : null,
            field.TryGetProperty("conditional_logic", out var logicProp) && logicProp.ValueKind == JsonValueKind.Object
                ? logicProp.GetRawText()
                : null,
            field.TryGetProperty("options", out var optionsProp) && optionsProp.ValueKind == JsonValueKind.Array
                ? optionsProp.GetRawText()
                : null);
    }

    private static JsonObject BuildFieldNode(V2FormField field)
    {
        var node = new JsonObject
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["name"] = field.FieldName,
            ["label"] = field.FieldLabel,
            ["type"] = field.FieldType,
            ["displayOrder"] = field.DisplayOrder,
            ["columnWidth"] = field.ColumnWidth,
        };

        if (field.IsRequired)
        {
            node["required"] = true;
        }

        if (!string.IsNullOrWhiteSpace(field.Placeholder))
        {
            node["placeholder"] = field.Placeholder;
        }

        if (!string.IsNullOrWhiteSpace(field.HelpText))
        {
            node["helpText"] = field.HelpText;
        }

        if (!string.IsNullOrWhiteSpace(field.Tooltip))
        {
            node["tooltip"] = field.Tooltip;
        }

        if (!string.IsNullOrWhiteSpace(field.OptionsJson))
        {
            node["options"] = JsonNode.Parse(field.OptionsJson);
        }

        if (!string.IsNullOrWhiteSpace(field.ValidationRulesJson))
        {
            node["validationRules"] = JsonNode.Parse(field.ValidationRulesJson);
        }

        var conditionalLogic = NormalizeConditionalLogicJson(field.ConditionalLogicJson);
        if (conditionalLogic is not null)
        {
            node["conditionalLogic"] = conditionalLogic;
        }

        return node;
    }

    private static JsonObject? NormalizeConditionalLogicJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            if (JsonNode.Parse(json) is not JsonObject source)
            {
                return null;
            }

            var result = new JsonObject();
            if (source.TryGetPropertyValue("show_if", out var showIfSnake))
            {
                result["showIf"] = NormalizeShowIfNode(showIfSnake);
            }
            else if (source.TryGetPropertyValue("showIf", out var showIfCamel))
            {
                result["showIf"] = NormalizeShowIfNode(showIfCamel);
            }

            return result.Count > 0 ? result : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonNode? NormalizeShowIfNode(JsonNode? node)
    {
        if (node is not JsonObject source)
        {
            return node;
        }

        var result = new JsonObject();
        foreach (var property in source)
        {
            result[property.Key] = property.Value is null ? null : JsonNode.Parse(property.Value.ToJsonString());
        }

        return result;
    }
}

public sealed record V2FormField(
    string FieldName,
    string FieldLabel,
    string FieldType,
    string? Placeholder,
    string? HelpText,
    string? Tooltip,
    bool IsRequired,
    int DisplayOrder,
    int ColumnWidth,
    string? ValidationRulesJson,
    string? ConditionalLogicJson,
    string? OptionsJson);
