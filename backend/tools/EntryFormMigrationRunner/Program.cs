using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var force = args.Contains("--force", StringComparer.OrdinalIgnoreCase);

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder => builder.AddConsole());
services.AddDbContext<AgriCheckDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

await using var provider = services.BuildServiceProvider();
var db = provider.GetRequiredService<AgriCheckDbContext>();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("EntryFormMigrationRunner");

await db.Database.MigrateAsync();
await EntryFormMigrationSeeder.MigrateFromV2Async(db, configuration, logger, force, CancellationToken.None);

var templates = await db.FormTemplates
    .AsNoTracking()
    .Include(t => t.Versions)
    .Include(t => t.AgencyTags)
    .ThenInclude(tag => tag.Agency)
    .Where(t => t.FormType == "ENTRY")
    .OrderBy(t => t.Name)
    .ToListAsync();

foreach (var template in templates)
{
    var published = template.Versions
        .Where(v => v.IsPublished)
        .OrderByDescending(v => v.VersionNumber)
        .FirstOrDefault();

    var fieldCount = 0;
    if (published?.SchemaJson is not null)
    {
        try
        {
            fieldCount = System.Text.Json.JsonDocument.Parse(published.SchemaJson).RootElement.GetArrayLength();
        }
        catch
        {
            fieldCount = 0;
        }
    }

    var agencies = string.Join(", ", template.AgencyTags.Select(tag => tag.Agency.Code));
    Console.WriteLine(
        $"{template.Name} | status={template.Status} | v{published?.VersionNumber ?? 0} | fields={fieldCount} | agencies={agencies}");
}

internal sealed class MigrationHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "EntryFormMigrationRunner";
    public string ContentRootPath { get; set; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AgriCheck.Api"));
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
