using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriCheck.IntegrationTests.OpsPortal;

public class OpsPortalTests
{
    [Fact]
    public async Task DriverContainers_OnlyReturnsAssignedContainers()
    {
        var (db, _, driver, _, assigned, other, _, _) = await OpsTestData.CreateAsync();
        await using (db)
        {
            var service = new DriverOpsService(db, new TestCurrentUserService(driver.Uuid, "ROLE_DRIVER"));
            var result = await service.ListAssignedContainersAsync();

            Assert.Single(result);
            Assert.Equal(assigned.Uuid, result[0].Uuid);
            Assert.DoesNotContain(result, c => c.Uuid == other.Uuid);
        }
    }

    [Fact]
    public async Task ReceiveContainer_CreatesStoredInventory()
    {
        var (db, staff, _, _, container, _, _, facility) = await OpsTestData.CreateAsync();
        await using (db)
        {
            var service = new WarehouseOpsService(db, new TestCurrentUserService(staff.Uuid, "ROLE_WAREHOUSE_STAFF"));
            var result = await service.ReceiveContainerAsync(new ReceiveContainerRequest(container.Uuid, facility.Id, "A-01"));

            Assert.Equal("Stored", result.Status);
            Assert.Equal(1, await db.WarehouseInventories.CountAsync());
            var updated = await db.Containers.FirstAsync(c => c.Id == container.Id);
            Assert.Equal(Domain.Enums.ContainerStatus.AtWarehouse, updated.Status);
        }
    }

    [Fact]
    public async Task MobileSyncPush_ProcessesContainerStatus()
    {
        var (db, _, driver, _, container, _, _, _) = await OpsTestData.CreateAsync();
        await using (db)
        {
            var driverService = new DriverOpsService(db, new TestCurrentUserService(driver.Uuid, "ROLE_DRIVER"));
            var syncService = new MobileSyncService(db, new TestCurrentUserService(driver.Uuid, "ROLE_DRIVER"), driverService);

            var payload = $$"""{"containerUuid":"{{container.Uuid}}","status":"InTransit"}""";
            var result = await syncService.PushAsync(new MobileSyncPushRequest(new[]
            {
                new MobileSyncItemRequest("sync-1", "container_status", payload)
            }));

            Assert.Equal(1, result.Processed);
            var updated = await db.Containers.FirstAsync(c => c.Id == container.Id);
            Assert.Equal(Domain.Enums.ContainerStatus.InTransit, updated.Status);
        }
    }
}
