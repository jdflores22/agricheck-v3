using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
services.AddSingleton<IHostEnvironment>(new MigrationHostEnvironment());

await using var provider = services.BuildServiceProvider();
var db = provider.GetRequiredService<AgriCheckDbContext>();
var environment = provider.GetRequiredService<IHostEnvironment>();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("CertificateMigrationRunner");

await db.Database.MigrateAsync();
await CertificateTemplateMigrationSeeder.MigrateFromV2Async(db, configuration, environment, logger, CancellationToken.None);

var template = await db.CertificateTemplates
    .Include(t => t.Versions).ThenInclude(v => v.Elements)
    .Include(t => t.ProcessAssignments)
    .FirstOrDefaultAsync(t => t.Name == "Accreditation Certificate - Importer");

if (template is null)
{
    Console.WriteLine("Migration finished, but accreditation template was not created.");
    return;
}

var version = template.Versions.FirstOrDefault();
Console.WriteLine(
    $"Template: {template.Name} | elements: {version?.Elements.Count ?? 0} | accreditation assignments: {template.ProcessAssignments.Count}");

internal sealed class MigrationHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "CertificateMigrationRunner";
    public string ContentRootPath { get; set; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AgriCheck.Api"));
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
