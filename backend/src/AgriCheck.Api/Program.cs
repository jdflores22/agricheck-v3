using System.Text;
using AgriCheck.Application;
using AgriCheck.Application.Auth;
using AgriCheck.Application.Notifications;
using AgriCheck.Api.Hubs;
using AgriCheck.Api.Services;
using AgriCheck.Infrastructure;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var listenPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(listenPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{listenPort}");
}

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt settings are not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var dataProtectionRoot = AgriCheck.Infrastructure.Services.UploadStorage.ResolveRoot(builder.Configuration, builder.Environment);
var dataProtectionKeys = Path.Combine(dataProtectionRoot, ".aspnet", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeys);
builder.Services.AddDataProtection()
    .SetApplicationName("AgriCheckV3")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeys));

builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgriCheck V3 API",
        Version = "v1",
        Description = "AgriCheck V3 — ASP.NET Core Web API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks();

var corsOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
    .Concat(new[]
    {
        builder.Configuration["App:PublicBaseUrl"],
        "https://dimgrey-hummingbird-677957.hostingersite.com",
        "http://localhost:5173"
    })
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

app.UseCors("Frontend");

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin)
            && corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.Headers.AccessControlAllowOrigin = origin;
            context.Response.Headers.AccessControlAllowCredentials = "true";
        }

        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var cancelled = error is OperationCanceledException || context.RequestAborted.IsCancellationRequested;
        context.Response.StatusCode = cancelled
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            errors = new[]
            {
                new
                {
                    code = cancelled ? "REQUEST_CANCELLED" : "SERVER_ERROR",
                    message = cancelled
                        ? "The request was cancelled."
                        : error?.GetBaseException().Message ?? "Unexpected error."
                }
            }
        });
    });
});

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AgriCheck V3 API v1");
    });
}

var uploadsRoot = AgriCheck.Infrastructure.Services.UploadStorage.ResolveRoot(app.Configuration, app.Environment);
Directory.CreateDirectory(Path.Combine(uploadsRoot, "system"));
Directory.CreateDirectory(Path.Combine(uploadsRoot, "agency-logos"));
Directory.CreateDirectory(Path.Combine(uploadsRoot, "certificate-templates"));
AgriCheck.Infrastructure.Services.UploadStorage.SeedBundledCertificateTemplateAssets(uploadsRoot);

app.Logger.LogInformation("Serving uploads from {UploadsRoot}", uploadsRoot);

MapPublicUploads(Path.Combine(uploadsRoot, "certificate-templates"), "/uploads/certificates");
MapPublicUploads(Path.Combine(uploadsRoot, "system"), "/uploads/system");
MapPublicUploads(Path.Combine(uploadsRoot, "agency-logos"), "/uploads/agency-logos");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Redirect("/swagger"));

await EnsureDatabaseReadyAsync(app);

app.Run();

static async Task EnsureDatabaseReadyAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AgriCheckDbContext>();
    var logger = app.Logger;

    try
    {
        await DriverRegistrationSchemaSeeder.EnsureAsync(db);
        logger.LogInformation("Driver registration schema ensured.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Driver registration schema ensure failed.");
    }

    try
    {
        await EntrySchemaSeeder.EnsureAsync(db);
        logger.LogInformation("Entry schema ensured.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Entry schema ensure failed.");
    }

    try
    {
        await BillingSchemaSeeder.EnsureAsync(db);
        logger.LogInformation("Billing schema ensured.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Billing schema ensure failed.");
    }

    try
    {
        await WarehouseProfilingSchemaSeeder.EnsureAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Warehouse profiling schema ensure failed.");
    }

    try
    {
        var pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            logger.LogWarning("Applying pending migrations: {Migrations}", string.Join(", ", pending));
        }

        await db.Database.MigrateAsync();
        logger.LogInformation("EF migrations applied.");

        await BillingSchemaSeeder.EnsureAsync(db);
        logger.LogInformation("Billing schema re-checked after migrations.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "EF migration apply failed.");
    }

    try
    {
        await OperatorInviteCodeSeeder.EnsureSeedDataAsync(db, logger);
        logger.LogInformation("Operator invite seed data ensured.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Operator invite seed failed.");
    }

    try
    {
        var passwordService = scope.ServiceProvider.GetRequiredService<AgriCheck.Infrastructure.Auth.IPasswordService>();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedDemoAccountsAsync(db, passwordService);
        logger.LogInformation("Demo accounts ensured.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Demo account seed failed.");
    }
}

void MapPublicUploads(string physicalPath, string requestPath)
{
    Directory.CreateDirectory(physicalPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(physicalPath),
        RequestPath = requestPath,
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.CacheControl = "public,max-age=86400";
        }
    });
}
