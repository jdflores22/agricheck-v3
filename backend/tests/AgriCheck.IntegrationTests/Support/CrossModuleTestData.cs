using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.IntegrationTests.Fixtures;

internal static class CrossModuleTestData
{
    public static async Task<(AgriCheckDbContext Db, User Admin, User Importer, User Evaluator, User Warehouse, User Driver)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AgriCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AgriCheckDbContext(options);
        var agency = new Agency { Code = "BAI", Name = "BAI", IsActive = true };
        db.Agencies.Add(agency);

        User MakeUser(string email, string first, string last) => new()
        {
            Uuid = Guid.NewGuid(),
            Email = email,
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = first, LastName = last }
        };

        var admin = MakeUser("admin@test.local", "Admin", "User");
        var importer = MakeUser("importer@test.local", "Import", "User");
        var evaluator = MakeUser("evaluator@test.local", "Eval", "User");
        var warehouse = MakeUser("warehouse@test.local", "WH", "Staff");
        var driver = MakeUser("driver@test.local", "Drive", "User");

        evaluator.AgencyMemberships.Add(new AgencyMembership { Agency = agency, IsPrimary = true });

        db.Users.AddRange(admin, importer, evaluator, warehouse, driver);
        await db.SaveChangesAsync();

        db.Entries.Add(new Entry
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = "ENT-001",
            UserId = importer.Id,
            AgencyId = agency.Id,
            EntryType = EntryType.Import,
            Status = EntryStatus.Submitted,
            PaymentStatus = PaymentStatus.Paid,
            SubmittedAt = DateTime.UtcNow
        });

        db.MavApplicationPeriods.Add(new MavApplicationPeriod
        {
            Uuid = Guid.NewGuid(),
            MavYear = 2026,
            PoolType = MavPoolType.BYP,
            OpeningDate = DateTime.UtcNow.AddDays(-1),
            ClosingDate = DateTime.UtcNow.AddMonths(1),
            Status = MavApplicationPeriodStatus.Open
        });

        await db.SaveChangesAsync();

        var entry = await db.Entries.FirstAsync();
        db.Containers.Add(new Container
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            ContainerNumber = "CONT-001",
            Status = ContainerStatus.Assigned,
            AssignedDriverUserId = driver.Id
        });

        await db.SaveChangesAsync();
        return (db, admin, importer, evaluator, warehouse, driver);
    }
}
