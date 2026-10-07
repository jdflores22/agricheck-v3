using AgriCheck.Domain.Entities;
using AgriCheck.Infrastructure.Services;

namespace AgriCheck.IntegrationTests.Support;

internal sealed class NoOpEntryCertificateAutoIssueService : IEntryCertificateAutoIssueService
{
    public Task<bool> TryIssueForEntryAsync(Entry entry, long issuedByUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
