using AgriCheck.Application.ClientPortal;

namespace AgriCheck.IntegrationTests.Fixtures;

internal sealed class NoOpFileStorage : IFileStorageService
{
    public Task<(string storedFileName, long size)> SaveAsync(Stream stream, string folder, string originalFileName, CancellationToken cancellationToken = default) =>
        Task.FromResult(("test.bin", 0L));

    public string GetPhysicalPath(string folder, string storedFileName) => Path.Combine(folder, storedFileName);

    public string? FindStoredFile(string folder, string storedFileName) =>
        File.Exists(GetPhysicalPath(folder, storedFileName)) ? GetPhysicalPath(folder, storedFileName) : null;
}
