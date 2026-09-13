using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260913120000_DriverRegistrationAndInvites")]
public partial class DriverRegistrationAndInvites : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "operator_invite_codes",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false),
                Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                OperatorUserId = table.Column<long>(type: "bigint", nullable: false),
                Label = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                MaxUses = table.Column<int>(type: "int", nullable: false),
                UsedCount = table.Column<int>(type: "int", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_operator_invite_codes", x => x.Id);
                table.ForeignKey(
                    name: "FK_operator_invite_codes_users_OperatorUserId",
                    column: x => x.OperatorUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_operator_invite_codes_Code",
            table: "operator_invite_codes",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_operator_invite_codes_OperatorUserId",
            table: "operator_invite_codes",
            column: "OperatorUserId");

        migrationBuilder.AddColumn<DateTime>(
            name: "BirthDate",
            table: "driver_profiles",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "RegionId",
            table: "driver_profiles",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "ProvinceId",
            table: "driver_profiles",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "CityId",
            table: "driver_profiles",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "BarangayId",
            table: "driver_profiles",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ZipCode",
            table: "driver_profiles",
            type: "varchar(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StreetAddress",
            table: "driver_profiles",
            type: "varchar(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "OperatorUserId",
            table: "driver_profiles",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_driver_profiles_OperatorUserId",
            table: "driver_profiles",
            column: "OperatorUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_driver_profiles_users_OperatorUserId",
            table: "driver_profiles",
            column: "OperatorUserId",
            principalTable: "users",
            principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_driver_profiles_users_OperatorUserId",
            table: "driver_profiles");

        migrationBuilder.DropIndex(
            name: "IX_driver_profiles_OperatorUserId",
            table: "driver_profiles");

        migrationBuilder.DropColumn(name: "BirthDate", table: "driver_profiles");
        migrationBuilder.DropColumn(name: "RegionId", table: "driver_profiles");
        migrationBuilder.DropColumn(name: "ProvinceId", table: "driver_profiles");
        migrationBuilder.DropColumn(name: "CityId", table: "driver_profiles");
        migrationBuilder.DropColumn(name: "BarangayId", table: "driver_profiles");
        migrationBuilder.DropColumn(name: "ZipCode", table: "driver_profiles");
        migrationBuilder.DropColumn(name: "StreetAddress", table: "driver_profiles");
        migrationBuilder.DropColumn(name: "OperatorUserId", table: "driver_profiles");

        migrationBuilder.DropTable(name: "operator_invite_codes");
    }
}
