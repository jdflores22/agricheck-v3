using System.Text.Json;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public static class CertificateTemplateMigrationSeeder
{
    private const string TemplateName = "Accreditation Certificate - Importer";
    private const int V2TemplateId = 1;

    public static async Task MigrateFromV2Async(
        AgriCheckDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.CertificateTemplates.AnyAsync(t => t.Name == TemplateName, cancellationToken))
        {
            logger.LogInformation("Certificate template {Name} already present; skipping V2 migration.", TemplateName);
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
            logger.LogWarning(ex, "Unable to connect to V2 database for certificate template migration.");
            return;
        }

        var templateMeta = await ReadTemplateMetaAsync(connection, cancellationToken);
        if (templateMeta is null)
        {
            logger.LogWarning("V2 certificate template id {Id} not found.", V2TemplateId);
            return;
        }

        var elements = await ReadElementsAsync(connection, cancellationToken);
        if (elements.Count == 0)
        {
            logger.LogWarning("V2 certificate template id {Id} has no elements.", V2TemplateId);
            return;
        }

        var v2PublicRoot = ResolveV2PublicRoot(configuration, environment);
        var assetFolder = Path.Combine(environment.ContentRootPath, "storage", "certificate-templates", "assets");
        Directory.CreateDirectory(assetFolder);

        var layoutJson = JsonSerializer.Serialize(new
        {
            paperSize = templateMeta.PaperSize,
            orientation = templateMeta.Orientation,
            marginTop = templateMeta.MarginTop,
            marginRight = templateMeta.MarginRight,
            marginBottom = templateMeta.MarginBottom,
            marginLeft = templateMeta.MarginLeft,
            backgroundColor = templateMeta.BackgroundColor,
            backgroundImage = templateMeta.BackgroundImage,
            source = "agricheck-v2",
            v2TemplateId = V2TemplateId,
        });

        var template = new CertificateTemplate
        {
            Uuid = Guid.NewGuid(),
            Name = templateMeta.Name,
            Description = templateMeta.Description,
            AgencyId = null,
            IsActive = templateMeta.IsActive,
            Versions =
            {
                new CertificateTemplateVersion
                {
                    VersionNumber = 1,
                    IsPublished = true,
                    LayoutJson = layoutJson,
                },
            },
        };

        db.CertificateTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);

        var version = template.Versions.First();
        var sortOrder = 1;
        foreach (var element in elements.OrderBy(e => e.DisplayOrder))
        {
            var config = BuildElementConfig(element, v2PublicRoot, assetFolder, logger);
            version.Elements.Add(new CertificateElement
            {
                ElementType = MapElementType(element.ElementType),
                Label = BuildElementLabel(element),
                ConfigJson = JsonSerializer.Serialize(config),
                SortOrder = sortOrder++,
            });
        }

        var agencies = await db.Agencies.Where(a => a.IsActive).ToListAsync(cancellationToken);
        foreach (var agency in agencies)
        {
            template.ProcessAssignments.Add(new CertificateProcessAssignment
            {
                AgencyId = agency.Id,
                TemplateId = template.Id,
                ProcessType = CertificateProcessType.Accreditation,
                IsActive = true,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Migrated V2 certificate template {Name} with {ElementCount} elements and accreditation assignments for {AgencyCount} agencies.",
            TemplateName,
            elements.Count,
            agencies.Count);
    }

    private static string? ResolveV2PublicRoot(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["V2:PublicRoot"];
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
        {
            return configured;
        }

        var sibling = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "..", "..", "agricheck", "public"));
        return Directory.Exists(sibling) ? sibling : null;
    }

    private static async Task<V2TemplateMeta?> ReadTemplateMetaAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name, description, paper_size, orientation, margin_top, margin_right, margin_bottom, margin_left,
                   background_color, background_image, is_active
            FROM certificate_templates
            WHERE id = @id
            """;
        command.Parameters.AddWithValue("@id", V2TemplateId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new V2TemplateMeta(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            reader.GetInt32(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.GetBoolean(10));
    }

    private static async Task<List<V2Element>> ReadElementsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        var elements = new List<V2Element>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT element_type, content, x_position, y_position, width, height, font_size, font_weight, font_style,
                   text_align, text_color, z_index, display_order, image_path, properties
            FROM certificate_elements
            WHERE template_id = @id
            ORDER BY display_order
            """;
        command.Parameters.AddWithValue("@id", V2TemplateId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            elements.Add(new V2Element(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetInt32(5),
                reader.IsDBNull(6) ? null : reader.GetInt32(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.GetInt32(11),
                reader.GetInt32(12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetString(14)));
        }

        return elements;
    }

    private static CertificateElementType MapElementType(string elementType) =>
        elementType.ToUpperInvariant() switch
        {
            "TEXT" => CertificateElementType.Text,
            "QR_CODE" => CertificateElementType.QrCode,
            "IMAGE" => CertificateElementType.Image,
            "SHAPE" => CertificateElementType.Shape,
            "LINE" => CertificateElementType.Line,
            _ => CertificateElementType.Text,
        };

    private static string BuildElementLabel(V2Element element)
    {
        if (!string.IsNullOrWhiteSpace(element.Content))
        {
            var trimmed = element.Content.Trim();
            return trimmed.Length <= 64 ? trimmed : $"{trimmed[..61]}...";
        }

        return element.ElementType switch
        {
            "QR_CODE" => "Verification QR",
            "IMAGE" => "Image",
            "SHAPE" => "Shape",
            "LINE" => "Line",
            _ => "Element",
        };
    }

    private static Dictionary<string, object?> BuildElementConfig(
        V2Element element,
        string? v2PublicRoot,
        string assetFolder,
        ILogger logger)
    {
        string? storedImagePath = null;
        if (!string.IsNullOrWhiteSpace(element.ImagePath) && v2PublicRoot is not null)
        {
            var sourcePath = Path.Combine(v2PublicRoot, element.ImagePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(sourcePath))
            {
                var fileName = Path.GetFileName(sourcePath);
                var targetPath = Path.Combine(assetFolder, fileName);
                if (!File.Exists(targetPath))
                {
                    File.Copy(sourcePath, targetPath);
                }

                storedImagePath = $"certificate-templates/assets/{fileName}";
            }
            else
            {
                logger.LogWarning("Missing V2 certificate image at {Path}", sourcePath);
            }
        }

        return new Dictionary<string, object?>
        {
            ["content"] = element.Content,
            ["x"] = element.X,
            ["y"] = element.Y,
            ["width"] = element.Width,
            ["height"] = element.Height,
            ["fontSize"] = element.FontSize,
            ["fontWeight"] = element.FontWeight,
            ["fontStyle"] = element.FontStyle,
            ["textAlign"] = element.TextAlign,
            ["textColor"] = element.TextColor,
            ["zIndex"] = element.ZIndex,
            ["displayOrder"] = element.DisplayOrder,
            ["imagePath"] = storedImagePath ?? element.ImagePath,
            ["properties"] = element.Properties,
        };
    }

    private sealed record V2TemplateMeta(
        string Name,
        string? Description,
        string PaperSize,
        string Orientation,
        int MarginTop,
        int MarginRight,
        int MarginBottom,
        int MarginLeft,
        string? BackgroundColor,
        string? BackgroundImage,
        bool IsActive);

    private sealed record V2Element(
        string ElementType,
        string? Content,
        int X,
        int Y,
        int? Width,
        int? Height,
        int? FontSize,
        string? FontWeight,
        string? FontStyle,
        string? TextAlign,
        string? TextColor,
        int ZIndex,
        int DisplayOrder,
        string? ImagePath,
        string? Properties);
}
