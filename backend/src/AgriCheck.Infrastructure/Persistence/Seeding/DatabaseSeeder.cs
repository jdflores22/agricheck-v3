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
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
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
            await CertificateTemplateMigrationSeeder.MigrateFromV2Async(db, configuration, environment, _logger, cancellationToken);
            await EntryFormMigrationSeeder.MigrateFromV2Async(db, configuration, _logger, force: false, cancellationToken);
            await ContainerFormMigrationSeeder.MigrateFromV2Async(db, configuration, _logger, force: false, cancellationToken);
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

    private static async Task SeedComplianceChecklistsAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        if (await db.ComplianceChecklists.AnyAsync(cancellationToken)) return;

        var bai = await db.Agencies.FirstAsync(a => a.Code == "BAI", cancellationToken);
        var checklist = new ComplianceChecklist
        {
            AgencyId = bai.Id,
            Name = "Standard Import Evaluation Checklist",
            IsActive = true,
            Items = new List<ComplianceChecklistItem>
            {
                new() { Label = "Complete application form", SortOrder = 1, IsRequired = true },
                new() { Label = "Valid health certificate attached", SortOrder = 2, IsRequired = true },
                new() { Label = "Commodity details verified", SortOrder = 3, IsRequired = true },
                new() { Label = "Processing fee paid", SortOrder = 4, IsRequired = true },
                new() { Label = "Supporting documents complete", SortOrder = 5, IsRequired = false }
            }
        };

        db.ComplianceChecklists.Add(checklist);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAgencyStaffAsync(AgriCheckDbContext db, IPasswordService passwordService, CancellationToken cancellationToken)
    {
        var staff = new (string Email, string Password, string RoleCode, string First, string Last)[]
        {
            ("evaluator@agricheck.local", "Evaluator@12345", "ROLE_EVALUATOR", "BAI", "Evaluator"),
            ("inspector@agricheck.local", "Inspector@12345", "ROLE_INSPECTOR", "BAI", "Inspector"),
            ("billing@agricheck.local", "Billing@12345", "ROLE_BILLING_AGENT", "BAI", "Billing Agent"),
            ("evaluator.bfar@agricheck.local", "Evaluator@12345", "ROLE_EVALUATOR", "BFAR", "Evaluator")
        };

        foreach (var (email, password, roleCode, first, last) in staff)
        {
            if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken)) continue;

            var agencyCode = email.Contains("bfar", StringComparison.OrdinalIgnoreCase) ? "BFAR" : "BAI";
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

    private async Task SeedAdminPortalDataAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.ProcessingFeeConfigs.AnyAsync(cancellationToken))
        {
            var bai = await db.Agencies.FirstAsync(a => a.Code == "BAI", cancellationToken);
            var bfar = await db.Agencies.FirstAsync(a => a.Code == "BFAR", cancellationToken);
            db.ProcessingFeeConfigs.AddRange(
                new ProcessingFeeConfig { AgencyId = bai.Id, EntryType = EntryType.Import, Amount = 2500m, Currency = "PHP", IsActive = true },
                new ProcessingFeeConfig { AgencyId = bai.Id, EntryType = EntryType.Export, Amount = 1500m, Currency = "PHP", IsActive = true },
                new ProcessingFeeConfig { AgencyId = bfar.Id, EntryType = EntryType.Import, Amount = 2000m, Currency = "PHP", IsActive = true });
            await db.SaveChangesAsync(cancellationToken);
        }

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
                        SchemaJson = """[{"name":"commodityName","label":"Commodity","type":"text","required":true},{"name":"quantity","label":"Quantity","type":"number","required":true},{"name":"originCountry","label":"Origin Country","type":"text","required":true},{"name":"mav_no","label":"MAV No.","type":"text","required":true},{"name":"mav_certificate","label":"MAV Certificate","type":"file","required":true,"accept":".pdf"}]"""
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

        if (!await db.CertificateTemplates.AnyAsync(cancellationToken))
        {
            var bai = await db.Agencies.FirstAsync(a => a.Code == "BAI", cancellationToken);
            var template = new CertificateTemplate
            {
                Uuid = Guid.NewGuid(),
                Name = "Standard Import Certificate",
                Description = "Default certificate layout for approved import entries",
                AgencyId = bai.Id,
                IsActive = true,
                Versions = new List<CertificateTemplateVersion>
                {
                    new()
                    {
                        VersionNumber = 1,
                        IsPublished = true,
                        Elements = new List<CertificateElement>
                        {
                            new() { ElementType = CertificateElementType.Text, Label = "Certificate Title", SortOrder = 1 },
                            new() { ElementType = CertificateElementType.Field, Label = "Holder Name", ConfigJson = """{"field":"holderName"}""", SortOrder = 2 },
                            new() { ElementType = CertificateElementType.Field, Label = "Entry Reference", ConfigJson = """{"field":"referenceNo"}""", SortOrder = 3 },
                            new() { ElementType = CertificateElementType.QrCode, Label = "Verification QR", SortOrder = 4 }
                        }
                    }
                }
            };
            db.CertificateTemplates.Add(template);
            await db.SaveChangesAsync(cancellationToken);

            db.CertificateProcessAssignments.AddRange(
                new CertificateProcessAssignment { AgencyId = bai.Id, TemplateId = template.Id, ProcessType = CertificateProcessType.ImportEntry, IsActive = true },
                new CertificateProcessAssignment { AgencyId = bai.Id, TemplateId = template.Id, ProcessType = CertificateProcessType.ExportEntry, IsActive = true });
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded default certificate template and process assignments for BAI");
        }
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
        MavApplicationPeriod period;
        if (!await db.MavApplicationPeriods.AnyAsync(cancellationToken))
        {
            var beef = await db.Commodities.FirstAsync(c => c.Code == "BEEF", cancellationToken);
            var rice = await db.Commodities.FirstAsync(c => c.Code == "RICE", cancellationToken);
            period = new MavApplicationPeriod
            {
                Uuid = Guid.NewGuid(),
                MavYear = 2026,
                PoolType = MavPoolType.BYP,
                OpeningDate = DateTime.UtcNow.AddDays(-7),
                ClosingDate = DateTime.UtcNow.AddMonths(3),
                Status = MavApplicationPeriodStatus.Open,
                CommodityAllocations = new List<MavCommodityAllocation>
                {
                    new() { CommodityId = beef.Id, HsCode = "0201", CommodityName = "Beef Products", TotalVolume = 10000m, MinimumImportVolume = 10m },
                    new() { CommodityId = rice.Id, HsCode = "1006", CommodityName = "Rice", TotalVolume = 50000m, MinimumImportVolume = 25m }
                }
            };
            db.MavApplicationPeriods.Add(period);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded open MAV application period for 2026 BYP");
        }
        else
        {
            period = await db.MavApplicationPeriods.OrderByDescending(p => p.MavYear).FirstAsync(cancellationToken);
        }

        await SeedImporterMavDemoLicenseAsync(db, period, cancellationToken);
        await SeedMavHsLibraryAsync(db, cancellationToken);
    }

    private async Task SeedMavHsLibraryAsync(AgriCheckDbContext db, CancellationToken cancellationToken)
    {
        await MavHsLibrarySeeder.SeedAsync(db, _logger, cancellationToken);
    }

    private async Task SeedImporterMavDemoLicenseAsync(
        AgriCheckDbContext db,
        MavApplicationPeriod period,
        CancellationToken cancellationToken)
    {
        if (await db.MavLicenses.AnyAsync(l => l.LicenseNumber == "MAV-LIC-DEMO-001", cancellationToken))
        {
            return;
        }

        var importer = await db.Users.FirstOrDefaultAsync(u => u.Email == "importer@agricheck.local", cancellationToken);
        if (importer is null)
        {
            return;
        }

        var beef = await db.Commodities.FirstAsync(c => c.Code == "BEEF", cancellationToken);
        var app = new MavApplication
        {
            Uuid = Guid.NewGuid(),
            ReferenceNumber = "MAV-APP-DEMO-001",
            ApplicationPeriodId = period.Id,
            ImporterId = importer.Id,
            HsCode = "0201",
            CommodityName = beef.Name,
            RequestedVolume = 500m,
            AllocatedVolume = 500m,
            Status = MavApplicationStatus.Approved,
            SubmittedAt = DateTime.UtcNow.AddDays(-14),
            ReviewedAt = DateTime.UtcNow.AddDays(-13)
        };
        db.MavApplications.Add(app);
        await db.SaveChangesAsync(cancellationToken);

        var license = new MavLicense
        {
            Uuid = Guid.NewGuid(),
            LicenseNumber = "MAV-LIC-DEMO-001",
            ApplicationId = app.Id,
            ImporterId = importer.Id,
            MavYear = period.MavYear,
            PoolType = period.PoolType,
            HsCode = app.HsCode,
            CommodityName = app.CommodityName,
            AwardedVolume = 500m,
            Status = MavLicenseStatus.Active,
            IssuedAt = DateTime.UtcNow.AddDays(-13),
            ExpiresAt = new DateTime(period.MavYear, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };
        db.MavLicenses.Add(license);
        await db.SaveChangesAsync(cancellationToken);

        var account = new MavAccount
        {
            LicenseId = license.Id,
            AwardedVolume = 500m,
            UtilizedVolume = 100m,
            LastTransactionAt = DateTime.UtcNow.AddDays(-5)
        };
        db.MavAccounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);

        db.MavImportCertificates.Add(new MavImportCertificate
        {
            Uuid = Guid.NewGuid(),
            CertificateNumber = "MIC-DEMO-001",
            LicenseId = license.Id,
            AccountId = account.Id,
            ImporterId = importer.Id,
            HsCode = license.HsCode,
            CommodityName = license.CommodityName,
            AuthorizedVolume = 100m,
            UtilizedVolume = 0m,
            Status = MavImportCertificateStatus.Active,
            IssuedAt = DateTime.UtcNow.AddDays(-5),
            ExpiresAt = DateTime.UtcNow.AddMonths(2)
        });

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded demo MAV license and MIC for importer@agricheck.local");
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
