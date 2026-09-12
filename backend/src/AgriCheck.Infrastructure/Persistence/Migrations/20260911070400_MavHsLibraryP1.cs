using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MavHsLibraryP1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MavHsDetailId",
                table: "mav_applications",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mav_hs_categories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    HsCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgencyId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_hs_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_hs_categories_agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "agencies",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_hs_headings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    HeadingNumber = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_hs_headings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_hs_headings_mav_hs_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "mav_hs_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_hs_details",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    HeadingId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_hs_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_hs_details_mav_hs_headings_HeadingId",
                        column: x => x.HeadingId,
                        principalTable: "mav_hs_headings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_mav_applications_MavHsDetailId",
                table: "mav_applications",
                column: "MavHsDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_hs_categories_AgencyId",
                table: "mav_hs_categories",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_hs_categories_HsCode",
                table: "mav_hs_categories",
                column: "HsCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_hs_categories_Uuid",
                table: "mav_hs_categories",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_hs_details_HeadingId",
                table: "mav_hs_details",
                column: "HeadingId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_hs_details_Uuid",
                table: "mav_hs_details",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_hs_headings_CategoryId_HeadingNumber",
                table: "mav_hs_headings",
                columns: new[] { "CategoryId", "HeadingNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_hs_headings_Uuid",
                table: "mav_hs_headings",
                column: "Uuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_mav_applications_mav_hs_details_MavHsDetailId",
                table: "mav_applications",
                column: "MavHsDetailId",
                principalTable: "mav_hs_details",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_mav_applications_mav_hs_details_MavHsDetailId",
                table: "mav_applications");

            migrationBuilder.DropTable(
                name: "mav_hs_details");

            migrationBuilder.DropTable(
                name: "mav_hs_headings");

            migrationBuilder.DropTable(
                name: "mav_hs_categories");

            migrationBuilder.DropIndex(
                name: "IX_mav_applications_MavHsDetailId",
                table: "mav_applications");

            migrationBuilder.DropColumn(
                name: "MavHsDetailId",
                table: "mav_applications");
        }
    }
}
