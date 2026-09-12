using AgriCheck.Application.ClientPortal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AgriCheck.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(IHostEnvironment environment, IConfiguration configuration)
    {
        _root = UploadStorage.ResolveRoot(configuration, environment);
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
