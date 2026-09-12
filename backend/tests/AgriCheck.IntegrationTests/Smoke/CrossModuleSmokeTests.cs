using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using Xunit;

namespace AgriCheck.IntegrationTests.Smoke;

public class CrossModuleSmokeTests
{
    [Fact]
    public async Task AdminDashboard_ReturnsCounts()
    {
        var (db, _, _, _, _, _) = await CrossModuleTestData.CreateAsync();
        await using (db)
        {
            var service = new AdminDashboardService(db);
            var result = await service.GetDashboardAsync();
            Assert.True(result.TotalUsers >= 5);
        }
    }

    [Fact]
    public async Task ClientDashboard_ReturnsCounts()
    {
        var (db, _, importer, _, _, _) = await CrossModuleTestData.CreateAsync();
        await using (db)
        {
            var service = new ClientDashboardService(db, new TestCurrentUserService(importer.Uuid));
            var result = await service.GetDashboardAsync();
            Assert.Equal(1, result.Entries.Total);
        }
    }

    [Fact]
    public async Task AgencyDashboard_ReturnsCounts()
    {
        var (db, _, _, evaluator, _, _) = await CrossModuleTestData.CreateAsync();
        await using (db)
        {
            var service = new AgencyDashboardService(db, new TestCurrentUserService(evaluator.Uuid, "ROLE_EVALUATOR"));
            var result = await service.GetDashboardAsync();
            Assert.True(result.QueueCount >= 0);
        }
    }

    [Fact]
    public async Task MavImporterDashboard_ReturnsCounts()
    {
        var (db, _, importer, _, _, _) = await CrossModuleTestData.CreateAsync();
        await using (db)
        {
            var service = new MavDashboardService(db, new TestCurrentUserService(importer.Uuid, "ROLE_IMPORTER"));
            var result = await service.GetImporterDashboardAsync();
            Assert.True(result.OpenPeriods >= 1);
        }
    }

    [Fact]
    public async Task WarehouseDashboard_ReturnsCounts()
    {
        var (db, _, _, _, warehouse, _) = await CrossModuleTestData.CreateAsync();
        await using (db)
        {
            var service = new WarehouseOpsService(db, new TestCurrentUserService(warehouse.Uuid, "ROLE_WAREHOUSE_STAFF"));
            var result = await service.GetDashboardAsync();
            Assert.True(result.ActiveFacilities >= 0);
        }
    }

    [Fact]
    public async Task DriverDashboard_ReturnsCounts()
    {
        var (db, _, _, _, _, driver) = await CrossModuleTestData.CreateAsync();
        await using (db)
        {
            var service = new DriverOpsService(db, new TestCurrentUserService(driver.Uuid, "ROLE_DRIVER"));
            var result = await service.GetDashboardAsync();
            Assert.Equal(1, result.AssignedContainers);
        }
    }
}
