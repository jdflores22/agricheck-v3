using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260928190000_WarehouseStaffLtoApplications")]
public partial class WarehouseStaffLtoApplications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "warehouse_staff_assignments",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                WarehouseFacilityId = table.Column<long>(type: "bigint", nullable: false),
                Role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_warehouse_staff_assignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_wh_staff_assign_user",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_wh_staff_assign_facility",
                    column: x => x.WarehouseFacilityId,
                    principalTable: "warehouse_facilities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_staff_assignments_UserId_WarehouseFacilityId",
            table: "warehouse_staff_assignments",
            columns: new[] { "UserId", "WarehouseFacilityId" },
            unique: true);

        migrationBuilder.CreateTable(
            name: "warehouse_operating_license_applications",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                WarehouseFacilityId = table.Column<long>(type: "bigint", nullable: false),
                SubmittedByUserId = table.Column<long>(type: "bigint", nullable: false),
                OperatorLegalName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                ContactEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                ContactPhone = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                ReviewComments = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                IssuedLicenseId = table.Column<long>(type: "bigint", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_warehouse_operating_license_applications", x => x.Id);
                table.ForeignKey(
                    name: "FK_wh_lto_app_reviewer",
                    column: x => x.ReviewedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_wh_lto_app_submitter",
                    column: x => x.SubmittedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_wh_lto_app_facility",
                    column: x => x.WarehouseFacilityId,
                    principalTable: "warehouse_facilities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_wh_lto_app_license",
                    column: x => x.IssuedLicenseId,
                    principalTable: "warehouse_operating_licenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_operating_license_applications_Uuid",
            table: "warehouse_operating_license_applications",
            column: "Uuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_operating_license_applications_WarehouseFacilityId",
            table: "warehouse_operating_license_applications",
            column: "WarehouseFacilityId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "warehouse_operating_license_applications");
        migrationBuilder.DropTable(name: "warehouse_staff_assignments");
    }
}
