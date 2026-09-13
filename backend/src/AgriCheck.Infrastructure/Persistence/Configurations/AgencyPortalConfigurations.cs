using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class EvaluatorAssignmentConfiguration : IEntityTypeConfiguration<EvaluatorAssignment>
{
    public void Configure(EntityTypeBuilder<EvaluatorAssignment> builder)
    {
        builder.ToTable("evaluator_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Entry).WithMany(x => x.EvaluatorAssignments).HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
        builder.HasOne(x => x.Evaluator).WithMany().HasForeignKey(x => x.EvaluatorUserId);
        builder.HasIndex(x => new { x.EntryId, x.EvaluatorUserId, x.Status });
    }
}

public class InspectorAssignmentConfiguration : IEntityTypeConfiguration<InspectorAssignment>
{
    public void Configure(EntityTypeBuilder<InspectorAssignment> builder)
    {
        builder.ToTable("inspector_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Container).WithMany(x => x.InspectorAssignments).HasForeignKey(x => x.ContainerId);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
        builder.HasOne(x => x.Inspector).WithMany().HasForeignKey(x => x.InspectorUserId);
        builder.HasIndex(x => new { x.ContainerId, x.InspectorUserId, x.Status });
    }
}

public class FileEvaluationConfiguration : IEntityTypeConfiguration<FileEvaluation>
{
    public void Configure(EntityTypeBuilder<FileEvaluation> builder)
    {
        builder.ToTable("file_evaluations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.EntryFile).WithMany(x => x.Evaluations).HasForeignKey(x => x.EntryFileId);
        builder.HasOne(x => x.Evaluator).WithMany().HasForeignKey(x => x.EvaluatorUserId);
        builder.HasIndex(x => new { x.EntryFileId, x.EvaluatorUserId }).IsUnique();
    }
}

public class EvaluatorNoteConfiguration : IEntityTypeConfiguration<EvaluatorNote>
{
    public void Configure(EntityTypeBuilder<EvaluatorNote> builder)
    {
        builder.ToTable("evaluator_notes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(2000).IsRequired();
        builder.HasOne(x => x.Entry).WithMany(x => x.EvaluatorNotes).HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorUserId);
    }
}

public class ComplianceChecklistConfiguration : IEntityTypeConfiguration<ComplianceChecklist>
{
    public void Configure(EntityTypeBuilder<ComplianceChecklist> builder)
    {
        builder.ToTable("compliance_checklists");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
    }
}

public class ComplianceChecklistItemConfiguration : IEntityTypeConfiguration<ComplianceChecklistItem>
{
    public void Configure(EntityTypeBuilder<ComplianceChecklistItem> builder)
    {
        builder.ToTable("compliance_checklist_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(256).IsRequired();
        builder.HasOne(x => x.Checklist).WithMany(x => x.Items).HasForeignKey(x => x.ChecklistId);
    }
}

public class EntryComplianceResultConfiguration : IEntityTypeConfiguration<EntryComplianceResult>
{
    public void Configure(EntityTypeBuilder<EntryComplianceResult> builder)
    {
        builder.ToTable("entry_compliance_results");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasOne(x => x.Entry).WithMany(x => x.ComplianceResults).HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.ChecklistItem).WithMany(x => x.Results).HasForeignKey(x => x.ChecklistItemId);
        builder.HasOne(x => x.EvaluatedBy).WithMany().HasForeignKey(x => x.EvaluatedByUserId);
        builder.HasIndex(x => new { x.EntryId, x.ChecklistItemId }).IsUnique();
    }
}

public class InspectionConfiguration : IEntityTypeConfiguration<Inspection>
{
    public void Configure(EntityTypeBuilder<Inspection> builder)
    {
        builder.ToTable("inspections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Entry).WithMany(x => x.Inspections).HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
        builder.HasOne(x => x.Inspector).WithMany().HasForeignKey(x => x.InspectorUserId);
    }
}

public class InspectionPhotoConfiguration : IEntityTypeConfiguration<InspectionPhoto>
{
    public void Configure(EntityTypeBuilder<InspectionPhoto> builder)
    {
        builder.ToTable("inspection_photos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasOne(x => x.Inspection).WithMany(x => x.Photos).HasForeignKey(x => x.InspectionId);
    }
}

public class AgencyBillingChargeConfiguration : IEntityTypeConfiguration<AgencyBillingCharge>
{
    public void Configure(EntityTypeBuilder<AgencyBillingCharge> builder)
    {
        builder.ToTable("billing_charges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.HasOne(x => x.AgencyBilling).WithMany(x => x.Charges).HasForeignKey(x => x.AgencyBillingId);
    }
}

public class AgencyBillingConfiguration : IEntityTypeConfiguration<AgencyBilling>
{
    public void Configure(EntityTypeBuilder<AgencyBilling> builder)
    {
        builder.ToTable("billings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.BillNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.BillNumber).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PaymentReference).HasMaxLength(128);
        builder.Property(x => x.PaymentProofStoredFileName).HasMaxLength(256);
        builder.Property(x => x.PaymentProofOriginalFileName).HasMaxLength(256);
        builder.Property(x => x.PaymentProofContentType).HasMaxLength(128);
        builder.Property(x => x.PaymentNotes).HasMaxLength(1000);
        builder.Property(x => x.VerificationNotes).HasMaxLength(1000);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
        builder.HasOne(x => x.Entry).WithMany(x => x.AgencyBillings).HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.IssuedBy).WithMany().HasForeignKey(x => x.IssuedByUserId);
        builder.HasOne(x => x.VerifiedBy).WithMany().HasForeignKey(x => x.VerifiedByUserId);
    }
}

public class SubmissionFileReviewConfiguration : IEntityTypeConfiguration<SubmissionFileReview>
{
    public void Configure(EntityTypeBuilder<SubmissionFileReview> builder)
    {
        builder.ToTable("submission_file_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.SubmissionFile).WithMany(x => x.Reviews).HasForeignKey(x => x.SubmissionFileId);
        builder.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerUserId);
        builder.HasIndex(x => new { x.SubmissionFileId, x.ReviewerUserId }).IsUnique();
    }
}
