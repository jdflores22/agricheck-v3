using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class AgencyNewsPostConfiguration : IEntityTypeConfiguration<AgencyNewsPost>
{
    public void Configure(EntityTypeBuilder<AgencyNewsPost> builder)
    {
        builder.ToTable("agency_news_posts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(1000);
        builder.Property(x => x.Body).HasColumnType("text").IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Category).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.IsPublished, x.PublishedAt });
        builder.HasIndex(x => x.DisplayOrder);
        builder.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId);
    }
}
