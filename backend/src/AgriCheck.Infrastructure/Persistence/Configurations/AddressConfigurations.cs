using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class AddressRegionConfiguration : IEntityTypeConfiguration<AddressRegion>
{
    public void Configure(EntityTypeBuilder<AddressRegion> builder)
    {
        builder.ToTable("address_regions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class AddressProvinceConfiguration : IEntityTypeConfiguration<AddressProvince>
{
    public void Configure(EntityTypeBuilder<AddressProvince> builder)
    {
        builder.ToTable("address_provinces");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new { x.RegionId, x.Code }).IsUnique();
        builder.HasOne(x => x.Region).WithMany(x => x.Provinces).HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AddressCityConfiguration : IEntityTypeConfiguration<AddressCity>
{
    public void Configure(EntityTypeBuilder<AddressCity> builder)
    {
        builder.ToTable("address_cities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new { x.ProvinceId, x.Code }).IsUnique();
        builder.HasOne(x => x.Province).WithMany(x => x.Cities).HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AddressBarangayConfiguration : IEntityTypeConfiguration<AddressBarangay>
{
    public void Configure(EntityTypeBuilder<AddressBarangay> builder)
    {
        builder.ToTable("address_barangays");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ZipCode).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => new { x.CityId, x.Code }).IsUnique();
        builder.HasOne(x => x.City).WithMany(x => x.Barangays).HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Cascade);
    }
}
