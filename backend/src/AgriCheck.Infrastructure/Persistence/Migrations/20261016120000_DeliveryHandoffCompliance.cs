using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20261016120000_DeliveryHandoffCompliance")]
public partial class DeliveryHandoffCompliance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "WarehouseHandoffEligibleAt",
            table: "containers",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "container_handoff_tokens",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Nonce = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                ContainerId = table.Column<long>(type: "bigint", nullable: false),
                IssuedByDriverUserId = table.Column<long>(type: "bigint", nullable: false),
                IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ConsumedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                ConsumedByUserId = table.Column<long>(type: "bigint", nullable: true),
                QrPayload = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_container_handoff_tokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_container_handoff_tokens_containers_ContainerId",
                    column: x => x.ContainerId,
                    principalTable: "containers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_container_handoff_tokens_users_ConsumedByUserId",
                    column: x => x.ConsumedByUserId,
                    principalTable: "users",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_container_handoff_tokens_users_IssuedByDriverUserId",
                    column: x => x.IssuedByDriverUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "delivery_compliance_events",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ContainerId = table.Column<long>(type: "bigint", nullable: false),
                DriverUserId = table.Column<long>(type: "bigint", nullable: true),
                EventType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                Details = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                RecordedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_delivery_compliance_events", x => x.Id);
                table.ForeignKey(
                    name: "FK_delivery_compliance_events_containers_ContainerId",
                    column: x => x.ContainerId,
                    principalTable: "containers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_delivery_compliance_events_users_DriverUserId",
                    column: x => x.DriverUserId,
                    principalTable: "users",
                    principalColumn: "Id");
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_container_handoff_tokens_ContainerId_ExpiresAt",
            table: "container_handoff_tokens",
            columns: new[] { "ContainerId", "ExpiresAt" });

        migrationBuilder.CreateIndex(
            name: "IX_container_handoff_tokens_Nonce",
            table: "container_handoff_tokens",
            column: "Nonce",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_delivery_compliance_events_ContainerId_RecordedAt",
            table: "delivery_compliance_events",
            columns: new[] { "ContainerId", "RecordedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "delivery_compliance_events");
        migrationBuilder.DropTable(name: "container_handoff_tokens");
        migrationBuilder.DropColumn(name: "WarehouseHandoffEligibleAt", table: "containers");
    }
}
