using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260926160000_AgencyLicenseFoundation")]
public partial class AgencyLicenseFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "agency_license_types",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                AgencyId = table.Column<long>(type: "bigint", nullable: false),
                Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                Kind = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                IsRequiredForSpsic = table.Column<bool>(type: "tinyint(1)", nullable: false),
                Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agency_license_types", x => x.Id);
                table.ForeignKey(
                    name: "FK_agency_license_types_agencies_AgencyId",
                    column: x => x.AgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "agency_license_applications",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                AgencyId = table.Column<long>(type: "bigint", nullable: false),
                AgencyLicenseTypeId = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                FormDataJson = table.Column<string>(type: "longtext", nullable: true),
                SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                ReviewComments = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agency_license_applications", x => x.Id);
                table.ForeignKey(
                    name: "FK_agency_license_applications_agencies_AgencyId",
                    column: x => x.AgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_agency_license_applications_agency_license_types_AgencyLicenseTypeId",
                    column: x => x.AgencyLicenseTypeId,
                    principalTable: "agency_license_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_agency_license_applications_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "agency_licenses",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                AgencyId = table.Column<long>(type: "bigint", nullable: false),
                AgencyLicenseTypeId = table.Column<long>(type: "bigint", nullable: false),
                ApplicationId = table.Column<long>(type: "bigint", nullable: true),
                LicenseNumber = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agency_licenses", x => x.Id);
                table.ForeignKey(
                    name: "FK_agency_licenses_agencies_AgencyId",
                    column: x => x.AgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_agency_licenses_agency_license_applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "agency_license_applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_agency_licenses_agency_license_types_AgencyLicenseTypeId",
                    column: x => x.AgencyLicenseTypeId,
                    principalTable: "agency_license_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_agency_licenses_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "commodity_license_rules",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                AgencyId = table.Column<long>(type: "bigint", nullable: false),
                AgencyLicenseTypeId = table.Column<long>(type: "bigint", nullable: false),
                CommodityId = table.Column<long>(type: "bigint", nullable: true),
                HsCodePrefix = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true),
                IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_commodity_license_rules", x => x.Id);
                table.ForeignKey(
                    name: "FK_commodity_license_rules_agencies_AgencyId",
                    column: x => x.AgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_commodity_license_rules_agency_license_types_AgencyLicenseTypeId",
                    column: x => x.AgencyLicenseTypeId,
                    principalTable: "agency_license_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_commodity_license_rules_commodities_CommodityId",
                    column: x => x.CommodityId,
                    principalTable: "commodities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_agency_license_types_AgencyId_Code",
            table: "agency_license_types",
            columns: new[] { "AgencyId", "Code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_agency_license_applications_Uuid",
            table: "agency_license_applications",
            column: "Uuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_agency_licenses_Uuid",
            table: "agency_licenses",
            column: "Uuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_agency_licenses_UserId_AgencyId_Status",
            table: "agency_licenses",
            columns: new[] { "UserId", "AgencyId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "commodity_license_rules");
        migrationBuilder.DropTable(name: "agency_licenses");
        migrationBuilder.DropTable(name: "agency_license_applications");
        migrationBuilder.DropTable(name: "agency_license_types");
    }
}
