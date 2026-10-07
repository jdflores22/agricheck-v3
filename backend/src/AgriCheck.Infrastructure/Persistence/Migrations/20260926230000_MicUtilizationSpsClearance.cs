using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260926230000_MicUtilizationSpsClearance")]
public partial class MicUtilizationSpsClearance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "SpsImportClearanceId",
            table: "mic_utilizations",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_mic_utilizations_SpsImportClearanceId",
            table: "mic_utilizations",
            column: "SpsImportClearanceId");

        migrationBuilder.AddForeignKey(
            name: "FK_mic_utilizations_sps_import_clearances_SpsImportClearanceId",
            table: "mic_utilizations",
            column: "SpsImportClearanceId",
            principalTable: "sps_import_clearances",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_mic_utilizations_sps_import_clearances_SpsImportClearanceId",
            table: "mic_utilizations");

        migrationBuilder.DropIndex(
            name: "IX_mic_utilizations_SpsImportClearanceId",
            table: "mic_utilizations");

        migrationBuilder.DropColumn(
            name: "SpsImportClearanceId",
            table: "mic_utilizations");
    }
}
