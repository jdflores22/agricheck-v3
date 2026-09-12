using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AgriCheck.Infrastructure.Services;

public static class UploadStorage
{
    public static string ResolveRoot(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["UPLOADS_ROOT"]
            ?? configuration["App:UploadsRoot"];

        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "storage")
            : configured.Trim();

        root = Path.GetFullPath(root);
        Directory.CreateDirectory(root);
        return root;
    }
}
