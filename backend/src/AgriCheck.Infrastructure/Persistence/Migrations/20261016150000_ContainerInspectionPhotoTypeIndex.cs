using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AgriCheck.Infrastructure.Persistence;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20261016150000_ContainerInspectionPhotoTypeIndex")]
public partial class ContainerInspectionPhotoTypeIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_container_inspection_photos_ContainerId_PhotoType",
            table: "container_inspection_photos");

        migrationBuilder.CreateIndex(
            name: "IX_container_inspection_photos_ContainerId_PhotoType",
            table: "container_inspection_photos",
            columns: new[] { "ContainerId", "PhotoType" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_container_inspection_photos_ContainerId_PhotoType",
            table: "container_inspection_photos");

        migrationBuilder.CreateIndex(
            name: "IX_container_inspection_photos_ContainerId_PhotoType",
            table: "container_inspection_photos",
            columns: new[] { "ContainerId", "PhotoType" },
            unique: true);
    }
}
