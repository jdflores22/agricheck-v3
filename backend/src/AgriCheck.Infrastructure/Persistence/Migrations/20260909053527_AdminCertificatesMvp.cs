using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminCertificatesMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AgencyId",
                table: "certificates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IssuedByUserId",
                table: "certificates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfStoredFileName",
                table: "certificates",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "certificates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedReason",
                table: "certificates",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "TemplateVersionId",
                table: "certificates",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "certificate_templates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgencyId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_certificate_templates_agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "agencies",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "form_templates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FormType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_templates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "processing_fee_configs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AgencyId = table.Column<long>(type: "bigint", nullable: false),
                    EntryType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_fee_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_processing_fee_configs_agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "certificate_process_assignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AgencyId = table.Column<long>(type: "bigint", nullable: false),
                    ProcessType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_process_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_certificate_process_assignments_agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_certificate_process_assignments_certificate_templates_Templa~",
                        column: x => x.TemplateId,
                        principalTable: "certificate_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "certificate_template_versions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LayoutJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_template_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_certificate_template_versions_certificate_templates_Template~",
                        column: x => x.TemplateId,
                        principalTable: "certificate_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "form_agency_tags",
                columns: table => new
                {
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    AgencyId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_agency_tags", x => new { x.TemplateId, x.AgencyId });
                    table.ForeignKey(
                        name: "FK_form_agency_tags_agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_form_agency_tags_form_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "form_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "form_template_versions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    SchemaJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_template_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_form_template_versions_form_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "form_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "certificate_elements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VersionId = table.Column<long>(type: "bigint", nullable: false),
                    ElementType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_elements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_certificate_elements_certificate_template_versions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "certificate_template_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_AgencyId",
                table: "certificates",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_IssuedByUserId",
                table: "certificates",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_TemplateVersionId",
                table: "certificates",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_elements_VersionId",
                table: "certificate_elements",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_process_assignments_AgencyId_ProcessType_IsActive",
                table: "certificate_process_assignments",
                columns: new[] { "AgencyId", "ProcessType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_certificate_process_assignments_TemplateId",
                table: "certificate_process_assignments",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_template_versions_TemplateId_VersionNumber",
                table: "certificate_template_versions",
                columns: new[] { "TemplateId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certificate_templates_AgencyId",
                table: "certificate_templates",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_templates_Uuid",
                table: "certificate_templates",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_agency_tags_AgencyId",
                table: "form_agency_tags",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_form_template_versions_TemplateId_VersionNumber",
                table: "form_template_versions",
                columns: new[] { "TemplateId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_templates_Uuid",
                table: "form_templates",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processing_fee_configs_AgencyId_EntryType_IsActive",
                table: "processing_fee_configs",
                columns: new[] { "AgencyId", "EntryType", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_certificates_agencies_AgencyId",
                table: "certificates",
                column: "AgencyId",
                principalTable: "agencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_certificates_certificate_template_versions_TemplateVersionId",
                table: "certificates",
                column: "TemplateVersionId",
                principalTable: "certificate_template_versions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_certificates_users_IssuedByUserId",
                table: "certificates",
                column: "IssuedByUserId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_certificates_agencies_AgencyId",
                table: "certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_certificates_certificate_template_versions_TemplateVersionId",
                table: "certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_certificates_users_IssuedByUserId",
                table: "certificates");

            migrationBuilder.DropTable(
                name: "certificate_elements");

            migrationBuilder.DropTable(
                name: "certificate_process_assignments");

            migrationBuilder.DropTable(
                name: "form_agency_tags");

            migrationBuilder.DropTable(
                name: "form_template_versions");

            migrationBuilder.DropTable(
                name: "processing_fee_configs");

            migrationBuilder.DropTable(
                name: "certificate_template_versions");

            migrationBuilder.DropTable(
                name: "form_templates");

            migrationBuilder.DropTable(
                name: "certificate_templates");

            migrationBuilder.DropIndex(
                name: "IX_certificates_AgencyId",
                table: "certificates");

            migrationBuilder.DropIndex(
                name: "IX_certificates_IssuedByUserId",
                table: "certificates");

            migrationBuilder.DropIndex(
                name: "IX_certificates_TemplateVersionId",
                table: "certificates");

            migrationBuilder.DropColumn(
                name: "AgencyId",
                table: "certificates");

            migrationBuilder.DropColumn(
                name: "IssuedByUserId",
                table: "certificates");

            migrationBuilder.DropColumn(
                name: "PdfStoredFileName",
                table: "certificates");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "certificates");

            migrationBuilder.DropColumn(
                name: "RevokedReason",
                table: "certificates");

            migrationBuilder.DropColumn(
                name: "TemplateVersionId",
                table: "certificates");
        }
    }
}
