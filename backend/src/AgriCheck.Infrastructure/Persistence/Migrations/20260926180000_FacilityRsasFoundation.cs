using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260926180000_FacilityRsasFoundation")]
public partial class FacilityRsasFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "facility_registrations",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false),
                WarehouseFacilityId = table.Column<long>(type: "bigint", nullable: false),
                SubmittedByUserId = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                RsasExternalRef = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                OwnerName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                MaxCapacityKg = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                ProductsJson = table.Column<string>(type: "longtext", nullable: true),
                FormDataJson = table.Column<string>(type: "longtext", nullable: true),
                RegisteredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                ReviewComments = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_facility_registrations", x => x.Id);
                table.ForeignKey(
                    name: "FK_facility_registrations_users_SubmittedByUserId",
                    column: x => x.SubmittedByUserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_facility_registrations_warehouse_facilities_WarehouseFacilityId",
                    column: x => x.WarehouseFacilityId,
                    principalTable: "warehouse_facilities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "facility_accreditations",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Uuid = table.Column<Guid>(type: "char(36)", nullable: false),
                FacilityRegistrationId = table.Column<long>(type: "bigint", nullable: false),
                WarehouseFacilityId = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                CertificateNumber = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                LeadAgencyId = table.Column<long>(type: "bigint", nullable: true),
                AccreditedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_facility_accreditations", x => x.Id);
                table.ForeignKey(
                    name: "FK_facility_accreditations_agencies_LeadAgencyId",
                    column: x => x.LeadAgencyId,
                    principalTable: "agencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_facility_accreditations_facility_registrations_FacilityRegistrationId",
                    column: x => x.FacilityRegistrationId,
                    principalTable: "facility_registrations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_facility_accreditations_warehouse_facilities_WarehouseFacilityId",
                    column: x => x.WarehouseFacilityId,
                    principalTable: "warehouse_facilities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_facility_registrations_Uuid",
            table: "facility_registrations",
            column: "Uuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_facility_registrations_WarehouseFacilityId",
            table: "facility_registrations",
            column: "WarehouseFacilityId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_facility_accreditations_Uuid",
            table: "facility_accreditations",
            column: "Uuid",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_facility_accreditations_WarehouseFacilityId",
            table: "facility_accreditations",
            column: "WarehouseFacilityId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "facility_accreditations");
        migrationBuilder.DropTable(name: "facility_registrations");
    }
}
