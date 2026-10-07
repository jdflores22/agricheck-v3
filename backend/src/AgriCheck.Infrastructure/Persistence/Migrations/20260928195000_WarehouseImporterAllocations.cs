using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260928195000_WarehouseImporterAllocations")]
public partial class WarehouseImporterAllocations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "warehouse_importer_allocations",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                ImporterUserId = table.Column<long>(type: "bigint", nullable: false),
                WarehouseFacilityId = table.Column<long>(type: "bigint", nullable: false),
                ContractReference = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                AllocatedCapacityKg = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                AllowedCommodityClassesJson = table.Column<string>(type: "longtext", nullable: false),
                ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ValidTo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_warehouse_importer_allocations", x => x.Id);
                table.ForeignKey(
                    name: "FK_wh_imp_alloc_importer",
                    column: x => x.ImporterUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_wh_imp_alloc_facility",
                    column: x => x.WarehouseFacilityId,
                    principalTable: "warehouse_facilities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_importer_allocations_Uuid",
            table: "warehouse_importer_allocations",
            column: "Uuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_warehouse_importer_allocations_ImporterUserId_WarehouseFacilityId_Status",
            table: "warehouse_importer_allocations",
            columns: new[] { "ImporterUserId", "WarehouseFacilityId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "warehouse_importer_allocations");
    }
}
