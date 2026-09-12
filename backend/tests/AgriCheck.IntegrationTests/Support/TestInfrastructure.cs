using AgriCheck.Application.ClientPortal;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AgriCheck.IntegrationTests.Fixtures;

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public TestHostEnvironment()
    {
        ContentRootPath = Path.Combine(Path.GetTempPath(), "agricheck-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ContentRootPath);
    }

    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "AgriCheck.Tests";
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}

internal sealed class TestFileStorageService : IFileStorageService
{
    private readonly string _root;

    public TestFileStorageService()
    {
        _root = Path.Combine(Path.GetTempPath(), "agricheck-storage", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public async Task<(string storedFileName, long size)> SaveAsync(Stream stream, string folder, string originalFileName, CancellationToken cancellationToken = default)
    {
        var safeFolder = Path.Combine(_root, folder);
        Directory.CreateDirectory(safeFolder);
        var ext = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(safeFolder, storedFileName);
        await using var fileStream = File.Create(path);
        await stream.CopyToAsync(fileStream, cancellationToken);
        return (storedFileName, fileStream.Length);
    }

    public string GetPhysicalPath(string folder, string storedFileName) =>
        Path.Combine(_root, folder, storedFileName);
}
