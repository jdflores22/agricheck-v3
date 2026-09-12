using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using Xunit;

namespace AgriCheck.IntegrationTests.AgencyPortal;

public class AgencyDataIsolationTests
{
    [Fact]
    public async Task EvaluatorQueue_OnlyReturnsEntriesForOwnAgency()
    {
        var (db, baiEvaluator, _, baiEntry, bfarEntry) = await AgencyTestData.CreateAsync();
        await using (db)
        {
            var service = new EvaluatorService(db, new TestCurrentUserService(baiEvaluator.Uuid, "ROLE_EVALUATOR"), TestNotificationFactory.Create(db, new TestCurrentUserService(baiEvaluator.Uuid, "ROLE_EVALUATOR")));
            var result = await service.ListQueueAsync(1, 20);

            Assert.Single(result.Items);
            Assert.Equal(baiEntry.Uuid, result.Items[0].Uuid);
            Assert.DoesNotContain(result.Items, i => i.Uuid == bfarEntry.Uuid);
        }
    }

    [Fact]
    public async Task AssignToSelf_RejectsEntryFromOtherAgency()
    {
        var (db, baiEvaluator, _, _, bfarEntry) = await AgencyTestData.CreateAsync();
        await using (db)
        {
            var service = new EvaluatorService(db, new TestCurrentUserService(baiEvaluator.Uuid, "ROLE_EVALUATOR"), TestNotificationFactory.Create(db, new TestCurrentUserService(baiEvaluator.Uuid, "ROLE_EVALUATOR")));
            var ex = await Assert.ThrowsAsync<ClientPortalException>(() => service.AssignToSelfAsync(bfarEntry.Uuid));
            Assert.Equal("ENTRY_NOT_FOUND", ex.Code);
        }
    }

    [Fact]
    public async Task GetEntry_RejectsEntryFromOtherAgency()
    {
        var (db, baiEvaluator, _, _, bfarEntry) = await AgencyTestData.CreateAsync();
        await using (db)
        {
            var service = new EvaluatorService(db, new TestCurrentUserService(baiEvaluator.Uuid, "ROLE_EVALUATOR"), TestNotificationFactory.Create(db, new TestCurrentUserService(baiEvaluator.Uuid, "ROLE_EVALUATOR")));
            var entry = await service.GetEntryAsync(bfarEntry.Uuid);
            Assert.Null(entry);
        }
    }

    [Fact]
    public async Task ApprovedEntries_OnlyReturnsOwnAgencyApprovedEntries()
    {
        var (db, baiEvaluator, _, baiEntry, bfarEntry) = await AgencyTestData.CreateAsync();
        baiEntry.Status = Domain.Enums.EntryStatus.Approved;
        bfarEntry.Status = Domain.Enums.EntryStatus.Approved;
        await db.SaveChangesAsync();

        await using (db)
        {
            var service = new EvaluatorService(db, new TestCurrentUserService(baiEvaluator.Uuid, "ROLE_EVALUATOR"), TestNotificationFactory.Create(db, new TestCurrentUserService(baiEvaluator.Uuid, "ROLE_EVALUATOR")));
            var result = await service.ListApprovedEntriesAsync(1, 20);

            Assert.Single(result.Items);
            Assert.Equal(baiEntry.Uuid, result.Items[0].Uuid);
        }
    }

    [Fact]
    public async Task InspectionList_OnlyReturnsOwnAgencyInspections()
    {
        var (db, baiEvaluator, _, baiEntry, bfarEntry) = await AgencyTestData.CreateAsync();
        var baiInspection = new Domain.Entities.Inspection
        {
            Uuid = Guid.NewGuid(),
            EntryId = baiEntry.Id,
            AgencyId = baiEntry.AgencyId,
            InspectorUserId = baiEvaluator.Id,
            Status = Domain.Enums.InspectionStatus.Scheduled
        };
        var bfarInspection = new Domain.Entities.Inspection
        {
            Uuid = Guid.NewGuid(),
            EntryId = bfarEntry.Id,
            AgencyId = bfarEntry.AgencyId,
            InspectorUserId = baiEvaluator.Id,
            Status = Domain.Enums.InspectionStatus.Scheduled
        };
        db.Inspections.AddRange(baiInspection, bfarInspection);
        await db.SaveChangesAsync();

        await using (db)
        {
            var service = new InspectionService(db, new TestCurrentUserService(baiEvaluator.Uuid, "ROLE_INSPECTOR"), new NoOpFileStorage());
            var result = await service.ListAsync(1, 20, null);

            Assert.Single(result.Items);
            Assert.Equal(baiInspection.Uuid, result.Items[0].Uuid);
        }
    }
}
