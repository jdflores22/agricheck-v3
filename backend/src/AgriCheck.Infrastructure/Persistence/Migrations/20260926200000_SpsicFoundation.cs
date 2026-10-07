using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260926200000_SpsicFoundation")]
public partial class SpsicFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "sps_import_clearance_applications",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false),
                EntryId = table.Column<long>(type: "bigint", nullable: false),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                AgencyId = table.Column<long>(type: "bigint", nullable: false),
                AgencyLicenseId = table.Column<long>(type: "bigint", nullable: true),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                FormDataJson = table.Column<string>(type: "longtext", nullable: true),
                SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                ReviewComments = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                ReviewerUserId = table.Column<long>(type: "bigint", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sps_import_clearance_applications", x => x.Id);
                table.ForeignKey(
                    name: "FK_sps_import_clearance_applications_agencies_AgencyId",
                    column: x => x.AgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_sps_import_clearance_applications_agency_licenses_AgencyLicenseId",
                    column: x => x.AgencyLicenseId,
                    principalTable: "agency_licenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_sps_import_clearance_applications_entries_EntryId",
                    column: x => x.EntryId,
                    principalTable: "entries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_sps_import_clearance_applications_users_ReviewerUserId",
                    column: x => x.ReviewerUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_sps_import_clearance_applications_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "sps_import_clearances",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false),
                EntryId = table.Column<long>(type: "bigint", nullable: false),
                ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                IssuingAgencyId = table.Column<long>(type: "bigint", nullable: false),
                ClearanceNumber = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                MustShipOutBy = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                MustArriveBy = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                PortOfEntry = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                FinalWarehouseFacilityId = table.Column<long>(type: "bigint", nullable: true),
                CommodityDescription = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                DaTradeReference = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sps_import_clearances", x => x.Id);
                table.ForeignKey(
                    name: "FK_sps_import_clearances_agencies_IssuingAgencyId",
                    column: x => x.IssuingAgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_sps_import_clearances_entries_EntryId",
                    column: x => x.EntryId,
                    principalTable: "entries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_sps_import_clearances_sps_import_clearance_applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "sps_import_clearance_applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_sps_import_clearances_warehouse_facilities_FinalWarehouseFacilityId",
                    column: x => x.FinalWarehouseFacilityId,
                    principalTable: "warehouse_facilities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_sps_import_clearance_applications_EntryId",
            table: "sps_import_clearance_applications",
            column: "EntryId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_sps_import_clearance_applications_Uuid",
            table: "sps_import_clearance_applications",
            column: "Uuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_sps_import_clearances_ApplicationId",
            table: "sps_import_clearances",
            column: "ApplicationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_sps_import_clearances_ClearanceNumber",
            table: "sps_import_clearances",
            column: "ClearanceNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_sps_import_clearances_Uuid",
            table: "sps_import_clearances",
            column: "Uuid",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "sps_import_clearances");
        migrationBuilder.DropTable(name: "sps_import_clearance_applications");
    }
}
