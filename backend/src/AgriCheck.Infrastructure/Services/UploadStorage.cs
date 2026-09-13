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

    public static void SeedBundledCertificateTemplateAssets(string uploadsRoot)
    {
        var seedRoot = Path.Combine(AppContext.BaseDirectory, "seed-certificate-templates", "assets");
        if (!Directory.Exists(seedRoot))
        {
            return;
        }

        var targetRoot = Path.Combine(uploadsRoot, "certificate-templates", "assets");
        Directory.CreateDirectory(targetRoot);

        foreach (var sourcePath in Directory.GetFiles(seedRoot))
        {
            var targetPath = Path.Combine(targetRoot, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, targetPath, overwrite: true);
        }
    }
}
