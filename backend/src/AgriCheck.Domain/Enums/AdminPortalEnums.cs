namespace AgriCheck.Domain.Enums;

public enum FormTemplateStatus
{
    Draft,
    Published,
    Archived
}

public enum CertificateElementType
{
    Text,
    QrCode,
    Image,
    Field,
    Shape,
    Line
}

public enum CertificateProcessType
{
    ImportEntry,
    ExportEntry,
    Accreditation
}
