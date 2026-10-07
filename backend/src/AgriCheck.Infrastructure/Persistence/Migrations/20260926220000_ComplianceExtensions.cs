using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260926220000_ComplianceExtensions")]
public partial class ComplianceExtensions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "SpsImportClearanceId",
            table: "containers",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "sps_document_checklist_templates",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Code = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                Title = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_sps_document_checklist_templates", x => x.Id));

        migrationBuilder.CreateTable(
            name: "sps_issuing_rules",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                HsCodePrefix = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                IssuingAgencyId = table.Column<long>(type: "bigint", nullable: false),
                IsPrimaryIssuer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                Notes = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sps_issuing_rules", x => x.Id);
                table.ForeignKey(
                    name: "FK_sps_issuing_rules_agencies_IssuingAgencyId",
                    column: x => x.IssuingAgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "sps_application_documents",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                DocumentCode = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                EntryFileId = table.Column<long>(type: "bigint", nullable: true),
                Notes = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sps_application_documents", x => x.Id);
                table.ForeignKey(
                    name: "FK_sps_application_documents_entry_files_EntryFileId",
                    column: x => x.EntryFileId,
                    principalTable: "entry_files",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_sps_application_documents_sps_import_clearance_applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "sps_import_clearance_applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "erfi_submissions",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false),
                EntryId = table.Column<long>(type: "bigint", nullable: false),
                SpsImportClearanceId = table.Column<long>(type: "bigint", nullable: false),
                FiledByUserId = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                FiledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                EtaAtPoe = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                BolReference = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                IfmDestinationJson = table.Column<string>(type: "longtext", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_erfi_submissions", x => x.Id);
                table.ForeignKey(
                    name: "FK_erfi_submissions_entries_EntryId",
                    column: x => x.EntryId,
                    principalTable: "entries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_erfi_submissions_sps_import_clearances_SpsImportClearanceId",
                    column: x => x.SpsImportClearanceId,
                    principalTable: "sps_import_clearances",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_erfi_submissions_users_FiledByUserId",
                    column: x => x.FiledByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_containers_SpsImportClearanceId",
            table: "containers",
            column: "SpsImportClearanceId");

        migrationBuilder.CreateIndex(
            name: "IX_erfi_submissions_EntryId",
            table: "erfi_submissions",
            column: "EntryId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_erfi_submissions_Uuid",
            table: "erfi_submissions",
            column: "Uuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_sps_application_documents_ApplicationId_DocumentCode",
            table: "sps_application_documents",
            columns: new[] { "ApplicationId", "DocumentCode" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_sps_document_checklist_templates_Code",
            table: "sps_document_checklist_templates",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_sps_issuing_rules_HsCodePrefix_IssuingAgencyId",
            table: "sps_issuing_rules",
            columns: new[] { "HsCodePrefix", "IssuingAgencyId" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_containers_sps_import_clearances_SpsImportClearanceId",
            table: "containers",
            column: "SpsImportClearanceId",
            principalTable: "sps_import_clearances",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_containers_sps_import_clearances_SpsImportClearanceId",
            table: "containers");

        migrationBuilder.DropTable(name: "erfi_submissions");
        migrationBuilder.DropTable(name: "sps_application_documents");
        migrationBuilder.DropTable(name: "sps_issuing_rules");
        migrationBuilder.DropTable(name: "sps_document_checklist_templates");

        migrationBuilder.DropIndex(
            name: "IX_containers_SpsImportClearanceId",
            table: "containers");

        migrationBuilder.DropColumn(
            name: "SpsImportClearanceId",
            table: "containers");
    }
}
