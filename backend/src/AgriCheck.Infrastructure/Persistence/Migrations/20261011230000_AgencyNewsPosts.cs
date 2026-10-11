using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20261011230000_AgencyNewsPosts")]
public partial class AgencyNewsPosts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "agency_news_posts",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                AgencyId = table.Column<long>(type: "bigint", nullable: false),
                Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Summary = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Body = table.Column<string>(type: "text", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ImageUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                IsRegulatory = table.Column<bool>(type: "tinyint(1)", nullable: false),
                IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agency_news_posts", x => x.Id);
                table.ForeignKey(
                    name: "FK_agency_news_posts_agencies_AgencyId",
                    column: x => x.AgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_agency_news_posts_AgencyId",
            table: "agency_news_posts",
            column: "AgencyId");

        migrationBuilder.CreateIndex(
            name: "IX_agency_news_posts_DisplayOrder",
            table: "agency_news_posts",
            column: "DisplayOrder");

        migrationBuilder.CreateIndex(
            name: "IX_agency_news_posts_IsPublished_PublishedAt",
            table: "agency_news_posts",
            columns: new[] { "IsPublished", "PublishedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_agency_news_posts_Uuid",
            table: "agency_news_posts",
            column: "Uuid",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "agency_news_posts");
    }
}
