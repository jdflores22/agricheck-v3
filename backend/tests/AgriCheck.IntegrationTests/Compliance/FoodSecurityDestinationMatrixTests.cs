using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Application.Compliance;
using AgriCheck.Application.DaPortal;
using AgriCheck.Application.DaPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using AgriCheck.IntegrationTests.Fixtures;
using AgriCheck.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace AgriCheck.IntegrationTests.Compliance;

/// <summary>
/// National food-import matrix: regulatory agency × warehouse scope × commodity class,
/// through destination validation and DA import pipeline visibility.
/// </summary>
public class FoodSecurityDestinationMatrixTests
{
    private readonly ITestOutputHelper _output;

    public FoodSecurityDestinationMatrixTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Food_security_matrix_108_validations_and_da_pipeline_sees_created_imports()
    {
        var ctx = await FoodSecurityMatrixFixture.CreateAsync();
        var options = new ComplianceOptions
        {
            EnforceEntryDestinationValidation = true,
            BlockEntrySaveOnDestinationValidation = true,
            RequireImporterWarehouseAllocation = true,
        };

        var validation = IntegrationTestServices.CreateEntryDestinationValidationService(
            ctx.Db,
            ctx.Importer.Uuid,
            options);

        var entryService = IntegrationTestServices.CreateEntryService(ctx.Db, ctx.Importer.Uuid, options);

        var volumes = new[] { 5m, 25m, 100m, 500m };
        var cases = new List<(string Agency, string WhScope, string HsClass, decimal Volume)>();
        foreach (var agency in FoodSecurityMatrixFixture.AgencyCodes)
        {
            foreach (var wh in FoodSecurityMatrixFixture.WarehouseScopes)
            {
                foreach (var hs in FoodSecurityMatrixFixture.CommodityClasses)
                {
                    foreach (var vol in volumes)
                    {
                        cases.Add((agency, wh, hs, vol));
                    }
                }
            }
        }

        Assert.Equal(108, cases.Count);

        var validationMismatches = new List<string>();
        var savesOk = 0;
        var savesBlocked = 0;
        var saveMismatches = new List<string>();
        var createdForPipeline = new List<Guid>();

        foreach (var (agencyCode, whScope, hsClass, volume) in cases)
        {
            var agencyId = ctx.Agencies[agencyCode].Id;
            var warehouseId = ctx.Warehouses[whScope].Id;
            var hsCode = FoodSecurityMatrixFixture.HsCodeFor(hsClass);
            var formJson = FoodSecurityMatrixFixture.FormDataJson(hsCode, volume);
            var expectedValid = FoodSecurityMatrixFixture.ExpectValid(agencyCode, whScope, hsClass);

            var result = await validation.ValidateForImporterAsync(
                ctx.Importer.Id,
                new EntryDestinationValidationRequest(
                    warehouseId,
                    volume,
                    "kg",
                    null,
                    formJson,
                    agencyId),
                CancellationToken.None);

            var hasError = result.Checks.Any(c => c.Severity == "error");
            var actualValid = result.IsValid && !hasError;

            if (actualValid != expectedValid)
            {
                var codes = string.Join(", ", result.Checks.Select(c => $"{c.Code}:{c.Severity}"));
                validationMismatches.Add(
                    $"{agencyCode}/{whScope}/{hsClass} vol={volume} expected={expectedValid} actual={actualValid} [{codes}]");
            }

            if (!expectedValid)
            {
                try
                {
                    await entryService.CreateAsync(new CreateEntryRequest(
                        agencyId,
                        "Import",
                        new EntryDetailInputDto(null, FoodSecurityMatrixFixture.CommodityLabel(hsClass), null, volume, "kg", "AU", "Philippines", "Manila"),
                        "matrix-negative",
                        formJson,
                        0,
                        "[]",
                        null,
                        "Regular",
                        warehouseId));
                    saveMismatches.Add($"{agencyCode}/{whScope}/{hsClass} vol={volume} should have been blocked on save");
                }
                catch (ClientPortalException)
                {
                    savesBlocked++;
                }
                continue;
            }

            try
            {
                var created = await entryService.CreateAsync(new CreateEntryRequest(
                    agencyId,
                    "Import",
                    new EntryDetailInputDto(null, FoodSecurityMatrixFixture.CommodityLabel(hsClass), null, volume, "kg", "AU", "Philippines", "Manila"),
                    $"matrix-{agencyCode}-{whScope}-{hsClass}-{volume}",
                    formJson,
                    0,
                    "[]",
                    null,
                    "Regular",
                    warehouseId));
                savesOk++;
                createdForPipeline.Add(created.Uuid);
            }
            catch (ClientPortalException ex)
            {
                saveMismatches.Add($"{agencyCode}/{whScope}/{hsClass} vol={volume} should save: {ex.Message}");
            }
        }

        const int targetSubmittedImports = 100;
        var extraNeeded = Math.Max(0, targetSubmittedImports - createdForPipeline.Count);
        for (var i = 0; i < extraNeeded; i++)
        {
            var agencyCode = FoodSecurityMatrixFixture.AgencyCodes[i % FoodSecurityMatrixFixture.AgencyCodes.Length];
            var scope = FoodSecurityMatrixFixture.WarehouseScopeForAgency(agencyCode);
            var agencyId = ctx.Agencies[agencyCode].Id;
            var warehouseId = ctx.Warehouses[scope].Id;
            var volume = 10m + i;
            var formJson = FoodSecurityMatrixFixture.FormDataJson(FoodSecurityMatrixFixture.HsCodeFor(scope), volume);
            try
            {
                var created = await entryService.CreateAsync(new CreateEntryRequest(
                    agencyId,
                    "Import",
                    new EntryDetailInputDto(null, FoodSecurityMatrixFixture.CommodityLabel(scope), null, volume, "kg", "AU", "Philippines", "Manila"),
                    $"national-food-batch-{i}",
                    formJson,
                    0,
                    "[]",
                    null,
                    "Regular",
                    warehouseId));
                createdForPipeline.Add(created.Uuid);
            }
            catch (ClientPortalException ex)
            {
                saveMismatches.Add($"batch-{i} ({agencyCode}/{scope}) should save: {ex.Message}");
            }
        }

        var submittedCount = 0;
        var submitFailures = new List<string>();
        foreach (var entryUuid in createdForPipeline)
        {
            try
            {
                await entryService.SubmitAsync(entryUuid);
                submittedCount++;
            }
            catch (ClientPortalException ex)
            {
                submitFailures.Add($"{entryUuid}: {ex.Message}");
            }
        }

        var warehouseStored = 0;
        for (var i = 0; i < createdForPipeline.Count; i++)
        {
            if (i % 2 != 0)
            {
                continue;
            }

            var entry = await ctx.Db.Entries
                .Include(e => e.Detail)
                .FirstAsync(e => e.Uuid == createdForPipeline[i]);
            await FoodSecurityMatrixFixture.StoreInWarehouseAsync(ctx.Db, entry, ctx.Importer.Id);
            warehouseStored++;
        }

        var daUser = new TestCurrentUserService(ctx.DaSecretary.Uuid, "ROLE_DA_SECRETARY");
        var pipeline = new DaOversightReportService(ctx.Db, daUser);
        var report = await pipeline.GetImportPipelineReportAsync(new DaImportPipelineQuery(), CancellationToken.None);

        _output.WriteLine("=== Food security destination matrix (108 scenarios) ===");
        _output.WriteLine($"Validation mismatches: {validationMismatches.Count}");
        _output.WriteLine($"Saves OK (valid combos): {savesOk}");
        _output.WriteLine($"Saves blocked (invalid combos): {savesBlocked}");
        _output.WriteLine($"Drafts created for pipeline: {createdForPipeline.Count}");
        _output.WriteLine($"Submitted to agency: {submittedCount}");
        _output.WriteLine($"Submit failures: {submitFailures.Count}");
        _output.WriteLine($"DA pipeline entries (up to 200): {report.Entries.Count}");
        _output.WriteLine($"DA byImporter rows: {report.ByImporter.Count}");
        _output.WriteLine($"DA byCommodityWarehouse rows: {report.ByCommodityWarehouse.Count}");
        _output.WriteLine($"Warehouse stored (demo intake): {warehouseStored}");
        _output.WriteLine($"DA expected kg (in-flight): {report.TotalExpectedKg}");
        _output.WriteLine($"DA actual kg (in warehouse): {report.TotalActualKg}");
        _output.WriteLine($"Agencies in pipeline: {string.Join(", ", report.Entries.Select(e => e.AgencyCode).Distinct().OrderBy(c => c))}");

        Assert.Empty(validationMismatches);
        Assert.Empty(saveMismatches);
        Assert.Empty(submitFailures);
        Assert.Equal(12, savesOk);
        Assert.Equal(96, savesBlocked);
        Assert.Equal(targetSubmittedImports, submittedCount);
        Assert.Equal(warehouseStored, targetSubmittedImports / 2);
        Assert.True(report.PipelineEntryCount >= warehouseStored / 2,
            "Half of imports remain in-flight; the rest are fully warehoused and appear under Actual.");
        Assert.Equal(3, report.Entries.Select(e => e.AgencyCode).Distinct().Count());
        Assert.True(report.ByImporter.Count >= 1);
        Assert.True(report.ByCommodity.Count >= 3, "Pipeline should break down meat, fish, and plant commodities.");
        Assert.True(report.ByCommodityWarehouse.Count >= 3, "Pipeline should show destination warehouse per commodity class.");
        Assert.True(report.ByCommodityWarehouse.Sum(r => r.ExpectedKg) > 0);
        Assert.True(report.TotalActualKg > 0, "DA pipeline should report volume already inside warehouses.");
        Assert.Contains(report.ByCommodityWarehouse, r => r.ActualKg > 0);
        Assert.True(report.Entries.All(e => e.DestinationWarehouseId.HasValue));
    }

    private sealed class FoodSecurityMatrixFixture
    {
        public static readonly string[] AgencyCodes = { "BAI", "BFAR", "BPI" };
        public static readonly string[] WarehouseScopes = { "meat", "fish", "plant" };
        public static readonly string[] CommodityClasses = { "meat", "fish", "plant" };

        public AgriCheckDbContext Db { get; private init; } = null!;
        public User Importer { get; private init; } = null!;
        public User DaSecretary { get; private init; } = null!;
        public Dictionary<string, Agency> Agencies { get; private init; } = null!;
        public Dictionary<string, WarehouseFacility> Warehouses { get; private init; } = null!;

        public static async Task<FoodSecurityMatrixFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<AgriCheckDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new AgriCheckDbContext(options);

            var agencies = new Dictionary<string, Agency>();
            foreach (var code in AgencyCodes)
            {
                var agency = new Agency { Code = code, Name = code, IsActive = true };
                db.Agencies.Add(agency);
                agencies[code] = agency;
            }

            var importer = new User
            {
                Uuid = Guid.NewGuid(),
                Email = "food.importer@test.local",
                PasswordHash = "hash",
                Status = UserStatus.Active,
                Profile = new UserProfile { FirstName = "Food", LastName = "Importer", CompanyName = "National Food Corp" },
            };
            var daSecretary = new User
            {
                Uuid = Guid.NewGuid(),
                Email = "da.secretary@test.local",
                PasswordHash = "hash",
                Status = UserStatus.Active,
                Profile = new UserProfile { FirstName = "DA", LastName = "Secretary" },
            };
            db.Users.AddRange(importer, daSecretary);
            await db.SaveChangesAsync();

            var warehouses = new Dictionary<string, WarehouseFacility>();
            await SeedWarehouseAsync(db, importer.Id, agencies["BAI"].Id, "WH-MEAT", "Meat Hub", ProductsJson("MeatAndMeatProducts"), warehouses, "meat", importer);
            await SeedWarehouseAsync(db, importer.Id, agencies["BFAR"].Id, "WH-FISH", "Fish Hub", ProductsJson("FishAndAquaticProducts"), warehouses, "fish", importer);
            await SeedWarehouseAsync(db, importer.Id, agencies["BPI"].Id, "WH-PLANT", "Plant Hub", ProductsJson("PlantProducts"), warehouses, "plant", importer);

            foreach (var agencyCode in AgencyCodes)
            {
                await ComplianceTestData.SeedImporterComplianceAsync(
                    db,
                    importer.Id,
                    agencies[agencyCode].Id,
                    warehouses[WarehouseScopeForAgency(agencyCode)].Id);
            }

            await ComplianceTestData.SeedAccreditationAsync(db, importer.Id);

            return new FoodSecurityMatrixFixture
            {
                Db = db,
                Importer = importer,
                DaSecretary = daSecretary,
                Agencies = agencies,
                Warehouses = warehouses,
            };
        }

        public static string WarehouseScopeForAgency(string agencyCode) =>
            agencyCode switch
            {
                "BAI" => "meat",
                "BFAR" => "fish",
                "BPI" => "plant",
                _ => "meat",
            };

        private static string ProductsJson(string commodityClass) => $"[\"{commodityClass}\"]";

        private static async Task SeedWarehouseAsync(
            AgriCheckDbContext db,
            long importerId,
            long leadAgencyId,
            string code,
            string name,
            string productsJson,
            Dictionary<string, WarehouseFacility> map,
            string scopeKey,
            User importer)
        {
            var facility = new WarehouseFacility
            {
                Code = code,
                Name = name,
                Location = "PH",
                Capacity = 1000,
                IsActive = true,
            };
            db.WarehouseFacilities.Add(facility);
            await db.SaveChangesAsync();

            db.WarehouseOperatingLicenses.Add(new WarehouseOperatingLicense
            {
                WarehouseFacilityId = facility.Id,
                LicenseNumber = $"DA-LTO-{code}",
                Status = WarehouseOperatingLicenseStatus.Active,
                IssuedAt = DateTime.UtcNow.AddYears(-1),
                ExpiresAt = DateTime.UtcNow.AddYears(2),
                IssuedByUserId = importerId,
            });

            db.ImporterFacilityAffiliations.Add(new ImporterFacilityAffiliation
            {
                UserId = importerId,
                WarehouseFacilityId = facility.Id,
            });

            var registration = new FacilityRegistration
            {
                Uuid = Guid.NewGuid(),
                WarehouseFacilityId = facility.Id,
                SubmittedByUserId = importerId,
                Status = FacilityRegistrationStatus.Registered,
                OwnerName = $"{name} Operator",
                MaxCapacityKg = 500_000,
                ProductsJson = productsJson,
                RegisteredAt = DateTime.UtcNow.AddMonths(-6),
                ExpiresAt = DateTime.UtcNow.AddYears(1),
            };
            db.FacilityRegistrations.Add(registration);
            await db.SaveChangesAsync();

            db.FacilityAccreditations.Add(new FacilityAccreditation
            {
                Uuid = Guid.NewGuid(),
                FacilityRegistrationId = registration.Id,
                WarehouseFacilityId = facility.Id,
                LeadAgencyId = leadAgencyId,
                Status = FacilityAccreditationStatus.Active,
                CertificateNumber = $"CSW-{code}",
                AccreditedAt = DateTime.UtcNow.AddMonths(-5),
                ExpiresAt = DateTime.UtcNow.AddYears(1),
            });

            var allowedJson = productsJson;
            db.WarehouseImporterAllocations.Add(new WarehouseImporterAllocation
            {
                ImporterUserId = importerId,
                WarehouseFacilityId = facility.Id,
                ContractReference = $"CTR-{code}",
                AllocatedCapacityKg = 400_000,
                AllowedCommodityClassesJson = allowedJson,
                Status = WarehouseImporterAllocationStatus.Active,
                ValidFrom = DateTime.UtcNow.AddMonths(-3),
            });

            await db.SaveChangesAsync();
            map[scopeKey] = facility;
        }

        public static string HsCodeFor(string hsClass) =>
            hsClass switch
            {
                "meat" => "0203.11",
                "fish" => "0302.11",
                "plant" => "0701.10",
                _ => "0201.10",
            };

        public static string CommodityLabel(string hsClass) =>
            hsClass switch
            {
                "meat" => "Frozen pork",
                "fish" => "Frozen fish fillet",
                "plant" => "Fresh potatoes",
                _ => "Agri commodity",
            };

        public static string FormDataJson(string hsCode, decimal quantity) =>
            $"{{\"import_commodity_hs_code\":\"{hsCode}\",\"import_commodity_commodity_name\":\"test\",\"quantity\":\"{quantity}\",\"unit\":\"kg\"}}";

        public static bool ExpectValid(string agencyCode, string warehouseScope, string commodityClass) =>
            agencyCode switch
            {
                "BAI" => warehouseScope == "meat" && commodityClass == "meat",
                "BFAR" => warehouseScope == "fish" && commodityClass == "fish",
                "BPI" => warehouseScope == "plant" && commodityClass == "plant",
                _ => false,
            };

        public static async Task StoreInWarehouseAsync(AgriCheckDbContext db, Entry entry, long receivedByUserId)
        {
            if (!entry.FinalWarehouseFacilityId.HasValue)
            {
                return;
            }

            var facilityId = entry.FinalWarehouseFacilityId.Value;
            var container = new Container
            {
                Uuid = Guid.NewGuid(),
                EntryId = entry.Id,
                SequenceNumber = 1,
                ContainerNumber = $"MAT-{entry.Id:D6}",
                ContainerType = "40FT",
                Status = ContainerStatus.AtWarehouse,
                WarehouseFacilityId = facilityId,
                ArrivalTime = DateTime.UtcNow.AddHours(-4),
            };
            db.Containers.Add(container);
            await db.SaveChangesAsync();

            db.ContainerDoctorInspections.Add(new ContainerDoctorInspection
            {
                Uuid = Guid.NewGuid(),
                ContainerId = container.Id,
                DoctorUserId = receivedByUserId,
                Status = DoctorInspectionStatus.Approved,
                CompletedAt = DateTime.UtcNow.AddHours(-5),
            });

            db.WarehouseInventories.Add(new WarehouseInventory
            {
                Uuid = Guid.NewGuid(),
                ContainerId = container.Id,
                WarehouseFacilityId = facilityId,
                LocationCode = "DEMO-A1",
                ReceivedAt = DateTime.UtcNow.AddHours(-2),
                ReceivedByUserId = receivedByUserId,
                Status = WarehouseInventoryStatus.Stored,
            });

            entry.Status = EntryStatus.InTransit;
            entry.CompliancePhase = EntryCompliancePhase.AtWarehouse;
            await db.SaveChangesAsync();
        }
    }
}
