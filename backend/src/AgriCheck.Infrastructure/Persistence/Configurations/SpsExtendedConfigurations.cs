using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class SpsIssuingRuleConfiguration : IEntityTypeConfiguration<SpsIssuingRule>
{
    public void Configure(EntityTypeBuilder<SpsIssuingRule> builder)
    {
        builder.ToTable("sps_issuing_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.HsCodePrefix).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(512);
        builder.HasIndex(x => new { x.HsCodePrefix, x.IssuingAgencyId }).IsUnique();
        builder.HasOne(x => x.IssuingAgency).WithMany().HasForeignKey(x => x.IssuingAgencyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SpsDocumentChecklistTemplateConfiguration : IEntityTypeConfiguration<SpsDocumentChecklistTemplate>
{
    public void Configure(EntityTypeBuilder<SpsDocumentChecklistTemplate> builder)
    {
        builder.ToTable("sps_document_checklist_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class SpsApplicationDocumentConfiguration : IEntityTypeConfiguration<SpsApplicationDocument>
{
    public void Configure(EntityTypeBuilder<SpsApplicationDocument> builder)
    {
        builder.ToTable("sps_application_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(512);
        builder.HasIndex(x => new { x.ApplicationId, x.DocumentCode }).IsUnique();
        builder.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.EntryFile).WithMany().HasForeignKey(x => x.EntryFileId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ErfiSubmissionConfiguration : IEntityTypeConfiguration<ErfiSubmission>
{
    public void Configure(EntityTypeBuilder<ErfiSubmission> builder)
    {
        builder.ToTable("erfi_submissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.BolReference).HasMaxLength(64);
        builder.Property(x => x.IfmDestinationJson).HasColumnType("longtext");
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.EntryId).IsUnique();
        builder.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SpsImportClearance).WithMany().HasForeignKey(x => x.SpsImportClearanceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FiledByUser).WithMany().HasForeignKey(x => x.FiledByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
