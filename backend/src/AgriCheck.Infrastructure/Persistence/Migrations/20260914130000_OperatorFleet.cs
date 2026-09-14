using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260914130000_OperatorFleet")]
public partial class OperatorFleet : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "operator_vehicles",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                OperatorUserId = table.Column<long>(type: "bigint", nullable: false),
                PlateNumber = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                VehicleType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                DefaultDriverUserId = table.Column<long>(type: "bigint", nullable: true),
                IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_operator_vehicles", x => x.Id);
                table.ForeignKey(
                    name: "FK_operator_vehicles_users_DefaultDriverUserId",
                    column: x => x.DefaultDriverUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_operator_vehicles_users_OperatorUserId",
                    column: x => x.OperatorUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_operator_vehicles_DefaultDriverUserId",
            table: "operator_vehicles",
            column: "DefaultDriverUserId");

        migrationBuilder.CreateIndex(
            name: "IX_operator_vehicles_OperatorUserId_PlateNumber",
            table: "operator_vehicles",
            columns: new[] { "OperatorUserId", "PlateNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_operator_vehicles_Uuid",
            table: "operator_vehicles",
            column: "Uuid",
            unique: true);

        migrationBuilder.AddColumn<long>(
            name: "AssignedOperatorVehicleId",
            table: "containers",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_containers_AssignedOperatorVehicleId",
            table: "containers",
            column: "AssignedOperatorVehicleId");

        migrationBuilder.AddForeignKey(
            name: "FK_containers_operator_vehicles_AssignedOperatorVehicleId",
            table: "containers",
            column: "AssignedOperatorVehicleId",
            principalTable: "operator_vehicles",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_containers_operator_vehicles_AssignedOperatorVehicleId",
            table: "containers");

        migrationBuilder.DropIndex(
            name: "IX_containers_AssignedOperatorVehicleId",
            table: "containers");

        migrationBuilder.DropColumn(
            name: "AssignedOperatorVehicleId",
            table: "containers");

        migrationBuilder.DropTable(name: "operator_vehicles");
    }
}
