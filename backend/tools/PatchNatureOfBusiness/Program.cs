using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

const long formTemplateId = 3;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddDbContext<AgriCheckDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AgriCheckDbContext>();

var now = DateTime.UtcNow;
var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

if (!await db.FormTemplateVersions.AnyAsync(v => v.TemplateId == formTemplateId && v.VersionNumber == 28))
{
    var sourceVersion = await db.FormTemplateVersions
        .Where(v => v.TemplateId == formTemplateId)
        .OrderByDescending(v => v.VersionNumber)
        .FirstAsync();

    var schemaJson = sourceVersion.SchemaJson.Replace("\\n", "\n", StringComparison.Ordinal);
    var fields = JsonNode.Parse(schemaJson)?.AsArray()
        ?? throw new InvalidOperationException("Unable to parse accreditation form schema.");

    if (!fields.Any(field => field?["name"]?.GetValue<string>() == "nature_of_business"))
    {
        foreach (var field in fields)
        {
            if (field is null)
            {
                continue;
            }

            var displayOrder = field["displayOrder"]?.GetValue<int?>() ?? 0;
            if (displayOrder >= 9)
            {
                field["displayOrder"] = displayOrder + 1;
            }
        }

        fields.Add(JsonNode.Parse("""
            {
              "id": "8c30c86d-e6d1-414f-8b3c-20bebb2dfcea",
              "name": "nature_of_business",
              "label": "Nature of Business",
              "type": "text",
              "required": true,
              "placeholder": "e.g. Importation of agricultural products",
              "helpText": "Primary business activity or line of business",
              "columnWidth": 12,
              "displayOrder": 9
            }
            """));
    }

    db.FormTemplateVersions.Add(new FormTemplateVersion
    {
        TemplateId = formTemplateId,
        VersionNumber = 28,
        SchemaJson = fields.ToJsonString(jsonOptions),
        IsPublished = true,
        CreatedAt = now,
        UpdatedAt = now,
    });
    Console.WriteLine("Created accreditation form version 28 with nature_of_business.");
}
else
{
    Console.WriteLine("Accreditation form version 28 already exists.");
}

await db.SaveChangesAsync();
Console.WriteLine("Patch complete.");
