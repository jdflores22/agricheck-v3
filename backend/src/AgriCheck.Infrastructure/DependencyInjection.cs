using AgriCheck.Application.AdminPortal;
using AgriCheck.Application.AgencyPortal;
using AgriCheck.Application.Auth;
using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.DaPortal;
using AgriCheck.Application.MavPortal;
using AgriCheck.Application.Notifications;
using AgriCheck.Application.OpsPortal;
using AgriCheck.Application.Payments;
using AgriCheck.Application.Common;
using AgriCheck.Infrastructure.Auth;
using AgriCheck.Infrastructure.Email;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Persistence.Seeding;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgriCheck.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AgriCheckDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddMemoryCache();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ILoginAttemptService, LoginAttemptService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IEntryService, EntryService>();
        services.AddScoped<IAccreditationService, AccreditationService>();
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<IWarehouseBookingService, WarehouseBookingService>();
        services.AddScoped<IClientBillService, ClientBillService>();
        services.AddScoped<IClientProfileService, ClientProfileService>();
        services.AddScoped<IClientContainerService, ClientContainerService>();
        services.AddScoped<IClientInspectionService, ClientInspectionService>();
        services.AddScoped<ICommodityService, CommodityService>();
        services.AddScoped<IClientDashboardService, ClientDashboardService>();
        services.AddScoped<IClientFormService, ClientFormService>();
        services.AddScoped<IAddressLookupService, AddressLookupService>();
        services.AddScoped<ISystemBrandingService, SystemBrandingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IPaymentGatewayService, PayMongoGatewayService>();
        services.AddHttpClient("PayMongo");
        services.AddScoped<IAgencyDashboardService, AgencyDashboardService>();
        services.AddScoped<IEvaluatorService, EvaluatorService>();
        services.AddScoped<IInspectionService, InspectionService>();
        services.AddScoped<IAgencyBillingService, AgencyBillingService>();
        services.AddScoped<IAccreditationReviewService, AccreditationReviewService>();
        services.AddScoped<IAccreditationNumberGenerator, AccreditationNumberGenerator>();
        services.AddScoped<IAccreditationCertificateService, AccreditationCertificateService>();
        services.AddScoped<ISecretaryReportService, SecretaryReportService>();
        services.AddScoped<IDaDashboardService, DaDashboardService>();
        services.AddScoped<IDaAgencyOverviewService, DaAgencyOverviewService>();
        services.AddScoped<IDaOversightReportService, DaOversightReportService>();
        services.AddScoped<IDaWarehouseManagementService, DaWarehouseManagementService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminAgencyService, AdminAgencyService>();
        services.AddScoped<IAdminCommodityService, AdminCommodityService>();
        services.AddScoped<IAdminPaymentConfigService, AdminPaymentConfigService>();
        services.AddScoped<IAdminEntryPaymentService, AdminEntryPaymentService>();
        services.AddScoped<IFormBuilderService, FormBuilderService>();
        services.AddScoped<ICertificateTemplateService, CertificateTemplateService>();
        services.AddScoped<ICertificateIssuanceService, CertificateIssuanceService>();
        services.AddScoped<IAdminAuditService, AdminAuditService>();
        services.AddScoped<IAdminRoleService, AdminRoleService>();
        services.AddScoped<IAdminSettingsService, AdminSettingsService>();
        services.AddScoped<IMavDashboardService, MavDashboardService>();
        services.AddScoped<IMavApplicationPeriodService, MavApplicationPeriodService>();
        services.AddScoped<IMavApplicationService, MavApplicationService>();
        services.AddScoped<IMavLicenseService, MavLicenseService>();
        services.AddScoped<IMavMicService, MavMicService>();
        services.AddScoped<IMavComplianceService, MavComplianceService>();
        services.AddScoped<IMavReportService, MavReportService>();
        services.AddScoped<IMavHsLibraryService, MavHsLibraryService>();
        services.AddScoped<IMavYearTransitionService, MavYearTransitionService>();
        services.AddScoped<IMavNotificationService, MavNotificationService>();
        services.AddScoped<IEntryWorkflowService, EntryWorkflowService>();
        services.AddScoped<IEntryCertificateAutoIssueService, EntryCertificateAutoIssueService>();
        services.AddScoped<IClientDaBillingService, ClientDaBillingService>();
        services.AddScoped<IContainerInspectionWorkflowService, ContainerInspectionWorkflowService>();
        services.AddScoped<ITransportTagService, TransportTagService>();
        services.AddScoped<IOperatorOpsService, OperatorOpsService>();
        services.AddScoped<IDoctorInspectionService, DoctorInspectionService>();
        services.AddScoped<IWarehouseOpsService, WarehouseOpsService>();
        services.AddScoped<IDriverOpsService, DriverOpsService>();
        services.AddScoped<IMobileSyncService, MobileSyncService>();
        services.AddScoped<IAgriTrackPushService, AgriTrackPushService>();
        services.AddHttpClient("ExpoPush");

        services.AddHostedService<DatabaseSeeder>();
        services.AddHostedService<MavComplianceNotificationBackgroundService>();

        return services;
    }
}
