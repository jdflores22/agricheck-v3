using AgriCheck.Application.ClientPortal.Dtos;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.IntegrationTests.Fixtures;

internal static class EndToEndTestData
{
    public sealed record Context(
        AgriCheckDbContext Db,
        Agency Agency,
        User Importer,
        User Admin,
        User MavAdmin,
        User WarehouseStaff,
        User Driver,
        MavApplicationPeriod Period);

    public static async Task<Context> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AgriCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AgriCheckDbContext(options);
        var agency = new Agency { Code = "BAI", Name = "Bureau of Animal Industry", IsActive = true };
        db.Agencies.Add(agency);

        var beef = new CommodityCategory { Code = "LIVESTOCK", Name = "Livestock" };
        db.CommodityCategories.Add(beef);
        await db.SaveChangesAsync();
        var commodity = new Commodity { CategoryId = beef.Id, Code = "BEEF", Name = "Beef Products" };
        db.Commodities.Add(commodity);

        User Make(string email, string first, string last) => new()
        {
            Uuid = Guid.NewGuid(),
            Email = email,
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = first, LastName = last, CompanyName = "Test Corp" }
        };

        var importer = Make("importer@test.local", "Demo", "Importer");
        var admin = Make("admin@test.local", "System", "Admin");
        var mavAdmin = Make("mav.admin@test.local", "MAV", "Admin");
        var warehouse = Make("warehouse@test.local", "WH", "Staff");
        var driver = Make("driver@test.local", "Demo", "Driver");
        db.Users.AddRange(importer, admin, mavAdmin, warehouse, driver);

        var facility = new WarehouseFacility { Code = "WH-TEST", Name = "Test Warehouse", Location = "Manila", Capacity = 100, IsActive = true };
        db.WarehouseFacilities.Add(facility);

        var template = new CertificateTemplate
        {
            Uuid = Guid.NewGuid(),
            Name = "Test Template",
            AgencyId = agency.Id,
            IsActive = true,
            Versions = new List<CertificateTemplateVersion>
            {
                new()
                {
                    VersionNumber = 1,
                    IsPublished = true,
                    Elements = new List<CertificateElement>
                    {
                        new() { ElementType = CertificateElementType.Text, Label = "Title", SortOrder = 1 }
                    }
                }
            }
        };
        db.CertificateTemplates.Add(template);

        db.CertificateProcessAssignments.Add(new CertificateProcessAssignment
        {
            AgencyId = agency.Id,
            Template = template,
            ProcessType = CertificateProcessType.ImportEntry,
            IsActive = true
        });

        var period = new MavApplicationPeriod
        {
            Uuid = Guid.NewGuid(),
            MavYear = 2026,
            PoolType = MavPoolType.BYP,
            OpeningDate = DateTime.UtcNow.AddDays(-1),
            ClosingDate = DateTime.UtcNow.AddMonths(2),
            Status = MavApplicationPeriodStatus.Open,
            CommodityAllocations = new List<MavCommodityAllocation>
            {
                new()
                {
                    CommodityId = commodity.Id,
                    HsCode = "0201",
                    CommodityName = "Beef Products",
                    TotalVolume = 10000m,
                    MinimumImportVolume = 10m
                }
            }
        };
        db.MavApplicationPeriods.Add(period);
        await db.SaveChangesAsync();

        return new Context(db, agency, importer, admin, mavAdmin, warehouse, driver, period);
    }

    public static CreateEntryRequest SampleEntryRequest(long agencyId) => new(
        agencyId,
        "Import",
        new EntryDetailInputDto(null, "Beef Products", "Frozen beef", 100m, "KG", "Australia", "Philippines", "Manila"),
        "E2E test entry",
        null,
        null,
        null);
}
