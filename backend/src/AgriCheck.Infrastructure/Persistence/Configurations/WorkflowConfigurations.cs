using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class ContainerInspectionPhotoConfiguration : IEntityTypeConfiguration<ContainerInspectionPhoto>
{
    public void Configure(EntityTypeBuilder<ContainerInspectionPhoto> builder)
    {
        builder.ToTable("container_inspection_photos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.PhotoType).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.ReviewDecision).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Container).WithMany(x => x.InspectionPhotos).HasForeignKey(x => x.ContainerId);
        builder.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedByUserId);
        builder.HasIndex(x => new { x.ContainerId, x.PhotoType }).IsUnique();
    }
}

public class ContainerTransportTagConfiguration : IEntityTypeConfiguration<ContainerTransportTag>
{
    public void Configure(EntityTypeBuilder<ContainerTransportTag> builder)
    {
        builder.ToTable("container_transport_tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.TransportType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.QrPayload).HasColumnType("longtext");
        builder.Property(x => x.QrCodeData).HasColumnType("longtext");
        builder.HasOne(x => x.Container).WithMany(x => x.TransportTags).HasForeignKey(x => x.ContainerId);
        builder.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.TaggedBy).WithMany().HasForeignKey(x => x.TaggedByUserId);
    }
}

public class ContainerDoctorInspectionConfiguration : IEntityTypeConfiguration<ContainerDoctorInspection>
{
    public void Configure(EntityTypeBuilder<ContainerDoctorInspection> builder)
    {
        builder.ToTable("container_doctor_inspections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Container).WithMany(x => x.DoctorInspections).HasForeignKey(x => x.ContainerId);
        builder.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorUserId);
        builder.HasIndex(x => x.ContainerId);
    }
}
