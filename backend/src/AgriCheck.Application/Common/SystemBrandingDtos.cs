namespace AgriCheck.Application.Common;

public record SystemBrandingDto(
    string SystemName,
    string? SystemLogoUrl,
    string? SpinnerLogoUrl,
    string? FaviconUrl,
    string SpinnerColor,
    string PrimaryColor,
    string FooterText);
