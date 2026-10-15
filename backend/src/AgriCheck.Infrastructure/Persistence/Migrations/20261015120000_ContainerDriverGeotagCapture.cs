using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20261015120000_ContainerDriverGeotagCapture")]
public partial class ContainerDriverGeotagCapture : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "CaptureLatitude",
            table: "container_inspection_photos",
            type: "decimal(65,30)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "CaptureLongitude",
            table: "container_inspection_photos",
            type: "decimal(65,30)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CapturedAtUtc",
            table: "container_inspection_photos",
            type: "datetime(6)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CaptureLatitude",
            table: "container_inspection_photos");

        migrationBuilder.DropColumn(
            name: "CaptureLongitude",
            table: "container_inspection_photos");

        migrationBuilder.DropColumn(
            name: "CapturedAtUtc",
            table: "container_inspection_photos");
    }
}
