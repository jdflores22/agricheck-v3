using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EntryMavIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MavDocumentStatus",
                table: "entries",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MavNo",
                table: "entries",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MavRemarks",
                table: "entries",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "PrimaryMicId",
                table: "entries",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_entries_MavNo",
                table: "entries",
                column: "MavNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entries_PrimaryMicId",
                table: "entries",
                column: "PrimaryMicId");

            migrationBuilder.AddForeignKey(
                name: "FK_entries_mav_import_certificates_PrimaryMicId",
                table: "entries",
                column: "PrimaryMicId",
                principalTable: "mav_import_certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_entries_mav_import_certificates_PrimaryMicId",
                table: "entries");

            migrationBuilder.DropIndex(
                name: "IX_entries_MavNo",
                table: "entries");

            migrationBuilder.DropIndex(
                name: "IX_entries_PrimaryMicId",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "MavDocumentStatus",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "MavNo",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "MavRemarks",
                table: "entries");

            migrationBuilder.DropColumn(
                name: "PrimaryMicId",
                table: "entries");
        }
    }
}
