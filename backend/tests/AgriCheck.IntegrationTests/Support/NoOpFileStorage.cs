using AgriCheck.Application.ClientPortal;

namespace AgriCheck.IntegrationTests.Fixtures;

internal sealed class NoOpFileStorage : IFileStorageService
{
    public Task<(string storedFileName, long size)> SaveAsync(Stream stream, string folder, string originalFileName, CancellationToken cancellationToken = default) =>
        Task.FromResult(("test.bin", 0L));

    public string GetPhysicalPath(string folder, string storedFileName) => Path.Combine(folder, storedFileName);
}
