using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260913040000_InspectorAssignments")]
public partial class InspectorAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "inspector_assignments",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                ContainerId = table.Column<long>(type: "bigint", nullable: false),
                AgencyId = table.Column<long>(type: "bigint", nullable: false),
                InspectorUserId = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inspector_assignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_inspector_assignments_agencies_AgencyId",
                    column: x => x.AgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_inspector_assignments_containers_ContainerId",
                    column: x => x.ContainerId,
                    principalTable: "containers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_inspector_assignments_users_InspectorUserId",
                    column: x => x.InspectorUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_inspector_assignments_AgencyId",
            table: "inspector_assignments",
            column: "AgencyId");

        migrationBuilder.CreateIndex(
            name: "IX_inspector_assignments_ContainerId_InspectorUserId_Status",
            table: "inspector_assignments",
            columns: new[] { "ContainerId", "InspectorUserId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_inspector_assignments_InspectorUserId",
            table: "inspector_assignments",
            column: "InspectorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_inspector_assignments_Uuid",
            table: "inspector_assignments",
            column: "Uuid",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "inspector_assignments");
    }
}
