using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260928120000_WarehouseLtoImporterFacilities")]
public partial class WarehouseLtoImporterFacilities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "warehouse_operating_licenses",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                WarehouseFacilityId = table.Column<long>(type: "bigint", nullable: false),
                LicenseNumber = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                IssuedByUserId = table.Column<long>(type: "bigint", nullable: false),
                Notes = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_warehouse_operating_licenses", x => x.Id);
                table.ForeignKey(
                    name: "FK_warehouse_operating_licenses_users_IssuedByUserId",
                    column: x => x.IssuedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_warehouse_operating_licenses_warehouse_facilities_WarehouseFacilityId",
                    column: x => x.WarehouseFacilityId,
                    principalTable: "warehouse_facilities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_operating_licenses_IssuedByUserId",
            table: "warehouse_operating_licenses",
            column: "IssuedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_operating_licenses_LicenseNumber",
            table: "warehouse_operating_licenses",
            column: "LicenseNumber");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_operating_licenses_WarehouseFacilityId",
            table: "warehouse_operating_licenses",
            column: "WarehouseFacilityId");

        migrationBuilder.CreateTable(
            name: "importer_facility_affiliations",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                WarehouseFacilityId = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_importer_facility_affiliations", x => x.Id);
                table.ForeignKey(
                    name: "FK_importer_facility_affiliations_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_importer_facility_affiliations_warehouse_facilities_WarehouseFacilityId",
                    column: x => x.WarehouseFacilityId,
                    principalTable: "warehouse_facilities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_importer_facility_affiliations_UserId_WarehouseFacilityId",
            table: "importer_facility_affiliations",
            columns: new[] { "UserId", "WarehouseFacilityId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_importer_facility_affiliations_Uuid",
            table: "importer_facility_affiliations",
            column: "Uuid",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "importer_facility_affiliations");
        migrationBuilder.DropTable(name: "warehouse_operating_licenses");
    }
}
