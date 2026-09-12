using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[Migration("20260910150000_EntryContainerFormData")]
public partial class EntryContainerFormData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FormDataJson",
            table: "containers",
            type: "json",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SequenceNumber",
            table: "containers",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateIndex(
            name: "IX_containers_EntryId_SequenceNumber",
            table: "containers",
            columns: new[] { "EntryId", "SequenceNumber" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_containers_EntryId_SequenceNumber",
            table: "containers");

        migrationBuilder.DropColumn(
            name: "FormDataJson",
            table: "containers");

        migrationBuilder.DropColumn(
            name: "SequenceNumber",
            table: "containers");
    }
}
