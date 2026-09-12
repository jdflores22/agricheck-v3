using AgriCheck.Domain.Entities;
using AgriCheck.Domain.Enums;
using AgriCheck.Infrastructure.Services;

var elements = new List<CertificateElement>
{
    new()
    {
        ElementType = CertificateElementType.Shape,
        SortOrder = 1,
        Label = "Bar",
        ConfigJson = """{"x":0,"y":0,"width":22,"height":299,"backgroundColor":"#0a430b","zIndex":1}""",
    },
    new()
    {
        ElementType = CertificateElementType.Text,
        SortOrder = 2,
        Label = "Side",
        ConfigJson = """{"content":"Department of Agriculture","x":-90.8,"y":129.2,"width":203,"height":25,"fontSize":50,"textColor":"#ffffff","rotation":90,"textAlign":"left","zIndex":15}""",
    },
};

var pdf = CertificateTemplatePdfGenerator.Generate(
    new CertificateTemplateVersion { LayoutJson = """{"orientation":"PORTRAIT"}""" },
    elements,
    new Dictionary<string, object?>(),
    "https://example.com",
    null,
    ".");

var path = Path.Combine(Path.GetTempPath(), "rotated-text-v2.pdf");
await File.WriteAllBytesAsync(path, pdf);
Console.WriteLine($"{pdf.Length} bytes -> {path}");
