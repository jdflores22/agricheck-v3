using AgriCheck.Infrastructure.Auth;
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
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException(
        "Set ConnectionStrings__DefaultConnection or appsettings DefaultConnection.");

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddSingleton<IHostEnvironment>(new SeederHostEnvironment());
services.AddSingleton<IPasswordService, PasswordService>();
services.AddDbContext<AgriCheckDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
services.AddSingleton<DatabaseSeeder>(sp =>
    new DatabaseSeeder(sp, sp.GetRequiredService<ILogger<DatabaseSeeder>>()));

await using var provider = services.BuildServiceProvider();
var db = provider.GetRequiredService<AgriCheckDbContext>();
var seeder = provider.GetRequiredService<DatabaseSeeder>();

Console.WriteLine("Applying migrations...");
await db.Database.MigrateAsync();
await DriverRegistrationSchemaSeeder.EnsureAsync(db);
Console.WriteLine("Seeding demo accounts...");
await seeder.SeedDemoAccountsAsync();
Console.WriteLine("Done.");

internal sealed class SeederHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "DemoUserSeederRunner";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
