using AgriCheck.Application.Compliance;
using AgriCheck.Application.OpsPortal.Dtos;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using AgriCheck.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriCheck.IntegrationTests.Compliance;

public class ComplianceSpineFlowTests
{
    [Fact]
    public async Task ImportCase_Phases0Through9_HappyPath_ReachesReleased()
    {
        var wired = await ComplianceTestData.CreatePortReadyImportAsync();
        await using (wired.Db)
        {
            var gate = IntegrationTestServices.CreateComplianceGateService(wired.Db);
            var importerId = wired.Base.Importer.Id;

            var createGate = await gate.EvaluateAsync(new ComplianceGateContext(
                importerId, 0, wired.Base.Agency.Id, ComplianceGatePhase.EntryCreate));
            Assert.True(createGate.IsCompliant);

            var entryService = IntegrationTestServices.CreateEntryService(wired.Db, wired.Base.Importer.Uuid);
            var draft = await entryService.CreateAsync(
                EndToEndTestData.SampleEntryRequest(wired.Base.Agency.Id) with { FinalWarehouseFacilityId = wired.Facility.Id });
            Assert.Equal(EntryCompliancePhase.Draft.ToString(), draft.CompliancePhase);

            var submitted = await entryService.SubmitAsync(draft.Uuid);
            Assert.Equal(EntryCompliancePhase.AccreditationReady.ToString(), submitted.CompliancePhase);

            var spsicGate = await gate.EvaluateAsync(new ComplianceGateContext(
                importerId,
                wired.Entry.Id,
                wired.Base.Agency.Id,
                ComplianceGatePhase.SpsicApplicationStart,
                wired.Facility.Id));
            Assert.True(spsicGate.IsCompliant);

            var portService = new AgencyPortInspectionService(wired.Db, new TestCurrentUserService(wired.AgencyEvaluator.Uuid, "ROLE_EVALUATOR"));
            await portService.RecordPortDabiAsync(
                wired.Entry.Uuid,
                new RecordPortDabiInspectionRequest("Cleared at port", StampSpsUsed: true, CompletedAt: null));

            var entryAfterPort = await wired.Db.Entries.FirstAsync(e => e.Id == wired.Entry.Id);
            Assert.Equal(EntryCompliancePhase.PortCleared, entryAfterPort.CompliancePhase);

            var chain = await IntegrationTestServices
                .CreateComplianceChainService(wired.Db, wired.Base.Importer.Uuid)
                .GetForEntryAsync(wired.Entry.Uuid);
            Assert.NotNull(chain);
            Assert.Equal("Complete", chain.Accreditation.Status);
            Assert.Equal("Complete", chain.AgencyLicense.Status);
            Assert.Equal("Complete", chain.DestinationFacility.Status);
            Assert.Equal("Complete", chain.SpsImportClearance.Status);
            Assert.Equal("Complete", chain.Erfi.Status);

            var warehouse = IntegrationTestServices.CreateWarehouseOpsService(
                wired.Db,
                wired.Base.WarehouseStaff.Uuid,
                "ROLE_WAREHOUSE_STAFF",
                new ComplianceOptions { RequireCswHaulCompliance = true });

            var inventory = await warehouse.ReceiveContainerAsync(
                new ReceiveContainerRequest(wired.Container.Uuid, wired.Facility.Id, "P8-A1"));
            Assert.Equal(WarehouseInventoryStatus.Stored.ToString(), inventory.Status);
            entryAfterPort = await wired.Db.Entries.FirstAsync(e => e.Id == wired.Entry.Id);
            Assert.Equal(EntryCompliancePhase.AtWarehouse, entryAfterPort.CompliancePhase);
            Assert.Equal(1, await wired.Db.StorageTallySheets.CountAsync(t => t.Direction == "In"));

            var auth = await warehouse.CreateReleaseAuthorizationAsync(
                new CreateReleaseAuthorizationRequest(wired.Entry.Uuid, "Recipient", "ID-P8"));
            await warehouse.ExecuteReleaseAsync(new ExecuteReleaseRequest(inventory.Uuid, auth.Uuid, null));

            wired.Db.ChangeTracker.Clear();
            entryAfterPort = await wired.Db.Entries.AsNoTracking().FirstAsync(e => e.Id == wired.Entry.Id);
            Assert.Equal(EntryCompliancePhase.Released, entryAfterPort.CompliancePhase);
            Assert.Equal(1, await wired.Db.StorageTallySheets.CountAsync(t => t.Direction == "Out"));
        }
    }
}
