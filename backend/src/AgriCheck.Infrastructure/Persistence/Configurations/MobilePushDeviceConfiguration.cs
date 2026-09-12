using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class MobilePushDeviceConfiguration : IEntityTypeConfiguration<MobilePushDevice>
{
    public void Configure(EntityTypeBuilder<MobilePushDevice> builder)
    {
        builder.ToTable("mobile_push_devices");
        builder.HasIndex(d => d.ExpoPushToken).IsUnique();
        builder.HasIndex(d => new { d.UserId, d.Platform });
        builder.Property(d => d.ExpoPushToken).HasMaxLength(256).IsRequired();
        builder.Property(d => d.Platform).HasMaxLength(32).IsRequired();
        builder.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
