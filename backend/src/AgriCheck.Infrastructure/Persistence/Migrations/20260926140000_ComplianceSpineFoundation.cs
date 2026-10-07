using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260926140000_ComplianceSpineFoundation")]
public partial class ComplianceSpineFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CompliancePhase",
            table: "entries",
            type: "varchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Draft");

        migrationBuilder.AddColumn<long>(
            name: "FinalWarehouseFacilityId",
            table: "entries",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_entries_FinalWarehouseFacilityId",
            table: "entries",
            column: "FinalWarehouseFacilityId");

        migrationBuilder.AddForeignKey(
            name: "FK_entries_warehouse_facilities_FinalWarehouseFacilityId",
            table: "entries",
            column: "FinalWarehouseFacilityId",
            principalTable: "warehouse_facilities",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.CreateTable(
            name: "compliance_sync_outbox",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                EntityType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                EntityId = table.Column<long>(type: "bigint", nullable: false),
                TargetSystem = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                Operation = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                PayloadJson = table.Column<string>(type: "longtext", nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                SyncedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_compliance_sync_outbox", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_compliance_sync_outbox_Status_CreatedAt",
            table: "compliance_sync_outbox",
            columns: new[] { "Status", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "compliance_sync_outbox");

        migrationBuilder.DropForeignKey(
            name: "FK_entries_warehouse_facilities_FinalWarehouseFacilityId",
            table: "entries");

        migrationBuilder.DropIndex(
            name: "IX_entries_FinalWarehouseFacilityId",
            table: "entries");

        migrationBuilder.DropColumn(name: "CompliancePhase", table: "entries");
        migrationBuilder.DropColumn(name: "FinalWarehouseFacilityId", table: "entries");
    }
}
