using AgriCheck.Application.MavPortal.Dtos;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriCheck.IntegrationTests.MavPortal;

public class MavFlowTests
{
    [Fact]
    public async Task ApplyApproveMicFlow_CreatesLicenseAndMic()
    {
        var ctx = await EndToEndTestData.CreateAsync();
        await using (ctx.Db)
        {
            var appService = new MavApplicationService(ctx.Db, new TestCurrentUserService(ctx.Importer.Uuid, "ROLE_IMPORTER"));
            var created = await appService.CreateAsync(new CreateMavApplicationRequest(
                ctx.Period.Uuid, "0201", "Beef Products", 500m, null));
            var submitted = await appService.SubmitAsync(created.Uuid);
            Assert.Equal(MavApplicationStatus.Submitted.ToString(), submitted.Status);

            var staffService = new MavApplicationService(ctx.Db, new TestCurrentUserService(ctx.MavAdmin.Uuid, "ROLE_MAV_ADMIN"));
            var approved = await staffService.ApproveAsync(
                created.Uuid,
                new ReviewMavApplicationRequest(500m));
            Assert.Equal(MavApplicationStatus.Approved.ToString(), approved.Status);
            Assert.NotNull(approved.LicenseUuid);

            var micService = new MavMicService(ctx.Db, new TestCurrentUserService(ctx.Importer.Uuid, "ROLE_IMPORTER"));
            var mic = await micService.IssueAsync(approved.LicenseUuid!.Value, new IssueMicRequest(100m));
            Assert.Equal(MavImportCertificateStatus.Active.ToString(), mic.Status);
            Assert.Equal(100m, mic.AuthorizedVolume);

            var license = await ctx.Db.MavLicenses.Include(l => l.Account).FirstAsync(l => l.Uuid == approved.LicenseUuid);
            Assert.Equal(100m, license.Account.UtilizedVolume);
        }
    }
}
