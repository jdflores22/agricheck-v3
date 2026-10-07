using AgriCheck.Application.Compliance;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using AgriCheck.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriCheck.IntegrationTests.Compliance;

public class ComplianceGateBlockerTests
{
    [Fact]
    public async Task EntryCreate_WithoutAccreditation_IsBlocked()
    {
        var ctx = await EndToEndTestData.CreateAsync();
        await using (ctx.Db)
        {
            var gate = IntegrationTestServices.CreateComplianceGateService(ctx.Db);
            var result = await gate.EvaluateAsync(new ComplianceGateContext(
                ctx.Importer.Id, 0, ctx.Agency.Id, ComplianceGatePhase.EntryCreate));

            Assert.False(result.IsCompliant);
            Assert.Contains(result.Blockers, b => b.Code == "COMPLIANCE_ACCREDITATION");
        }
    }

    [Fact]
    public async Task SpsicApplicationStart_WithoutAgencyLicense_IsBlocked()
    {
        var ctx = await EndToEndTestData.CreateAsync();
        await using (ctx.Db)
        {
            await ComplianceTestData.SeedAccreditationAsync(ctx.Db, ctx.Importer.Id);
            var facility = await ctx.Db.WarehouseFacilities.FirstAsync();
            await ComplianceTestData.SeedImporterComplianceAsync(ctx.Db, ctx.Importer.Id, ctx.Agency.Id, facility.Id);

            var licenses = await ctx.Db.AgencyLicenses.Where(l => l.UserId == ctx.Importer.Id).ToListAsync();
            ctx.Db.AgencyLicenses.RemoveRange(licenses);
            await ctx.Db.SaveChangesAsync();

            var entry = new Entry
            {
                Uuid = Guid.NewGuid(),
                ReferenceNo = "ENT-GATE-LIC",
                UserId = ctx.Importer.Id,
                AgencyId = ctx.Agency.Id,
                EntryType = EntryType.Import,
                Status = EntryStatus.Draft,
                FinalWarehouseFacilityId = facility.Id
            };
            ctx.Db.Entries.Add(entry);
            await ctx.Db.SaveChangesAsync();

            var gate = IntegrationTestServices.CreateComplianceGateService(ctx.Db);
            var result = await gate.EvaluateAsync(new ComplianceGateContext(
                ctx.Importer.Id,
                entry.Id,
                ctx.Agency.Id,
                ComplianceGatePhase.SpsicApplicationStart,
                facility.Id));

            Assert.False(result.IsCompliant);
            Assert.Contains(result.Blockers, b => b.Code == "COMPLIANCE_AGENCY_LICENSE");
        }
    }

    [Fact]
    public async Task SpsicApplicationStart_DestinationNotReady_IsBlocked()
    {
        var ctx = await EndToEndTestData.CreateAsync();
        await using (ctx.Db)
        {
            await ComplianceTestData.SeedAccreditationAsync(ctx.Db, ctx.Importer.Id);
            var facility = await ctx.Db.WarehouseFacilities.FirstAsync();
            await ComplianceTestData.SeedImporterComplianceAsync(ctx.Db, ctx.Importer.Id, ctx.Agency.Id, facility.Id);

            var registrations = await ctx.Db.FacilityRegistrations.Where(r => r.WarehouseFacilityId == facility.Id).ToListAsync();
            ctx.Db.FacilityRegistrations.RemoveRange(registrations);
            var accreditations = await ctx.Db.FacilityAccreditations.Where(a => a.WarehouseFacilityId == facility.Id).ToListAsync();
            ctx.Db.FacilityAccreditations.RemoveRange(accreditations);
            await ctx.Db.SaveChangesAsync();

            var entry = new Entry
            {
                Uuid = Guid.NewGuid(),
                ReferenceNo = "ENT-GATE-DEST",
                UserId = ctx.Importer.Id,
                AgencyId = ctx.Agency.Id,
                EntryType = EntryType.Import,
                Status = EntryStatus.Draft,
                FinalWarehouseFacilityId = facility.Id
            };
            ctx.Db.Entries.Add(entry);
            await ctx.Db.SaveChangesAsync();

            var gate = IntegrationTestServices.CreateComplianceGateService(
                ctx.Db,
                new ComplianceOptions { RequireDestinationForSpsic = true });
            var result = await gate.EvaluateAsync(new ComplianceGateContext(
                ctx.Importer.Id,
                entry.Id,
                ctx.Agency.Id,
                ComplianceGatePhase.SpsicApplicationStart,
                facility.Id));

            Assert.False(result.IsCompliant);
            Assert.Contains(result.Blockers, b => b.Code == "COMPLIANCE_DESTINATION_RSAS_CSW");
        }
    }

    [Fact]
    public async Task WarehouseReceive_WithCswCompliance_MissingErfi_IsBlocked()
    {
        var wired = await ComplianceTestData.CreatePortReadyImportAsync();
        await using (wired.Db)
        {
            var erfi = await wired.Db.ErfiSubmissions.FirstAsync(e => e.EntryId == wired.Entry.Id);
            wired.Db.ErfiSubmissions.Remove(erfi);
            await wired.Db.SaveChangesAsync();

            var warehouse = IntegrationTestServices.CreateWarehouseOpsService(
                wired.Db,
                wired.Base.WarehouseStaff.Uuid,
                "ROLE_WAREHOUSE_STAFF",
                new ComplianceOptions { RequireCswHaulCompliance = true });

            var ex = await Assert.ThrowsAsync<ClientPortalException>(() =>
                warehouse.ReceiveContainerAsync(new ReceiveContainerRequest(wired.Container.Uuid, wired.Facility.Id, "X-01")));
            Assert.Equal("COMPLIANCE_ERFI_REQUIRED", ex.Code);
        }
    }
}
