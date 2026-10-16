using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20261016133000_DeliveryComplianceHold")]
public partial class DeliveryComplianceHold : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DeliveryComplianceClearanceNotes",
            table: "containers",
            type: "varchar(2000)",
            maxLength: 2000,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<DateTime>(
            name: "DeliveryComplianceClearedAt",
            table: "containers",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "DeliveryComplianceClearedByUserId",
            table: "containers",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DeliveryComplianceHoldAt",
            table: "containers",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeliveryComplianceHoldReason",
            table: "containers",
            type: "varchar(64)",
            maxLength: 64,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DeliveryComplianceClearanceNotes", table: "containers");
        migrationBuilder.DropColumn(name: "DeliveryComplianceClearedAt", table: "containers");
        migrationBuilder.DropColumn(name: "DeliveryComplianceClearedByUserId", table: "containers");
        migrationBuilder.DropColumn(name: "DeliveryComplianceHoldAt", table: "containers");
        migrationBuilder.DropColumn(name: "DeliveryComplianceHoldReason", table: "containers");
    }
}
