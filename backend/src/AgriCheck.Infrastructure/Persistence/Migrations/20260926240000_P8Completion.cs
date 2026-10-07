using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260926240000_P8Completion")]
public partial class P8Completion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Kind",
            table: "inspections",
            type: "varchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Standard");

        migrationBuilder.AddColumn<long>(
            name: "SpsImportClearanceId",
            table: "inspections",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "ErfiSubmissionId",
            table: "inspections",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SpsUsedStampAt",
            table: "inspections",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "WarehouseFacilityId",
            table: "containers",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsBmbeExempt",
            table: "facility_registrations",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "sps_prerequisite_clearances",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                EntryId = table.Column<long>(type: "bigint", nullable: false),
                SpsApplicationId = table.Column<long>(type: "bigint", nullable: false),
                IssuingAgencyId = table.Column<long>(type: "bigint", nullable: false),
                PrerequisiteCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                ReferenceNumber = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                ClearedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sps_prerequisite_clearances", x => x.Id);
                table.ForeignKey(
                    name: "FK_sps_prerequisite_clearances_agencies_IssuingAgencyId",
                    column: x => x.IssuingAgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_sps_prerequisite_clearances_entries_EntryId",
                    column: x => x.EntryId,
                    principalTable: "entries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_sps_prerequisite_clearances_sps_import_clearance_applications_SpsApplicationId",
                    column: x => x.SpsApplicationId,
                    principalTable: "sps_import_clearance_applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "sps_must_ship_out_rules",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                HsCodePrefix = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                CommodityKeyword = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                ValidityDays = table.Column<int>(type: "int", nullable: false),
                Notes = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_sps_must_ship_out_rules", x => x.Id));

        migrationBuilder.CreateTable(
            name: "storage_tally_sheets",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false),
                WarehouseFacilityId = table.Column<long>(type: "bigint", nullable: false),
                ContainerId = table.Column<long>(type: "bigint", nullable: false),
                EntryId = table.Column<long>(type: "bigint", nullable: false),
                Direction = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                VolumeKg = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                CommodityDescription = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                RecordedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                RecordedByUserId = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_storage_tally_sheets", x => x.Id);
                table.ForeignKey(
                    name: "FK_storage_tally_sheets_containers_ContainerId",
                    column: x => x.ContainerId,
                    principalTable: "containers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_storage_tally_sheets_entries_EntryId",
                    column: x => x.EntryId,
                    principalTable: "entries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_storage_tally_sheets_users_RecordedByUserId",
                    column: x => x.RecordedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_storage_tally_sheets_warehouse_facilities_WarehouseFacilityId",
                    column: x => x.WarehouseFacilityId,
                    principalTable: "warehouse_facilities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_containers_WarehouseFacilityId",
            table: "containers",
            column: "WarehouseFacilityId");

        migrationBuilder.CreateIndex(
            name: "IX_inspections_ErfiSubmissionId",
            table: "inspections",
            column: "ErfiSubmissionId");

        migrationBuilder.CreateIndex(
            name: "IX_inspections_SpsImportClearanceId",
            table: "inspections",
            column: "SpsImportClearanceId");

        migrationBuilder.CreateIndex(
            name: "IX_sps_must_ship_out_rules_HsCodePrefix_CommodityKeyword",
            table: "sps_must_ship_out_rules",
            columns: new[] { "HsCodePrefix", "CommodityKeyword" });

        migrationBuilder.CreateIndex(
            name: "IX_sps_prerequisite_clearances_SpsApplicationId_PrerequisiteCode",
            table: "sps_prerequisite_clearances",
            columns: new[] { "SpsApplicationId", "PrerequisiteCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_storage_tally_sheets_Uuid",
            table: "storage_tally_sheets",
            column: "Uuid",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_containers_warehouse_facilities_WarehouseFacilityId",
            table: "containers",
            column: "WarehouseFacilityId",
            principalTable: "warehouse_facilities",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_inspections_erfi_submissions_ErfiSubmissionId",
            table: "inspections",
            column: "ErfiSubmissionId",
            principalTable: "erfi_submissions",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_inspections_sps_import_clearances_SpsImportClearanceId",
            table: "inspections",
            column: "SpsImportClearanceId",
            principalTable: "sps_import_clearances",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_containers_warehouse_facilities_WarehouseFacilityId", table: "containers");
        migrationBuilder.DropForeignKey(name: "FK_inspections_erfi_submissions_ErfiSubmissionId", table: "inspections");
        migrationBuilder.DropForeignKey(name: "FK_inspections_sps_import_clearances_SpsImportClearanceId", table: "inspections");

        migrationBuilder.DropTable(name: "storage_tally_sheets");
        migrationBuilder.DropTable(name: "sps_must_ship_out_rules");
        migrationBuilder.DropTable(name: "sps_prerequisite_clearances");

        migrationBuilder.DropIndex(name: "IX_containers_WarehouseFacilityId", table: "containers");
        migrationBuilder.DropIndex(name: "IX_inspections_ErfiSubmissionId", table: "inspections");
        migrationBuilder.DropIndex(name: "IX_inspections_SpsImportClearanceId", table: "inspections");

        migrationBuilder.DropColumn(name: "WarehouseFacilityId", table: "containers");
        migrationBuilder.DropColumn(name: "IsBmbeExempt", table: "facility_registrations");
        migrationBuilder.DropColumn(name: "Kind", table: "inspections");
        migrationBuilder.DropColumn(name: "SpsImportClearanceId", table: "inspections");
        migrationBuilder.DropColumn(name: "ErfiSubmissionId", table: "inspections");
        migrationBuilder.DropColumn(name: "SpsUsedStampAt", table: "inspections");
    }
}
