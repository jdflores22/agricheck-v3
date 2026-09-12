using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.IntegrationTests.Fixtures;

internal static class OpsTestData
{
    public static async Task<(AgriCheckDbContext Db, User WarehouseStaff, User Driver, User OtherDriver, Container AssignedContainer, Container OtherContainer, Entry Entry, WarehouseFacility Facility)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AgriCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AgriCheckDbContext(options);
        var agency = new Agency { Code = "BAI", Name = "BAI", IsActive = true };
        db.Agencies.Add(agency);

        var client = new User
        {
            Uuid = Guid.NewGuid(),
            Email = "client@test.local",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = "Client", LastName = "User" }
        };

        var warehouseStaff = new User
        {
            Uuid = Guid.NewGuid(),
            Email = "warehouse@test.local",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = "Warehouse", LastName = "Staff" }
        };

        var driver = new User
        {
            Uuid = Guid.NewGuid(),
            Email = "driver@test.local",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = "Main", LastName = "Driver" }
        };

        var otherDriver = new User
        {
            Uuid = Guid.NewGuid(),
            Email = "other-driver@test.local",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = "Other", LastName = "Driver" }
        };

        db.Users.AddRange(client, warehouseStaff, driver, otherDriver);
        await db.SaveChangesAsync();

        var entry = new Entry
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = "ENT-OPS-001",
            UserId = client.Id,
            AgencyId = agency.Id,
            EntryType = EntryType.Import,
            Status = EntryStatus.Approved,
            PaymentStatus = PaymentStatus.Paid,
            SubmittedAt = DateTime.UtcNow
        };
        db.Entries.Add(entry);

        var facility = new WarehouseFacility { Code = "WH-TEST", Name = "Test Warehouse", Location = "Manila", Capacity = 100, IsActive = true };
        db.WarehouseFacilities.Add(facility);
        await db.SaveChangesAsync();

        var assignedContainer = new Container
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            ContainerNumber = "CONT-001",
            Status = ContainerStatus.Assigned,
            AssignedDriverUserId = driver.Id
        };

        var otherContainer = new Container
        {
            Uuid = Guid.NewGuid(),
            EntryId = entry.Id,
            ContainerNumber = "CONT-002",
            Status = ContainerStatus.Assigned,
            AssignedDriverUserId = otherDriver.Id
        };

        db.Containers.AddRange(assignedContainer, otherContainer);
        await db.SaveChangesAsync();

        return (db, warehouseStaff, driver, otherDriver, assignedContainer, otherContainer, entry, facility);
    }
}
