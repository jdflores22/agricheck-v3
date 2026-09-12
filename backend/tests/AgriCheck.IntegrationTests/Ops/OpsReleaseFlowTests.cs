using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriCheck.IntegrationTests.OpsPortal;

public class OpsReleaseFlowTests
{
    [Fact]
    public async Task ReceiveAndReleaseFlow_CompletesWarehouseCycle()
    {
        var ctx = await EndToEndTestData.CreateAsync();
        await using (ctx.Db)
        {
            var entry = new Entry
            {
                Uuid = Guid.NewGuid(),
                ReferenceNo = "ENT-OPS-REL-001",
                UserId = ctx.Importer.Id,
                AgencyId = ctx.Agency.Id,
                EntryType = EntryType.Import,
                Status = EntryStatus.Approved,
                PaymentStatus = PaymentStatus.Paid
            };
            ctx.Db.Entries.Add(entry);
            await ctx.Db.SaveChangesAsync();

            var container = new Container
            {
                Uuid = Guid.NewGuid(),
                EntryId = entry.Id,
                ContainerNumber = "CONT-REL-001",
                Status = ContainerStatus.InTransit,
                AssignedDriverUserId = ctx.Driver.Id
            };
            ctx.Db.Containers.Add(container);
            await ctx.Db.SaveChangesAsync();

            var facility = await ctx.Db.WarehouseFacilities.FirstAsync();
            var warehouse = new WarehouseOpsService(ctx.Db, new TestCurrentUserService(ctx.WarehouseStaff.Uuid, "ROLE_WAREHOUSE_STAFF"));

            var inventory = await warehouse.ReceiveContainerAsync(new ReceiveContainerRequest(container.Uuid, facility.Id, "A-01"));
            Assert.Equal(WarehouseInventoryStatus.Stored.ToString(), inventory.Status);

            var auth = await warehouse.CreateReleaseAuthorizationAsync(new CreateReleaseAuthorizationRequest(
                entry.Uuid, "John Recipient", "ID-12345"));
            Assert.Equal(entry.ReferenceNo, auth.EntryReference);

            var release = await warehouse.ExecuteReleaseAsync(new ExecuteReleaseRequest(inventory.Uuid, auth.Uuid, null));
            Assert.Equal(container.ContainerNumber, release.ContainerNumber);

            var updatedContainer = await ctx.Db.Containers.FirstAsync(c => c.Id == container.Id);
            Assert.Equal(ContainerStatus.Released, updatedContainer.Status);
        }
    }
}
