using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class CertificateTemplateConfiguration : IEntityTypeConfiguration<CertificateTemplate>
{
    public void Configure(EntityTypeBuilder<CertificateTemplate> builder)
    {
        builder.ToTable("certificate_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
    }
}

public class CertificateTemplateVersionConfiguration : IEntityTypeConfiguration<CertificateTemplateVersion>
{
    public void Configure(EntityTypeBuilder<CertificateTemplateVersion> builder)
    {
        builder.ToTable("certificate_template_versions");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Template).WithMany(x => x.Versions).HasForeignKey(x => x.TemplateId);
        builder.HasIndex(x => new { x.TemplateId, x.VersionNumber }).IsUnique();
    }
}

public class CertificateElementConfiguration : IEntityTypeConfiguration<CertificateElement>
{
    public void Configure(EntityTypeBuilder<CertificateElement> builder)
    {
        builder.ToTable("certificate_elements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ElementType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Label).HasMaxLength(128).IsRequired();
        builder.HasOne(x => x.Version).WithMany(x => x.Elements).HasForeignKey(x => x.VersionId);
    }
}

public class CertificateProcessAssignmentConfiguration : IEntityTypeConfiguration<CertificateProcessAssignment>
{
    public void Configure(EntityTypeBuilder<CertificateProcessAssignment> builder)
    {
        builder.ToTable("certificate_process_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProcessType).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
        builder.HasOne(x => x.Template).WithMany(x => x.ProcessAssignments).HasForeignKey(x => x.TemplateId);
        builder.HasIndex(x => new { x.AgencyId, x.ProcessType, x.IsActive });
    }
}

public class FormTemplateConfiguration : IEntityTypeConfiguration<FormTemplate>
{
    public void Configure(EntityTypeBuilder<FormTemplate> builder)
    {
        builder.ToTable("form_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FormType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
    }
}

public class FormTemplateVersionConfiguration : IEntityTypeConfiguration<FormTemplateVersion>
{
    public void Configure(EntityTypeBuilder<FormTemplateVersion> builder)
    {
        builder.ToTable("form_template_versions");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Template).WithMany(x => x.Versions).HasForeignKey(x => x.TemplateId);
        builder.HasIndex(x => new { x.TemplateId, x.VersionNumber }).IsUnique();
    }
}

public class FormAgencyTagConfiguration : IEntityTypeConfiguration<FormAgencyTag>
{
    public void Configure(EntityTypeBuilder<FormAgencyTag> builder)
    {
        builder.ToTable("form_agency_tags");
        builder.HasKey(x => new { x.TemplateId, x.AgencyId });
        builder.HasOne(x => x.Template).WithMany(x => x.AgencyTags).HasForeignKey(x => x.TemplateId);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
    }
}

public class ProcessingFeeConfigConfiguration : IEntityTypeConfiguration<ProcessingFeeConfig>
{
    public void Configure(EntityTypeBuilder<ProcessingFeeConfig> builder)
    {
        builder.ToTable("processing_fee_configs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
        builder.HasIndex(x => new { x.AgencyId, x.EntryType, x.IsActive });
    }
}
