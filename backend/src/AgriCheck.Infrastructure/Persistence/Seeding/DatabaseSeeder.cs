using AgriCheck.Application.AdminPortal;
using AgriCheck.Application.Auth;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Auth;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgriCheck.Infrastructure.Persistence.Seeding;

public class DatabaseSeeder : IHostedService
{
    private const string LegacyBaiEntrySchema = """[{"name":"commodityName","label":"Commodity","type":"text","required":true},{"name":"quantity","label":"Quantity","type":"number","required":true},{"name":"originCountry","label":"Origin Country","type":"text","required":true},{"name":"mav_no","label":"MAV No.","type":"text","required":true},{"name":"mav_certificate","label":"MAV Certificate","type":"file","required":true,"accept":".pdf"}]""";
    private const string ConnectedBaiEntrySchema = """[{"name":"commodityName","label":"Commodity","type":"commodity","required":true,"helpText":"Required automatically when MAV is active for this agency."},{"name":"quantity","label":"Quantity","type":"number","required":true},{"name":"originCountry","label":"Origin Country","type":"text","required":true},{"name":"mav_no","label":"MAV No.","type":"text","required":false},{"name":"mav_certificate","label":"MAV Certificate","type":"file","required":false,"accept":".pdf"}]""";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => SeedAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgriCheckDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        try
        {
            await db.Database.MigrateAsync(cancellationToken);
            await WarehouseProfilingSchemaSeeder.EnsureAsync(db, cancellationToken);
            await SeedRolesAsync(db, cancellationToken);
            await SeedSystemSettingsAsync(db, cancellationToken);
            await SeedAgenciesAsync(db, cancellationToken);
            await SeedAdminUserAsync(db, passwordService, cancellationToken);
            await SeedReferenceDataAsync(db, cancellationToken);
            await PhilippineAddressSeeder.SeedAsync(db, cancellationToken);
            await WarehouseFacilitySeeder.SeedAsync(db, cancellationToken);
            await SeedDemoClientUserAsync(db, passwordService, cancellationToken);
            await SeedAgencyStaffAsync(db, passwordService, cancellationToken);
            await SyncDaAccreditationOfficersAsync(db, passwordService, cancellationToken);
            await SyncDaLeadershipAsync(db, passwordService, cancellationToken);
            await SeedAdminPortalDataAsync(db, cancellationToken);
            if (ShouldImportFromV2(configuration, environment))
            {
                await CertificateTemplateMigrationSeeder.MigrateFromV2Async(db, configuration, environment, _logger, cancellationToken);
                await CertificateTemplateMigrationSeeder.MigrateEntryTemplatesFromV2Async(db, configuration, environment, _logger, cancellationToken);
                await EntryFormMigrationSeeder.MigrateFromV2Async(db, configuration, _logger, force: false, cancellationToken);
                await ContainerFormMigrationSeeder.MigrateFromV2Async(db, configuration, _logger, force: false, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Skipping V2 database imports in this environment.");
            }
            await ContainerFormSchemaSeeder.EnsureContainerTypeFieldAsync(db, _logger, cancellationToken);
            await ContainerFormSchemaSeeder.EnsureWarehouseNameFieldAsync(db, _logger, cancellationToken);
            await SeedMavStaffAsync(db, passwordService, cancellationToken);
            await SeedMavDemoDataAsync(db, cancellationToken);
            await SeedOpsStaffAsync(db, passwordService, cancellationToken);
            await WorkflowDemoSeeder.SeedAsync(db, _logger, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migrate/seed failed. API will still listen.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool ShouldImportFromV2(IConfiguration configuration, IHostEnvironment environment)
    {
        var v2 = configuration.GetConnectionString("V2Connection");
        if (string.IsNullOrWhiteSpace(v2))
        {
            return false;
        }

        if (!environment.IsProduction())
        {
            return true;
        }

        return !v2.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            && !v2.Contains("127.0.0.1");
    }

    private static async Task SeedRolesAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        foreach (var (code, name, description) in RoleDefinitions.All)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
            if (role is null)
            {
                db.Roles.Add(new Role { Code = code, Name = name, Description = description });
                continue;
            }

            role.Name = name;
            role.Description = description;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSystemSettingsAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var defaults = new Dictionary<string, (string Value, string Description)>
        {
            ["system_name"] = ("AgriCheck System", "Application display name"),
            ["system_description"] = ("", "Short system description"),
            ["contact_email"] = ("", "Primary contact email"),
            ["support_phone"] = ("", "Support phone number"),
            ["timezone"] = ("Asia/Manila", "Default timezone"),
            ["date_format"] = ("Y-m-d", "Default date format"),
            ["primary_color"] = ("#166534", "Primary brand color"),
            ["secondary_color"] = ("#4caf50", "Secondary brand color"),
            ["footer_text"] = ("© 2025 Department of Agriculture. All rights reserved.", "Footer text"),
            ["spinner_color"] = ("#166534", "Loading spinner color"),
            ["system_logo_path"] = ("", "Main header logo path"),
            ["spinner_logo_path"] = ("", "Spinner center logo path"),
            ["favicon_path"] = ("", "Browser favicon path"),
            ["smtp_host"] = ("", "SMTP host"),
            ["smtp_port"] = ("587", "SMTP port"),
            ["smtp_encryption"] = ("tls", "SMTP encryption"),
            ["smtp_username"] = ("", "SMTP username"),
            ["from_email"] = ("", "Default from email"),
            ["from_name"] = ("AgriCheck System", "Default from name"),
            ["max_file_size"] = ("10", "Max upload size in MB"),
            ["allowed_file_types"] = ("pdf,jpg,jpeg,png,doc,docx,xls,xlsx", "Allowed file extensions"),
            ["file_retention_days"] = ("365", "File retention in days"),
            ["session_timeout"] = ("60", "Session timeout in minutes"),
            ["max_login_attempts"] = ("5", "Max failed login attempts"),
            ["min_password_length"] = ("8", "Minimum password length"),
            ["require_uppercase"] = ("1", "Require uppercase in passwords"),
            ["require_numbers"] = ("1", "Require numbers in passwords"),
            ["require_special_chars"] = ("0", "Require special characters in passwords"),
            ["enable_email_notifications"] = ("1", "Enable email notifications"),
            ["notify_on_submission"] = ("1", "Notify on entry submission"),
            ["notify_on_approval"] = ("1", "Notify on approval"),
            ["enable_inapp_notifications"] = ("1", "Enable in-app notifications"),
            ["autosave_interval"] = ("60", "Form autosave interval in seconds"),
            ["required_indicator"] = ("*", "Required field indicator"),
            ["accreditation_validity_days"] = ("365", "Accreditation validity in days"),
            ["renewal_reminder_days"] = ("60", "Renewal reminder lead time"),
            ["compliance_deadline_days"] = ("30", "Compliance deadline in days"),
            ["maintenance_mode"] = ("0", "Maintenance mode enabled"),
            ["maintenance_message"] = ("System is currently under maintenance. Please check back later.", "Maintenance message"),
            ["maintenance_allowed_ips"] = ("", "IPs allowed during maintenance"),
            ["paymongo_enabled"] = ("0", "Enable PayMongo live payments"),
            ["paymongo_api_key"] = ("", "PayMongo secret API key"),
            ["paymongo_webhook_secret"] = ("", "PayMongo webhook signing secret"),
            ["paymongo_public_key"] = ("", "PayMongo public key"),
            ["entry_processing_fee_import"] = ("2500", "Global entry processing fee for Import"),
            ["entry_processing_fee_export"] = ("2500", "Global entry processing fee for Export"),
            ["entry_processing_fee_currency"] = ("PHP", "Entry processing fee currency"),
        };

        foreach (var (key, (value, description)) in defaults)
        {
            if (await db.SystemSettings.AnyAsync(s => s.SettingKey == key, cancellationToken)) continue;
            db.SystemSettings.Add(new SystemSetting
            {
                SettingKey = key,
                SettingValue = value,
                Description = description
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAgenciesAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var da = await db.Agencies.FirstOrDefaultAsync(a => a.Code == "DA", cancellationToken);
        if (da is null)
        {
            da = new Agency
            {
                Code = "DA",
                Name = "Department of Agriculture",
                Description = "Main department overseeing all agricultural bureaus and agencies",
                IsActive = true,
            };
            db.Agencies.Add(da);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            da.Name = "Department of Agriculture";
            da.Description = "Main department overseeing all agricultural bureaus and agencies";
            da.ParentId = null;
            da.IsActive = true;
        }

        var childAgencies = new (string Code, string Name, string? Description)[]
        {
            ("BAI", "Bureau of Animal Industry", "Responsible for livestock and poultry regulation, including MAV management"),
            ("BFAR", "Bureau of Fisheries and Aquatic Resources", "Responsible for fisheries and aquatic product imports/exports"),
            ("BPI", "Bureau of Plant Industry", "Responsible for plant and plant product imports/exports"),
            ("SRA", "Sugar Regulatory Administration", "Administers MAV allocation and regulation for sugar (HS 1701)"),
            ("MAV", "MAV Management Committee", "Division responsible for Minimum Access Volume (MAV) administration, licensing, and monitoring"),
            ("NTA", "National Tobacco Administration", "Regulates tobacco industry and related imports"),
        };

        foreach (var (code, name, description) in childAgencies)
        {
            var agency = await db.Agencies.FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
            if (agency is null)
            {
                db.Agencies.Add(new Agency
                {
                    Code = code,
                    Name = name,
                    Description = description,
                    ParentId = da.Id,
                    IsActive = true,
                });
                continue;
            }

            agency.Name = name;
            if (!string.IsNullOrWhiteSpace(description))
            {
                agency.Description = description;
            }
            agency.ParentId = da.Id;
            agency.IsActive = true;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(AgriCheckDbContext db, IPasswordService passwordService, CancellationToken cancellationToken)
    {
        const string adminEmail = "admin@agricheck.local";
        if (await db.Users.AnyAsync(u => u.Email == adminEmail, cancellationToken)) return;

        var adminRole = await db.Roles.FirstAsync(r => r.Code == "ROLE_ADMIN", cancellationToken);

        var admin = new User
        {
            Uuid = Guid.NewGuid(),
            Email = adminEmail,
            PasswordHash = passwordService.Hash("Admin@12345"),
            Status = UserStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            Profile = new UserProfile { FirstName = "System", LastName = "Administrator" }
        };

        admin.UserRoles.Add(new UserRole { Role = adminRole, AssignedAt = DateTime.UtcNow });
        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded admin user {Email} with password Admin@12345", adminEmail);
    }

    private static async Task SeedReferenceDataAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.CommodityCategories.AnyAsync(cancellationToken))
        {
            var livestock = new CommodityCategory { Code = "LIVESTOCK", Name = "Livestock & Animal Products" };
            var crops = new CommodityCategory { Code = "CROPS", Name = "Crops & Plant Products" };
            db.CommodityCategories.AddRange(livestock, crops);
            await db.SaveChangesAsync(cancellationToken);

            db.Commodities.AddRange(
                new Commodity { CategoryId = livestock.Id, Code = "BEEF", Name = "Beef Products" },
                new Commodity { CategoryId = livestock.Id, Code = "PORK", Name = "Pork Products" },
                new Commodity { CategoryId = crops.Id, Code = "RICE", Name = "Rice" },
                new Commodity { CategoryId = crops.Id, Code = "CORN", Name = "Corn" });
            await db.SaveChangesAsync(cancellationToken);
        }

        await SeedComplianceChecklistsAsync(db, cancellationToken);
    }

    private static readonly string[] ComplianceChecklistAgencyCodes = { "BAI", "BPI", "BFAR", "SRA", "NTA" };

    private static async Task SeedComplianceChecklistsAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var agencies = await db.Agencies
            .Where(a => a.IsActive && ComplianceChecklistAgencyCodes.Contains(a.Code))
            .ToListAsync(cancellationToken);

        foreach (var agency in agencies)
        {
            if (await db.ComplianceChecklists.AnyAsync(c => c.AgencyId == agency.Id, cancellationToken))
            {
                continue;
            }

            db.ComplianceChecklists.Add(new ComplianceChecklist
            {
                AgencyId = agency.Id,
                Name = "Standard Import Evaluation Checklist",
                IsActive = true,
                Items = new List<ComplianceChecklistItem>
                {
                    new() { Label = "Complete application form", SortOrder = 1, IsRequired = true },
                    new() { Label = "Valid sanitary/phytosanitary certificate attached", SortOrder = 2, IsRequired = true },
                    new() { Label = "Commodity details verified", SortOrder = 3, IsRequired = true },
                    new() { Label = "Processing fee paid", SortOrder = 4, IsRequired = true },
                    new() { Label = "Supporting documents complete", SortOrder = 5, IsRequired = false }
                }
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAgencyStaffAsync(AgriCheckDbContext db, IPasswordService passwordService, CancellationToken cancellationToken)
    {
        var staff = new (string Email, string Password, string RoleCode, string AgencyCode, string First, string Last)[]
        {
            ("evaluator@agricheck.local", "Evaluator@12345", "ROLE_EVALUATOR", "BAI", "BAI", "Evaluator"),
            ("inspector@agricheck.local", "Inspector@12345", "ROLE_INSPECTOR", "BAI", "BAI", "Inspector"),
            ("inspector.bfar@agricheck.local", "Inspector@12345", "ROLE_INSPECTOR", "BFAR", "BFAR", "Inspector"),
            ("inspector.bpi@agricheck.local", "Inspector@12345", "ROLE_INSPECTOR", "BPI", "BPI", "Inspector"),
            ("inspector.sra@agricheck.local", "Inspector@12345", "ROLE_INSPECTOR", "SRA", "SRA", "Inspector"),
            ("inspector.nta@agricheck.local", "Inspector@12345", "ROLE_INSPECTOR", "NTA", "NTA", "Inspector"),
            ("billing@agricheck.local", "Billing@12345", "ROLE_BILLING_AGENT", "BAI", "BAI", "Billing Agent"),
            ("billing.bfar@agricheck.local", "Billing@12345", "ROLE_BILLING_AGENT", "BFAR", "BFAR", "Billing Agent"),
            ("billing.bpi@agricheck.local", "Billing@12345", "ROLE_BILLING_AGENT", "BPI", "BPI", "Billing Agent"),
            ("billing.sra@agricheck.local", "Billing@12345", "ROLE_BILLING_AGENT", "SRA", "SRA", "Billing Agent"),
            ("billing.nta@agricheck.local", "Billing@12345", "ROLE_BILLING_AGENT", "NTA", "NTA", "Billing Agent"),
            ("evaluator.bfar@agricheck.local", "Evaluator@12345", "ROLE_EVALUATOR", "BFAR", "BFAR", "Evaluator"),
            ("evaluator.bpi@agricheck.local", "Evaluator@12345", "ROLE_EVALUATOR", "BPI", "BPI", "Evaluator"),
            ("bai.admin@agricheck.local", "AgencyAdmin@12345", "ROLE_AGENCY_ADMIN", "BAI", "BAI", "Agency Admin"),
            ("bfar.admin@agricheck.local", "AgencyAdmin@12345", "ROLE_AGENCY_ADMIN", "BFAR", "BFAR", "Agency Admin"),
            ("bpi.admin@agricheck.local", "AgencyAdmin@12345", "ROLE_AGENCY_ADMIN", "BPI", "BPI", "Agency Admin"),
            ("sra.admin@agricheck.local", "AgencyAdmin@12345", "ROLE_AGENCY_ADMIN", "SRA", "SRA", "Agency Admin"),
            ("nta.admin@agricheck.local", "AgencyAdmin@12345", "ROLE_AGENCY_ADMIN", "NTA", "NTA", "Agency Admin"),
        };

        foreach (var (email, password, roleCode, agencyCode, first, last) in staff)
        {
            if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken)) continue;

            var agency = await db.Agencies.FirstAsync(a => a.Code == agencyCode, cancellationToken);
            var role = await db.Roles.FirstAsync(r => r.Code == roleCode, cancellationToken);
            var user = new User
            {
                Uuid = Guid.NewGuid(),
                Email = email,
                PasswordHash = passwordService.Hash(password),
                Status = UserStatus.Active,
                EmailVerifiedAt = DateTime.UtcNow,
                Profile = new UserProfile { FirstName = first, LastName = last }
            };
            user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
            user.AgencyMemberships.Add(new AgencyMembership { AgencyId = agency.Id, IsPrimary = true, JoinedAt = DateTime.UtcNow });
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded agency staff {Email} with password {Password}", email, password);
        }
    }

    private async Task SyncDaAccreditationOfficersAsync(
        AgriCheckDbContext db,
        IPasswordService passwordService,
        CancellationToken cancellationToken)
    {
        var officers = new (string Email, string Password, string First, string Last)[]
        {
            ("accred@agricheck.local", "Accred@12345", "DA", "Accreditation Officer"),
            ("daevaluator@agricheck.local", "DaEval@12345", "DA", "Accreditation Evaluator"),
        };

        var role = await db.Roles.FirstAsync(r => r.Code == "ROLE_ACCREDITATION_OFFICER", cancellationToken);

        foreach (var (email, password, first, last) in officers)
        {
            var user = await db.Users
                .Include(u => u.Profile)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.AgencyMemberships)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user is null)
            {
                user = new User
                {
                    Uuid = Guid.NewGuid(),
                    Email = email,
                    PasswordHash = passwordService.Hash(password),
                    Status = UserStatus.Active,
                    EmailVerifiedAt = DateTime.UtcNow,
                    Profile = new UserProfile { FirstName = first, LastName = last }
                };
                user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
                db.Users.Add(user);
                _logger.LogInformation("Seeded DA accreditation officer {Email} with password {Password}", email, password);
                continue;
            }

            user.UserRoles.Clear();
            user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
            user.AgencyMemberships.Clear();

            if (user.Profile is null)
            {
                user.Profile = new UserProfile { FirstName = first, LastName = last };
            }
            else
            {
                user.Profile.FirstName = first;
                user.Profile.LastName = last;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncDaLeadershipAsync(
        AgriCheckDbContext db,
        IPasswordService passwordService,
        CancellationToken cancellationToken)
    {
        var leaders = new (string Email, string Password, string First, string Last, string RoleCode)[]
        {
            ("da.secretary@agricheck.local", "DaSec@12345", "DA", "Secretary", "ROLE_DA_SECRETARY"),
            ("da.undersecretary@agricheck.local", "DaUsec@12345", "DA", "Undersecretary", "ROLE_DA_UNDERSECRETARY"),
        };

        foreach (var (email, password, first, last, roleCode) in leaders)
        {
            var role = await db.Roles.FirstAsync(r => r.Code == roleCode, cancellationToken);
            var user = await db.Users
                .Include(u => u.Profile)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.AgencyMemberships)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user is null)
            {
                user = new User
                {
                    Uuid = Guid.NewGuid(),
                    Email = email,
                    PasswordHash = passwordService.Hash(password),
                    Status = UserStatus.Active,
                    EmailVerifiedAt = DateTime.UtcNow,
                    Profile = new UserProfile { FirstName = first, LastName = last }
                };
                user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
                db.Users.Add(user);
                _logger.LogInformation("Seeded DA leadership user {Email} ({RoleCode}) with password {Password}", email, roleCode, password);
                continue;
            }

            user.UserRoles.Clear();
            user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
            user.AgencyMemberships.Clear();

            if (user.Profile is null)
            {
                user.Profile = new UserProfile { FirstName = first, LastName = last };
            }
            else
            {
                user.Profile.FirstName = first;
                user.Profile.LastName = last;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoClientUserAsync(AgriCheckDbContext db, IPasswordService passwordService, CancellationToken cancellationToken)
    {
        const string email = "importer@agricheck.local";
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken)) return;

        var role = await db.Roles.FirstAsync(r => r.Code == "ROLE_IMPORTER", cancellationToken);
        var user = new User
        {
            Uuid = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordService.Hash("Importer@12345"),
            Status = UserStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            Profile = new UserProfile { FirstName = "Demo", LastName = "Importer", CompanyName = "Agri Trade Corp" }
        };
        user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded demo client {Email} with password Importer@12345", email);
    }

    private static async Task EnsureAgencyProcessingFeeConfigsAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var defaults = new (string AgencyCode, EntryType EntryType, decimal Amount)[]
        {
            ("BAI", EntryType.Import, 2500m),
            ("BAI", EntryType.Export, 1500m),
            ("BFAR", EntryType.Import, 2000m),
            ("BPI", EntryType.Import, 2500m),
            ("SRA", EntryType.Import, 2500m),
            ("NTA", EntryType.Import, 2500m),
        };

        foreach (var (agencyCode, entryType, amount) in defaults)
        {
            var agency = await db.Agencies.FirstOrDefaultAsync(a => a.Code == agencyCode, cancellationToken);
            if (agency is null)
            {
                continue;
            }

            var exists = await db.ProcessingFeeConfigs.AnyAsync(
                c => c.AgencyId == agency.Id && c.EntryType == entryType,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            db.ProcessingFeeConfigs.Add(new ProcessingFeeConfig
            {
                AgencyId = agency.Id,
                EntryType = entryType,
                Amount = amount,
                Currency = "PHP",
                IsActive = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAgencyPaymentSettingsAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var agencies = await db.Agencies.Where(a => a.IsActive && a.Code != "DA" && a.Code != "MAV").ToListAsync(cancellationToken);
        foreach (var agency in agencies)
        {
            if (await db.AgencyPaymentSettings.AnyAsync(s => s.AgencyId == agency.Id, cancellationToken))
            {
                continue;
            }

            db.AgencyPaymentSettings.Add(new AgencyPaymentSettings
            {
                AgencyId = agency.Id,
                PayMongoEnabled = false,
                CashPaymentEnabled = true,
                CashPaymentInstructions = "Pay at the agency cashier and submit your official receipt (OR) number for verification."
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminPortalDataAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        await EnsureAgencyProcessingFeeConfigsAsync(db, cancellationToken);
        await EnsureAgencyPaymentSettingsAsync(db, cancellationToken);

        if (!await db.FormTemplates.AnyAsync(cancellationToken))
        {
            var bai = await db.Agencies.FirstAsync(a => a.Code == "BAI", cancellationToken);
            var form = new FormTemplate
            {
                Uuid = Guid.NewGuid(),
                Name = "Standard Import Entry Form",
                FormType = "ENTRY",
                Status = FormTemplateStatus.Published,
                IsActive = true,
                Versions = new List<FormTemplateVersion>
                {
                    new()
                    {
                        VersionNumber = 1,
                        IsPublished = true,
                        SchemaJson = ConnectedBaiEntrySchema
                    }
                },
                AgencyTags = new List<FormAgencyTag> { new() { AgencyId = bai.Id } }
            };
            db.FormTemplates.Add(form);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.FormTemplates.AnyAsync(t => t.FormType == "ACCREDITATION", cancellationToken))
        {
            var accreditationForm = new FormTemplate
            {
                Uuid = Guid.NewGuid(),
                Name = "Standard DA Accreditation Form",
                FormType = "ACCREDITATION",
                Status = FormTemplateStatus.Published,
                IsActive = true,
                Versions = new List<FormTemplateVersion>
                {
                    new()
                    {
                        VersionNumber = 1,
                        IsPublished = true,
                        SchemaJson = AccreditationFormSchema.Json
                    }
                }
            };
            db.FormTemplates.Add(accreditationForm);
            await db.SaveChangesAsync(cancellationToken);
        }

        // Entry certificate templates are per-agency and migrated from V2 (or created in the admin certificate builder).
    }

    private async Task SeedMavStaffAsync(AgriCheckDbContext db, IPasswordService passwordService, CancellationToken cancellationToken)
    {
        var staff = new (string Email, string Password, string RoleCode, string First, string Last)[]
        {
            ("mav.admin@agricheck.local", "MavAdmin@12345", "ROLE_MAV_ADMIN", "MAV", "Administrator"),
            ("mav.evaluator@agricheck.local", "MavEval@12345", "ROLE_MAV_EVALUATOR", "MAV", "Evaluator"),
            ("mav.secretary@agricheck.local", "MavSec@12345", "ROLE_MAV_SECRETARY", "MAV", "Secretary")
        };

        foreach (var (email, password, roleCode, first, last) in staff)
        {
            if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken)) continue;
            var role = await db.Roles.FirstAsync(r => r.Code == roleCode, cancellationToken);
            var user = new User
            {
                Uuid = Guid.NewGuid(),
                Email = email,
                PasswordHash = passwordService.Hash(password),
                Status = UserStatus.Active,
                EmailVerifiedAt = DateTime.UtcNow,
                Profile = new UserProfile { FirstName = first, LastName = last }
            };
            user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded MAV staff {Email} with password {Password}", email, password);
        }
    }

    private async Task SeedMavDemoDataAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.MavApplicationPeriods.AnyAsync(cancellationToken))
        {
            var bai = await db.Agencies.FirstAsync(a => a.Code == "BAI", cancellationToken);
            var beef = await db.Commodities.FirstAsync(c => c.Code == "BEEF", cancellationToken);
            var rice = await db.Commodities.FirstAsync(c => c.Code == "RICE", cancellationToken);
            var period = new MavApplicationPeriod
            {
                Uuid = Guid.NewGuid(),
                MavYear = 2026,
                PoolType = MavPoolType.BYP,
                OpeningDate = DateTime.UtcNow.AddDays(-7),
                ClosingDate = DateTime.UtcNow.AddMonths(3),
                Status = MavApplicationPeriodStatus.Open,
                AgencyId = bai.Id,
                CommodityAllocations = new List<MavCommodityAllocation>
                {
                    new() { CommodityId = beef.Id, HsCode = "0201", CommodityName = "Beef Products", TotalVolume = 10000m, MinimumImportVolume = 10m, AgencyId = bai.Id },
                    new() { CommodityId = rice.Id, HsCode = "1006", CommodityName = "Rice", TotalVolume = 50000m, MinimumImportVolume = 25m, AgencyId = bai.Id }
                }
            };
            db.MavApplicationPeriods.Add(period);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded open BAI MAV application period for 2026 BYP");
        }

        await ResetMavDemoLedgerAsync(db, cancellationToken);
        await AlignMavPeriodAgenciesAsync(db, cancellationToken);
        await UpgradeBaiEntryFormForMavAsync(db, cancellationToken);
        await SeedMavHsLibraryAsync(db, cancellationToken);
    }

    private async Task ResetMavDemoLedgerAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var hasDemo = await db.MavLicenses.AnyAsync(l => l.LicenseNumber == "MAV-LIC-DEMO-001", cancellationToken)
            || await db.MavApplications.AnyAsync(a => a.ReferenceNumber == "MAV-APP-DEMO-001", cancellationToken);
        var hasUnscopedPeriod = await db.MavApplicationPeriods.AnyAsync(p => p.AgencyId == null, cancellationToken);
        if (!hasDemo && !hasUnscopedPeriod)
        {
            return;
        }

        db.MicUtilizations.RemoveRange(db.MicUtilizations);
        db.MavAccountTransactions.RemoveRange(db.MavAccountTransactions);
        db.MavImportCertificates.RemoveRange(db.MavImportCertificates);
        db.MavAccounts.RemoveRange(db.MavAccounts);
        db.MavLicenses.RemoveRange(db.MavLicenses);
        db.MavApplications.RemoveRange(db.MavApplications);
        foreach (var allocation in db.MavCommodityAllocations)
        {
            allocation.AllocatedVolume = 0;
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Reset MAV applications, licenses, and MIC utilization so the connected agency flow can start clean.");
    }

    private async Task AlignMavPeriodAgenciesAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var bai = await db.Agencies.FirstOrDefaultAsync(a => a.Code == "BAI", cancellationToken);
        if (bai is null)
        {
            return;
        }

        var unscoped = await db.MavApplicationPeriods
            .Include(p => p.CommodityAllocations)
            .Where(p => p.AgencyId == null)
            .ToListAsync(cancellationToken);
        if (unscoped.Count == 0)
        {
            return;
        }

        foreach (var period in unscoped)
        {
            period.AgencyId = bai.Id;
            foreach (var allocation in period.CommodityAllocations.Where(a => a.AgencyId == null))
            {
                allocation.AgencyId = bai.Id;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Assigned BAI to {Count} unscoped MAV application period(s).", unscoped.Count);
    }

    private async Task UpgradeBaiEntryFormForMavAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        var bai = await db.Agencies.FirstOrDefaultAsync(a => a.Code == "BAI", cancellationToken);
        if (bai is null)
        {
            return;
        }

        var form = await db.FormTemplates
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.FormType == "ENTRY" && t.AgencyTags.Any(tag => tag.AgencyId == bai.Id), cancellationToken);
        var published = form?.Versions.FirstOrDefault(v => v.IsPublished) ?? form?.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (published is null)
        {
            return;
        }

        var current = published.SchemaJson.Trim();
        if (current == ConnectedBaiEntrySchema)
        {
            return;
        }

        if (current == LegacyBaiEntrySchema || (current.Contains("\"name\":\"commodityName\"", StringComparison.Ordinal) && current.Contains("\"type\":\"text\"", StringComparison.Ordinal) && current.Contains("\"name\":\"mav_no\"", StringComparison.Ordinal)))
        {
            published.SchemaJson = ConnectedBaiEntrySchema;
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated BAI entry form so commodity uses the agency MAV HS picker.");
        }
    }

    private async Task SeedMavHsLibraryAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        await MavHsLibrarySeeder.SeedAsync(db, _logger, cancellationToken);
    }

    private async Task SeedOpsStaffAsync(AgriCheckDbContext db, IPasswordService passwordService, CancellationToken cancellationToken)
    {
        var staff = new (string Email, string Password, string RoleCode, string First, string Last)[]
        {
            ("warehouse@agricheck.local", "Warehouse@12345", "ROLE_WAREHOUSE_STAFF", "Warehouse", "Staff"),
            ("driver@agricheck.local", "Driver@12345", "ROLE_DRIVER", "Demo", "Driver"),
            ("operator@agricheck.local", "Operator@12345", "ROLE_OPERATOR", "AgriTrack", "Operator"),
            ("doctor@agricheck.local", "Doctor@12345", "ROLE_DOCTOR", "Port", "Doctor")
        };

        foreach (var (email, password, roleCode, first, last) in staff)
        {
            if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken)) continue;
            var role = await db.Roles.FirstAsync(r => r.Code == roleCode, cancellationToken);
            var user = new User
            {
                Uuid = Guid.NewGuid(),
                Email = email,
                PasswordHash = passwordService.Hash(password),
                Status = UserStatus.Active,
                EmailVerifiedAt = DateTime.UtcNow,
                Profile = new UserProfile { FirstName = first, LastName = last }
            };
            user.UserRoles.Add(new UserRole { Role = role, AssignedAt = DateTime.UtcNow });
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded ops user {Email} with password {Password}", email, password);
        }
    }

}
