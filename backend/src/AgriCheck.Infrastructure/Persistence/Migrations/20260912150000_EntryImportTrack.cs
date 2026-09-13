using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260912150000_EntryImportTrack")]
public partial class EntryImportTrack : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ImportTrack",
            table: "entries",
            type: "varchar(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Regular");

        migrationBuilder.Sql("""
            UPDATE entries
            SET ImportTrack = 'Mav'
            WHERE EntryType = 'Import'
              AND (
                    MavNo IS NOT NULL
                 OR MavDocumentStatus <> 'NotProvided'
                 OR PrimaryMicId IS NOT NULL
                 OR Id IN (SELECT DISTINCT EntryId FROM mic_utilizations)
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ImportTrack",
            table: "entries");
    }
}
