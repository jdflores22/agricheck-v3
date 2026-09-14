using AgriCheck.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgriCheck.Infrastructure.Persistence.Configurations;

public class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("containers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.ContainerNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ContainerNumber);
        builder.Property(x => x.ContainerType).HasMaxLength(50);
        builder.Property(x => x.FormDataJson).HasColumnType("json");
        builder.Property(x => x.SequenceNumber).HasDefaultValue(1);
        builder.HasIndex(x => new { x.EntryId, x.SequenceNumber }).IsUnique();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne(x => x.Entry).WithMany(x => x.Containers).HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.ClaimedBy).WithMany().HasForeignKey(x => x.ClaimedByUserId);
        builder.HasOne(x => x.AssignedDriver).WithMany().HasForeignKey(x => x.AssignedDriverUserId);
        builder.HasOne(x => x.AssignedOperatorVehicle).WithMany().HasForeignKey(x => x.AssignedOperatorVehicleId);
    }
}

public class OperatorVehicleConfiguration : IEntityTypeConfiguration<OperatorVehicle>
{
    public void Configure(EntityTypeBuilder<OperatorVehicle> builder)
    {
        builder.ToTable("operator_vehicles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.PlateNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.OperatorUserId, x.PlateNumber }).IsUnique();
        builder.Property(x => x.VehicleType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(255);
        builder.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId);
        builder.HasOne(x => x.DefaultDriver).WithMany().HasForeignKey(x => x.DefaultDriverUserId);
    }
}

public class ContainerLocationConfiguration : IEntityTypeConfiguration<ContainerLocation>
{
    public void Configure(EntityTypeBuilder<ContainerLocation> builder)
    {
        builder.ToTable("container_locations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Latitude).HasPrecision(10, 7);
        builder.Property(x => x.Longitude).HasPrecision(10, 7);
        builder.HasIndex(x => x.ContainerId);
        builder.HasOne(x => x.Container).WithMany(x => x.Locations).HasForeignKey(x => x.ContainerId);
    }
}

public class WarehouseInventoryConfiguration : IEntityTypeConfiguration<WarehouseInventory>
{
    public void Configure(EntityTypeBuilder<WarehouseInventory> builder)
    {
        builder.ToTable("warehouse_inventory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.LocationCode).HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasOne(x => x.Container).WithMany(x => x.Inventories).HasForeignKey(x => x.ContainerId);
        builder.HasOne(x => x.WarehouseFacility).WithMany().HasForeignKey(x => x.WarehouseFacilityId);
        builder.HasOne(x => x.ReceivedBy).WithMany().HasForeignKey(x => x.ReceivedByUserId);
    }
}

public class ReleaseAuthorizationConfiguration : IEntityTypeConfiguration<ReleaseAuthorization>
{
    public void Configure(EntityTypeBuilder<ReleaseAuthorization> builder)
    {
        builder.ToTable("release_authorizations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.RecipientName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.RecipientIdNumber).HasMaxLength(100).IsRequired();
        builder.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId);
        builder.HasOne(x => x.AuthorizedBy).WithMany().HasForeignKey(x => x.AuthorizedByUserId);
    }
}

public class ReleaseRecordConfiguration : IEntityTypeConfiguration<ReleaseRecord>
{
    public void Configure(EntityTypeBuilder<ReleaseRecord> builder)
    {
        builder.ToTable("release_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.RecipientSignaturePath).HasMaxLength(500);
        builder.HasOne(x => x.WarehouseInventory).WithMany(x => x.ReleaseRecords).HasForeignKey(x => x.WarehouseInventoryId);
        builder.HasOne(x => x.ReleaseAuthorization).WithMany(x => x.ReleaseRecords).HasForeignKey(x => x.ReleaseAuthorizationId);
        builder.HasOne(x => x.ReleasedBy).WithMany().HasForeignKey(x => x.ReleasedByUserId);
    }
}

public class DriverProfileConfiguration : IEntityTypeConfiguration<DriverProfile>
{
    public void Configure(EntityTypeBuilder<DriverProfile> builder)
    {
        builder.ToTable("driver_profiles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.LicenseNumber).HasMaxLength(50);
        builder.Property(x => x.VehicleType).HasMaxLength(100);
        builder.Property(x => x.VehicleRegistration).HasMaxLength(50);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.EmergencyContact).HasMaxLength(255);
        builder.Property(x => x.EmergencyPhone).HasMaxLength(30);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.ZipCode).HasMaxLength(16);
        builder.Property(x => x.StreetAddress).HasMaxLength(255);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId);
    }
}

public class OperatorInviteCodeConfiguration : IEntityTypeConfiguration<OperatorInviteCode>
{
    public void Configure(EntityTypeBuilder<OperatorInviteCode> builder)
    {
        builder.ToTable("operator_invite_codes");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(128);
        builder.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId);
    }
}

public class DriverDocumentConfiguration : IEntityTypeConfiguration<DriverDocument>
{
    public void Configure(EntityTypeBuilder<DriverDocument> builder)
    {
        builder.ToTable("driver_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(255).IsRequired();
        builder.HasOne(x => x.DriverProfile).WithMany(x => x.Documents).HasForeignKey(x => x.DriverProfileId);
    }
}

public class DriverProfileHistoryConfiguration : IEntityTypeConfiguration<DriverProfileHistory>
{
    public void Configure(EntityTypeBuilder<DriverProfileHistory> builder)
    {
        builder.ToTable("driver_profile_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChangeDescription).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.DriverProfile).WithMany(x => x.History).HasForeignKey(x => x.DriverProfileId);
        builder.HasOne(x => x.ChangedBy).WithMany().HasForeignKey(x => x.ChangedByUserId);
    }
}

public class OfflineSyncQueueConfiguration : IEntityTypeConfiguration<OfflineSyncQueue>
{
    public void Configure(EntityTypeBuilder<OfflineSyncQueue> builder)
    {
        builder.ToTable("offline_sync_queue");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uuid).IsRequired();
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.ClientId }).IsUnique();
        builder.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ClientId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("json").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public class FaceVerificationLogConfiguration : IEntityTypeConfiguration<FaceVerificationLog>
{
    public void Configure(EntityTypeBuilder<FaceVerificationLog> builder)
    {
        builder.ToTable("face_verification_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Confidence).HasPrecision(5, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}
