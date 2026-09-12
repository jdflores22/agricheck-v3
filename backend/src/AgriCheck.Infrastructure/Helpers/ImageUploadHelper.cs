namespace AgriCheck.Infrastructure.Helpers;

internal static class ImageUploadHelper
{
    private static readonly HashSet<string> AgencyLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg",
    };

    public static string? ResolveAgencyLogoExtension(string fileName, string? contentType) =>
        ResolveAllowedImageExtension(fileName, contentType, AgencyLogoExtensions);

    private static string? ResolveAllowedImageExtension(
        string fileName,
        string? contentType,
        IReadOnlySet<string> allowedExtensions)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension) && allowedExtensions.Contains(extension))
        {
            return extension.ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        extension = contentType.Trim().ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => null,
        };

        return extension is not null && allowedExtensions.Contains(extension) ? extension : null;
    }
}
