using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MavModuleMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mav_application_periods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MavYear = table.Column<int>(type: "int", nullable: false),
                    PoolType = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpeningDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ClosingDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgencyId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_application_periods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_application_periods_agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "agencies",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_audit_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    EntityType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_audit_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_notification_preferences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ApplicationSubmitted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ApplicationApproved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ApplicationRejected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LicenseIssued = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LicenseExpiring = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MicIssued = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MicExpiring = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_notification_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_notification_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_applications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReferenceNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApplicationPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    ImporterId = table.Column<long>(type: "bigint", nullable: false),
                    AccreditationSubmissionId = table.Column<long>(type: "bigint", nullable: true),
                    HsCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommodityName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestedVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    AllocatedVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RejectionReason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentsJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_applications_mav_application_periods_ApplicationPeriodId",
                        column: x => x.ApplicationPeriodId,
                        principalTable: "mav_application_periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mav_applications_users_ImporterId",
                        column: x => x.ImporterId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mav_applications_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_commodity_allocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    CommodityId = table.Column<long>(type: "bigint", nullable: false),
                    HsCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommodityName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    AllocatedVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    MinimumImportVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    AgencyId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_commodity_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_commodity_allocations_agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "agencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_mav_commodity_allocations_commodities_CommodityId",
                        column: x => x.CommodityId,
                        principalTable: "commodities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mav_commodity_allocations_mav_application_periods_Applicatio~",
                        column: x => x.ApplicationPeriodId,
                        principalTable: "mav_application_periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_licenses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LicenseNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    ImporterId = table.Column<long>(type: "bigint", nullable: false),
                    MavYear = table.Column<int>(type: "int", nullable: false),
                    HsCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommodityName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AwardedVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    PoolType = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevokedReason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_licenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_licenses_mav_applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "mav_applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mav_licenses_users_ImporterId",
                        column: x => x.ImporterId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_accounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LicenseId = table.Column<long>(type: "bigint", nullable: false),
                    AwardedVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    UtilizedVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    LastTransactionAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_accounts_mav_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "mav_licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_account_transactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Volume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_account_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_account_transactions_mav_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "mav_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mav_account_transactions_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mav_import_certificates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CertificateNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LicenseId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    ImporterId = table.Column<long>(type: "bigint", nullable: false),
                    HsCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommodityName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuthorizedVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    UtilizedVolume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mav_import_certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mav_import_certificates_mav_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "mav_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mav_import_certificates_mav_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "mav_licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mav_import_certificates_users_ImporterId",
                        column: x => x.ImporterId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mic_utilizations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MicId = table.Column<long>(type: "bigint", nullable: false),
                    EntryId = table.Column<long>(type: "bigint", nullable: false),
                    Volume = table.Column<decimal>(type: "decimal(15,3)", precision: 15, scale: 3, nullable: false),
                    UtilizedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mic_utilizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mic_utilizations_entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mic_utilizations_mav_import_certificates_MicId",
                        column: x => x.MicId,
                        principalTable: "mav_import_certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_mav_account_transactions_AccountId",
                table: "mav_account_transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_account_transactions_CreatedByUserId",
                table: "mav_account_transactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_accounts_LicenseId",
                table: "mav_accounts",
                column: "LicenseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_application_periods_AgencyId",
                table: "mav_application_periods",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_application_periods_MavYear",
                table: "mav_application_periods",
                column: "MavYear");

            migrationBuilder.CreateIndex(
                name: "IX_mav_application_periods_Status",
                table: "mav_application_periods",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_mav_application_periods_Uuid",
                table: "mav_application_periods",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_applications_ApplicationPeriodId",
                table: "mav_applications",
                column: "ApplicationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_applications_ImporterId",
                table: "mav_applications",
                column: "ImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_applications_ReferenceNumber",
                table: "mav_applications",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_applications_ReviewedByUserId",
                table: "mav_applications",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_applications_Uuid",
                table: "mav_applications",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_audit_logs_UserId",
                table: "mav_audit_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_commodity_allocations_AgencyId",
                table: "mav_commodity_allocations",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_commodity_allocations_ApplicationPeriodId",
                table: "mav_commodity_allocations",
                column: "ApplicationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_commodity_allocations_CommodityId",
                table: "mav_commodity_allocations",
                column: "CommodityId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_import_certificates_AccountId",
                table: "mav_import_certificates",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_import_certificates_CertificateNumber",
                table: "mav_import_certificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_import_certificates_ImporterId",
                table: "mav_import_certificates",
                column: "ImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_import_certificates_LicenseId",
                table: "mav_import_certificates",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_import_certificates_Uuid",
                table: "mav_import_certificates",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_licenses_ApplicationId",
                table: "mav_licenses",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_licenses_ImporterId",
                table: "mav_licenses",
                column: "ImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_mav_licenses_LicenseNumber",
                table: "mav_licenses",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_licenses_Uuid",
                table: "mav_licenses",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mav_notification_preferences_UserId",
                table: "mav_notification_preferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mic_utilizations_EntryId",
                table: "mic_utilizations",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_mic_utilizations_MicId",
                table: "mic_utilizations",
                column: "MicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mav_account_transactions");

            migrationBuilder.DropTable(
                name: "mav_audit_logs");

            migrationBuilder.DropTable(
                name: "mav_commodity_allocations");

            migrationBuilder.DropTable(
                name: "mav_notification_preferences");

            migrationBuilder.DropTable(
                name: "mic_utilizations");

            migrationBuilder.DropTable(
                name: "mav_import_certificates");

            migrationBuilder.DropTable(
                name: "mav_accounts");

            migrationBuilder.DropTable(
                name: "mav_licenses");

            migrationBuilder.DropTable(
                name: "mav_applications");

            migrationBuilder.DropTable(
                name: "mav_application_periods");
        }
    }
}
