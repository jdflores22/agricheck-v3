using AgriCheck.Application.AgencyPortal;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const string defaultEmail = "importer@agricheck.local";
var targetEmail = args.Length > 0 ? args[0] : defaultEmail;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

var apiRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "AgriCheck.Api"));

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<IHostEnvironment>(new ToolHostEnvironment(apiRoot));
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddDbContext<AgriCheckDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
services.AddSingleton<IFileStorageService, LocalFileStorageService>();
services.AddSingleton<INotificationService, ToolNotificationService>();
services.AddScoped<IAccreditationCertificateService, AccreditationCertificateService>();

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();

var db = scope.ServiceProvider.GetRequiredService<AgriCheckDbContext>();
var certificateService = scope.ServiceProvider.GetRequiredService<IAccreditationCertificateService>();

var user = await db.Users.FirstOrDefaultAsync(u => u.Email == targetEmail)
    ?? throw new InvalidOperationException($"User not found: {targetEmail}");

var submission = await db.AccreditationSubmissions
    .Where(s => s.UserId == user.Id && s.Status == AccreditationSubmissionStatus.Approved)
    .OrderByDescending(s => s.CreatedAt)
    .FirstOrDefaultAsync()
    ?? throw new InvalidOperationException($"No approved accreditation submission found for {targetEmail}");

var issuer = await db.Users.FirstAsync(u => u.Email == "admin@agricheck.local");

Console.WriteLine($"Regenerating certificate for {targetEmail}");
Console.WriteLine($"Submission: {submission.Uuid} ({submission.CompanyName})");

var result = await certificateService.RegenerateForApprovedSubmissionAsync(
    submission.Uuid,
    issuer.Id,
    CancellationToken.None);

Console.WriteLine($"Issued: {result.Issued}");
Console.WriteLine($"Certificate: {result.CertificateNumber} ({result.CertificateUuid})");
Console.WriteLine($"Message: {result.Message}");

internal sealed class ToolHostEnvironment : IHostEnvironment
{
    public ToolHostEnvironment(string contentRootPath)
    {
        ContentRootPath = contentRootPath;
        ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
    }

    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "RegenerateAccreditationCertificate";
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
}

internal sealed class ToolNotificationService : INotificationService
{
    public Task NotifyAsync(long userId, string type, string title, string message, string? relatedEntityType = null, string? relatedEntityUuid = null, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<NotificationDto>> ListAsync(int limit, bool unreadOnly, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());

    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task MarkReadAsync(Guid uuid, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task MarkAllReadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<NotificationPreferenceDto> GetPreferencesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new NotificationPreferenceDto(true, true));

    public Task<NotificationPreferenceDto> UpdatePreferencesAsync(UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new NotificationPreferenceDto(request.EmailEnabled, request.InAppEnabled));
}
