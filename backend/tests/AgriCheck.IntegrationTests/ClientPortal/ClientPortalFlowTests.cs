using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriCheck.IntegrationTests.ClientPortal;

public class ClientPortalFlowTests
{
    [Fact]
    public async Task EntrySubmitFlow_CreatesBillAndSubmittedStatus()
    {
        var ctx = await EndToEndTestData.CreateAsync();
        await using (ctx.Db)
        {
            ctx.Db.AccreditationSubmissions.Add(new AgriCheck.Domain.Entities.AccreditationSubmission
            {
                Uuid = Guid.NewGuid(),
                UserId = ctx.Importer.Id,
                CompanyName = "Test Corp",
                SubmissionType = "NEW",
                Status = AccreditationSubmissionStatus.Approved,
                SubmittedAt = DateTime.UtcNow,
            });
            await ctx.Db.SaveChangesAsync();

            var entryService = new EntryService(ctx.Db, new TestCurrentUserService(ctx.Importer.Uuid), new TestFileStorageService(), TestNotificationFactory.Create(ctx.Db, new TestCurrentUserService(ctx.Importer.Uuid)));

            var created = await entryService.CreateAsync(EndToEndTestData.SampleEntryRequest(ctx.Agency.Id));
            Assert.Equal(EntryStatus.Draft.ToString(), created.Status);

            var submitted = await entryService.SubmitAsync(created.Uuid);
            Assert.Equal(EntryStatus.Submitted.ToString(), submitted.Status);
            Assert.Equal(PaymentStatus.Unpaid.ToString(), submitted.PaymentStatus);
            Assert.NotNull(submitted.PaymentAmount);
            Assert.Single(submitted.Bills);

            var billCount = await ctx.Db.ClientBills.CountAsync(b => b.EntryId != 0);
            Assert.Equal(1, billCount);
        }
    }

    [Fact]
    public async Task AccreditationSubmitFlow_UpdatesStatusAndHistory()
    {
        var ctx = await EndToEndTestData.CreateAsync();
        await using (ctx.Db)
        {
            var service = new AccreditationService(ctx.Db, new TestCurrentUserService(ctx.Importer.Uuid), new TestFileStorageService(), TestNotificationFactory.Create(ctx.Db, new TestCurrentUserService(ctx.Importer.Uuid)));
            var created = await service.CreateAsync(new CreateAccreditationRequest("Test Corp", "NEW", "{}"));
            Assert.Equal(AccreditationSubmissionStatus.Draft.ToString(), created.Status);

            var submitted = await service.SubmitAsync(created.Uuid);
            Assert.Equal(AccreditationSubmissionStatus.Submitted.ToString(), submitted.Status);
            Assert.NotEmpty(submitted.History);
        }
    }

    [Fact]
    public async Task AccreditationCreate_PreventsDuplicateDraft()
    {
        var ctx = await EndToEndTestData.CreateAsync();
        await using (ctx.Db)
        {
            var service = new AccreditationService(ctx.Db, new TestCurrentUserService(ctx.Importer.Uuid), new TestFileStorageService(), TestNotificationFactory.Create(ctx.Db, new TestCurrentUserService(ctx.Importer.Uuid)));
            await service.CreateAsync(new CreateAccreditationRequest("Test Corp", "NEW", "{}"));

            var ex = await Assert.ThrowsAsync<ClientPortalException>(() => service.CreateAsync(new CreateAccreditationRequest("Another Corp", "NEW", "{}")));
            Assert.Equal("DRAFT_EXISTS", ex.Code);
        }
    }
}
