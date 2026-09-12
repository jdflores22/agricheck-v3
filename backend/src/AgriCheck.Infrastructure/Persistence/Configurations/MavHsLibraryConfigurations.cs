using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class MavHsCategoryConfiguration : IEntityTypeConfiguration<MavHsCategory>
{
    public void Configure(EntityTypeBuilder<MavHsCategory> builder)
    {
        builder.ToTable("mav_hs_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.HsCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.HsCode).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
    }
}

public class MavHsHeadingConfiguration : IEntityTypeConfiguration<MavHsHeading>
{
    public void Configure(EntityTypeBuilder<MavHsHeading> builder)
    {
        builder.ToTable("mav_hs_headings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.HeadingNumber).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasIndex(x => new { x.CategoryId, x.HeadingNumber }).IsUnique();
        builder.HasOne(x => x.Category).WithMany(x => x.Headings).HasForeignKey(x => x.CategoryId);
    }
}

public class MavHsDetailConfiguration : IEntityTypeConfiguration<MavHsDetail>
{
    public void Configure(EntityTypeBuilder<MavHsDetail> builder)
    {
        builder.ToTable("mav_hs_details");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasOne(x => x.Heading).WithMany(x => x.Details).HasForeignKey(x => x.HeadingId);
    }
}
