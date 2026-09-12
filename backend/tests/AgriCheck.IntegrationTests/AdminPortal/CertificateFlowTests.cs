using AgriCheck.Application.AdminPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AgriCheck.IntegrationTests.AdminPortal;

public class CertificateFlowTests
{
    [Fact]
    public async Task IssueCertificate_OnApprovedEntry_GeneratesVerifiableCertificate()
    {
        var ctx = await EndToEndTestData.CreateAsync();
        await using (ctx.Db)
        {
            var entry = new Entry
            {
                Uuid = Guid.NewGuid(),
                ReferenceNo = "ENT-CERT-001",
                UserId = ctx.Importer.Id,
                AgencyId = ctx.Agency.Id,
                EntryType = EntryType.Import,
                Status = EntryStatus.Approved,
                PaymentStatus = PaymentStatus.Paid,
                SubmittedAt = DateTime.UtcNow.AddDays(-2),
                Detail = new EntryDetail { CommodityName = "Beef Products", Quantity = 100, Unit = "KG" }
            };
            ctx.Db.Entries.Add(entry);
            await ctx.Db.SaveChangesAsync();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["App:PublicBaseUrl"] = "http://localhost:5173" })
                .Build();

            var issuance = new CertificateIssuanceService(
                ctx.Db,
                new TestCurrentUserService(ctx.Admin.Uuid, "ROLE_ADMIN"),
                new TestFileStorageService(),
                config);

            var issued = await issuance.IssueAsync(new IssueCertificateRequest(entry.Uuid, null, null));
            Assert.Equal(CertificateStatus.Active.ToString(), issued.Status);
            Assert.Equal(entry.ReferenceNo, issued.EntryReferenceNo);

            var cert = await ctx.Db.Certificates.FirstAsync(c => c.EntryId == entry.Id);
            var verify = new CertificateService(ctx.Db, new TestCurrentUserService(ctx.Importer.Uuid), new TestFileStorageService());
            var result = await verify.VerifyAsync(cert.VerificationCode);
            Assert.NotNull(result);
            Assert.True(result!.IsValid);
            Assert.Equal(cert.CertificateNumber, result.CertificateNumber);
        }
    }
}
