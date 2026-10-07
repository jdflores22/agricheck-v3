using AgriCheck.Application.Compliance;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AgriCheck.IntegrationTests.Support;

internal static class IntegrationTestServices
{
    public static EvaluatorService CreateEvaluatorService(AgriCheckDbContext db, Guid userUuid, string role)
    {
        var currentUser = new TestCurrentUserService(userUuid, role);
        var notifications = TestNotificationFactory.Create(db, currentUser);
        var workflow = new EntryWorkflowService(db, notifications, new NoOpEntryCertificateAutoIssueService());
        return new EvaluatorService(db, currentUser, notifications, new TestFileStorageService(), workflow);
    }

    public static DriverOpsService CreateDriverOpsService(AgriCheckDbContext db, Guid userUuid, string role)
    {
        var currentUser = new TestCurrentUserService(userUuid, role);
        var notifications = TestNotificationFactory.Create(db, currentUser);
        return new DriverOpsService(
            db,
            currentUser,
            new ConfigurationBuilder().Build(),
            new TestFileStorageService(),
            new NoOpAgriTrackPushService(),
            notifications);
    }

    public static MavApplicationService CreateMavApplicationService(AgriCheckDbContext db, Guid userUuid, string role) =>
        new(db, new TestCurrentUserService(userUuid, role), new NoOpMavNotificationService());

    public static WarehouseOpsService CreateWarehouseOpsService(AgriCheckDbContext db, Guid userUuid, string role) =>
        CreateWarehouseOpsService(db, userUuid, role, new ComplianceOptions { RequireCswHaulCompliance = false });

    public static WarehouseOpsService CreateWarehouseOpsService(
        AgriCheckDbContext db,
        Guid userUuid,
        string role,
        ComplianceOptions complianceOptions)
    {
        var currentUser = new TestCurrentUserService(userUuid, role);
        var facilityCompliance = new FacilityComplianceService(db);
        var importGate = new ContainerImportComplianceGate(db, facilityCompliance, Options.Create(complianceOptions));
        return new WarehouseOpsService(db, currentUser, importGate);
    }

    public static EntryDestinationValidationService CreateEntryDestinationValidationService(
        AgriCheckDbContext db,
        Guid userUuid,
        ComplianceOptions? options = null) =>
        new(
            db,
            new TestCurrentUserService(userUuid),
            Options.Create(options ?? new ComplianceOptions()));

    public static ComplianceGateService CreateComplianceGateService(AgriCheckDbContext db, ComplianceOptions? options = null) =>
        new(
            db,
            new AgencyLicenseService(db),
            new FacilityComplianceService(db),
            CreateEntryDestinationValidationService(db, Guid.Empty, options),
            Options.Create(options ?? new ComplianceOptions()));

    public static ComplianceChainService CreateComplianceChainService(AgriCheckDbContext db, Guid userUuid, ComplianceOptions? options = null)
    {
        var currentUser = new TestCurrentUserService(userUuid);
        var gate = CreateComplianceGateService(db, options);
        return new ComplianceChainService(db, gate, new AgencyLicenseService(db), new FacilityComplianceService(db), currentUser);
    }

    public static EntryService CreateEntryService(AgriCheckDbContext db, Guid userUuid, ComplianceOptions? options = null)
    {
        var currentUser = new TestCurrentUserService(userUuid);
        var compliance = options ?? new ComplianceOptions();
        return new EntryService(
            db,
            currentUser,
            new TestFileStorageService(),
            TestNotificationFactory.Create(db, currentUser),
            new MavMicService(db, currentUser, new NoOpMavNotificationService()),
            CreateComplianceGateService(db, compliance),
            CreateEntryDestinationValidationService(db, userUuid, compliance),
            Options.Create(compliance));
    }
}
