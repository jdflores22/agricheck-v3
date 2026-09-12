using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[Migration("20260909223500_AccreditationOfficerAssignment")]
public partial class AccreditationOfficerAssignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "AssignedOfficerUserId",
            table: "accreditation_submissions",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ClaimedAt",
            table: "accreditation_submissions",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_accreditation_submissions_AssignedOfficerUserId",
            table: "accreditation_submissions",
            column: "AssignedOfficerUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_accreditation_submissions_users_AssignedOfficerUserId",
            table: "accreditation_submissions",
            column: "AssignedOfficerUserId",
            principalTable: "users",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_accreditation_submissions_users_AssignedOfficerUserId",
            table: "accreditation_submissions");

        migrationBuilder.DropIndex(
            name: "IX_accreditation_submissions_AssignedOfficerUserId",
            table: "accreditation_submissions");

        migrationBuilder.DropColumn(
            name: "AssignedOfficerUserId",
            table: "accreditation_submissions");

        migrationBuilder.DropColumn(
            name: "ClaimedAt",
            table: "accreditation_submissions");
    }
}
