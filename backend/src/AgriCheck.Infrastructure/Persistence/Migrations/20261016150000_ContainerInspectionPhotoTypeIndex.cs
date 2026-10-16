using AgriCheck.Infrastructure.Persistence;
using AgriCheck.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20261016150000_ContainerInspectionPhotoTypeIndex")]
public partial class ContainerInspectionPhotoTypeIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // MySQL: FK on ContainerId uses the composite index unless a dedicated ContainerId index exists.
        var containerIdIndex = ContainerInspectionPhotoIndexHelper.ContainerIdIndexName;
        var compositeIndex = ContainerInspectionPhotoIndexHelper.ContainerPhotoTypeIndexName;

        migrationBuilder.Sql(
            $"""
             SET @container_id_idx_exists := (
                 SELECT COUNT(*)
                 FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'container_inspection_photos'
                   AND index_name = '{containerIdIndex}'
             );
             SET @create_container_id_idx := IF(
                 @container_id_idx_exists = 0,
                 'CREATE INDEX {containerIdIndex} ON container_inspection_photos (ContainerId)',
                 'SELECT 1'
             );
             PREPARE create_container_id_idx_stmt FROM @create_container_id_idx;
             EXECUTE create_container_id_idx_stmt;
             DEALLOCATE PREPARE create_container_id_idx_stmt;

             SET @composite_is_unique := (
                 SELECT COALESCE(MAX(CASE WHEN NON_UNIQUE = 0 THEN 1 ELSE 0 END), 0)
                 FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'container_inspection_photos'
                   AND index_name = '{compositeIndex}'
             );
             SET @drop_composite := IF(
                 @composite_is_unique = 1,
                 'ALTER TABLE container_inspection_photos DROP INDEX {compositeIndex}',
                 'SELECT 1'
             );
             PREPARE drop_composite_stmt FROM @drop_composite;
             EXECUTE drop_composite_stmt;
             DEALLOCATE PREPARE drop_composite_stmt;

             SET @composite_exists := (
                 SELECT COUNT(*)
                 FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'container_inspection_photos'
                   AND index_name = '{compositeIndex}'
             );
             SET @create_composite := IF(
                 @composite_exists = 0,
                 'CREATE INDEX {compositeIndex} ON container_inspection_photos (ContainerId, PhotoType)',
                 'SELECT 1'
             );
             PREPARE create_composite_stmt FROM @create_composite;
             EXECUTE create_composite_stmt;
             DEALLOCATE PREPARE create_composite_stmt;
             """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: ContainerInspectionPhotoIndexHelper.ContainerPhotoTypeIndexName,
            table: "container_inspection_photos");

        migrationBuilder.CreateIndex(
            name: ContainerInspectionPhotoIndexHelper.ContainerPhotoTypeIndexName,
            table: "container_inspection_photos",
            columns: new[] { "ContainerId", "PhotoType" },
            unique: true);

        migrationBuilder.DropIndex(
            name: ContainerInspectionPhotoIndexHelper.ContainerIdIndexName,
            table: "container_inspection_photos");
    }
}
