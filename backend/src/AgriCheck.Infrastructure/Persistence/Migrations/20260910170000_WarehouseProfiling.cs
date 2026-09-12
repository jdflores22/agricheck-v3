using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[Migration("20260910170000_WarehouseProfiling")]
public partial class WarehouseProfiling : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "BarangayId",
            table: "warehouse_facilities",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "CityId",
            table: "warehouse_facilities",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "Latitude",
            table: "warehouse_facilities",
            type: "decimal(10,7)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "Longitude",
            table: "warehouse_facilities",
            type: "decimal(10,7)",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "ProvinceId",
            table: "warehouse_facilities",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "RegionId",
            table: "warehouse_facilities",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StreetAddress",
            table: "warehouse_facilities",
            type: "varchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ZipCode",
            table: "warehouse_facilities",
            type: "varchar(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_facilities_BarangayId",
            table: "warehouse_facilities",
            column: "BarangayId");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_facilities_CityId",
            table: "warehouse_facilities",
            column: "CityId");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_facilities_ProvinceId",
            table: "warehouse_facilities",
            column: "ProvinceId");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_facilities_RegionId",
            table: "warehouse_facilities",
            column: "RegionId");

        migrationBuilder.AddForeignKey(
            name: "FK_warehouse_facilities_address_barangays_BarangayId",
            table: "warehouse_facilities",
            column: "BarangayId",
            principalTable: "address_barangays",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_warehouse_facilities_address_cities_CityId",
            table: "warehouse_facilities",
            column: "CityId",
            principalTable: "address_cities",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_warehouse_facilities_address_provinces_ProvinceId",
            table: "warehouse_facilities",
            column: "ProvinceId",
            principalTable: "address_provinces",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_warehouse_facilities_address_regions_RegionId",
            table: "warehouse_facilities",
            column: "RegionId",
            principalTable: "address_regions",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_warehouse_facilities_address_barangays_BarangayId",
            table: "warehouse_facilities");

        migrationBuilder.DropForeignKey(
            name: "FK_warehouse_facilities_address_cities_CityId",
            table: "warehouse_facilities");

        migrationBuilder.DropForeignKey(
            name: "FK_warehouse_facilities_address_provinces_ProvinceId",
            table: "warehouse_facilities");

        migrationBuilder.DropForeignKey(
            name: "FK_warehouse_facilities_address_regions_RegionId",
            table: "warehouse_facilities");

        migrationBuilder.DropIndex(
            name: "IX_warehouse_facilities_BarangayId",
            table: "warehouse_facilities");

        migrationBuilder.DropIndex(
            name: "IX_warehouse_facilities_CityId",
            table: "warehouse_facilities");

        migrationBuilder.DropIndex(
            name: "IX_warehouse_facilities_ProvinceId",
            table: "warehouse_facilities");

        migrationBuilder.DropIndex(
            name: "IX_warehouse_facilities_RegionId",
            table: "warehouse_facilities");

        migrationBuilder.DropColumn(
            name: "BarangayId",
            table: "warehouse_facilities");

        migrationBuilder.DropColumn(
            name: "CityId",
            table: "warehouse_facilities");

        migrationBuilder.DropColumn(
            name: "Latitude",
            table: "warehouse_facilities");

        migrationBuilder.DropColumn(
            name: "Longitude",
            table: "warehouse_facilities");

        migrationBuilder.DropColumn(
            name: "ProvinceId",
            table: "warehouse_facilities");

        migrationBuilder.DropColumn(
            name: "RegionId",
            table: "warehouse_facilities");

        migrationBuilder.DropColumn(
            name: "StreetAddress",
            table: "warehouse_facilities");

        migrationBuilder.DropColumn(
            name: "ZipCode",
            table: "warehouse_facilities");
    }
}
