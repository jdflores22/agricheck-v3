using AgriCheck.Application.ClientPortal;
using AgriCheck.Application.Common;
using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AgriCheck.IntegrationTests.Fixtures;

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public TestCurrentUserService(Guid userUuid, params string[] roles)
    {
        UserUuid = userUuid;
        Roles = roles;
    }

    public Guid? UserUuid { get; }
    public long? UserId { get; set; }
    public IReadOnlyList<string> Roles { get; }

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

internal sealed class TestEmailService : IEmailService
{
    public Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal static class TestNotificationFactory
{
    public static NotificationService Create(AgriCheckDbContext db, TestCurrentUserService user) =>
        new(db, user, new TestEmailService(), new NoOpNotificationRealtimePublisher());
}

internal static class AgencyTestData
{
    public static async Task<(AgriCheckDbContext Db, User BaiEvaluator, User BfarEvaluator, Entry BaiEntry, Entry BfarEntry)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AgriCheckDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AgriCheckDbContext(options);

        var bai = new Domain.Entities.Agency { Code = "BAI", Name = "Bureau of Animal Industry", IsActive = true };
        var bfar = new Domain.Entities.Agency { Code = "BFAR", Name = "Bureau of Fisheries and Aquatic Resources", IsActive = true };
        db.Agencies.AddRange(bai, bfar);

        var client = new User
        {
            Uuid = Guid.NewGuid(),
            Email = "client@test.local",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = "Test", LastName = "Client" }
        };
        db.Users.Add(client);

        var baiEvaluator = new User
        {
            Uuid = Guid.NewGuid(),
            Email = "evaluator.bai@test.local",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = "BAI", LastName = "Evaluator" }
        };
        baiEvaluator.AgencyMemberships.Add(new AgencyMembership { Agency = bai, IsPrimary = true });

        var bfarEvaluator = new User
        {
            Uuid = Guid.NewGuid(),
            Email = "evaluator.bfar@test.local",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            Profile = new UserProfile { FirstName = "BFAR", LastName = "Evaluator" }
        };
        bfarEvaluator.AgencyMemberships.Add(new AgencyMembership { Agency = bfar, IsPrimary = true });

        db.Users.AddRange(baiEvaluator, bfarEvaluator);
        await db.SaveChangesAsync();

        var baiEntry = new Entry
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = "ENT-BAI-001",
            UserId = client.Id,
            AgencyId = bai.Id,
            EntryType = EntryType.Import,
            Status = EntryStatus.Submitted,
            PaymentStatus = PaymentStatus.Paid,
            SubmittedAt = DateTime.UtcNow
        };

        var bfarEntry = new Entry
        {
            Uuid = Guid.NewGuid(),
            ReferenceNo = "ENT-BFAR-001",
            UserId = client.Id,
            AgencyId = bfar.Id,
            EntryType = EntryType.Import,
            Status = EntryStatus.Submitted,
            PaymentStatus = PaymentStatus.Paid,
            SubmittedAt = DateTime.UtcNow
        };

        db.Entries.AddRange(baiEntry, bfarEntry);
        await db.SaveChangesAsync();

        return (db, baiEvaluator, bfarEvaluator, baiEntry, bfarEntry);
    }
}
