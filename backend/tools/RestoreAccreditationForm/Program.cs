using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

const string templateName = "Accreditation Form";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
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
var schemaJson = AccreditationFormSchema.Json.Trim();

ValidateSchema(schemaJson);

var template = await db.FormTemplates
    .Include(t => t.Versions)
    .FirstOrDefaultAsync(t => t.FormType == "ACCREDITATION" && t.Name == templateName)
    ?? await db.FormTemplates
        .Include(t => t.Versions)
        .OrderByDescending(t => t.Versions.Count)
        .FirstOrDefaultAsync(t => t.FormType == "ACCREDITATION");

if (template is null)
{
    template = new FormTemplate
    {
        Uuid = Guid.NewGuid(),
        Name = templateName,
        FormType = "ACCREDITATION",
        Status = FormTemplateStatus.Published,
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now,
    };
    db.FormTemplates.Add(template);
    await db.SaveChangesAsync();
}

var emptyDuplicates = await db.FormTemplates
    .Include(t => t.Versions)
    .Include(t => t.AgencyTags)
    .Where(t => t.FormType == "ACCREDITATION" && t.Id != template.Id)
    .ToListAsync();

foreach (var duplicate in emptyDuplicates)
{
    db.FormAgencyTags.RemoveRange(duplicate.AgencyTags);
    db.FormTemplateVersions.RemoveRange(duplicate.Versions);
    db.FormTemplates.Remove(duplicate);
    Console.WriteLine($"Removed duplicate accreditation template: {duplicate.Name} ({duplicate.Uuid})");
}

foreach (var version in template.Versions)
{
    version.IsPublished = false;
    version.UpdatedAt = now;
}

var nextVersion = template.Versions.Count > 0 ? template.Versions.Max(v => v.VersionNumber) + 1 : 1;
db.FormTemplateVersions.Add(new FormTemplateVersion
{
    TemplateId = template.Id,
    VersionNumber = nextVersion,
    SchemaJson = schemaJson,
    IsPublished = true,
    CreatedAt = now,
    UpdatedAt = now,
});

template.Name = templateName;
template.Status = FormTemplateStatus.Published;
template.IsActive = true;
template.UpdatedAt = now;

await db.SaveChangesAsync();

Console.WriteLine($"Restored accreditation form '{template.Name}' ({template.Uuid}) as published version {nextVersion} with {CountFields(schemaJson)} fields.");

static void ValidateSchema(string schemaJson)
{
    using var doc = JsonDocument.Parse(schemaJson);
    if (doc.RootElement.ValueKind != JsonValueKind.Array)
    {
        throw new InvalidOperationException("Accreditation schema must be a JSON array.");
    }

    foreach (var field in doc.RootElement.EnumerateArray())
    {
        if (field.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString()))
        {
            continue;
        }

        throw new InvalidOperationException("Accreditation schema contains a field without a name.");
    }
}

static int CountFields(string schemaJson)
{
    using var doc = JsonDocument.Parse(schemaJson);
    return doc.RootElement.GetArrayLength();
}
