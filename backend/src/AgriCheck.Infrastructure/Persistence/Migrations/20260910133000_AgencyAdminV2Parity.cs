using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [Migration("20260910133000_AgencyAdminV2Parity")]
    public partial class AgencyAdminV2Parity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "agencies",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "agencies",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "agencies",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "agencies",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "ParentId",
                table: "agencies",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_agencies_ParentId",
                table: "agencies",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_agencies_agencies_ParentId",
                table: "agencies",
                column: "ParentId",
                principalTable: "agencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agencies_agencies_ParentId",
                table: "agencies");

            migrationBuilder.DropIndex(
                name: "IX_agencies_ParentId",
                table: "agencies");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "agencies");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "agencies");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "agencies");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "agencies");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "agencies");
        }
    }
}
