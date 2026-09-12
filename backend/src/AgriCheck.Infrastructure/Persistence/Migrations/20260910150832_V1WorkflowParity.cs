using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V1WorkflowParity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WarehouseBookingId",
                table: "client_bills",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentNotes",
                table: "billings",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentProofContentType",
                table: "billings",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentProofOriginalFileName",
                table: "billings",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentProofStoredFileName",
                table: "billings",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "billings",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentUploadedAt",
                table: "billings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "billings",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "billings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VerifiedByUserId",
                table: "billings",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "container_doctor_inspections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ContainerId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorUserId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Findings = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_container_doctor_inspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_container_doctor_inspections_containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_container_doctor_inspections_users_DoctorUserId",
                        column: x => x.DoctorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "container_inspection_photos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ContainerId = table.Column<long>(type: "bigint", nullable: false),
                    EntryId = table.Column<long>(type: "bigint", nullable: false),
                    PhotoType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalFileName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StoredFileName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewDecision = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewComment = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_container_inspection_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_container_inspection_photos_containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_container_inspection_photos_entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_container_inspection_photos_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "container_transport_tags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Uuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ContainerId = table.Column<long>(type: "bigint", nullable: false),
                    EntryId = table.Column<long>(type: "bigint", nullable: false),
                    TransportType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TaggedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    TaggedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_container_transport_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_container_transport_tags_containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_container_transport_tags_entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_container_transport_tags_users_TaggedByUserId",
                        column: x => x.TaggedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_client_bills_WarehouseBookingId",
                table: "client_bills",
                column: "WarehouseBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_billings_VerifiedByUserId",
                table: "billings",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_accreditation_history_ActorUserId",
                table: "accreditation_history",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_container_doctor_inspections_ContainerId",
                table: "container_doctor_inspections",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_container_doctor_inspections_DoctorUserId",
                table: "container_doctor_inspections",
                column: "DoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_container_doctor_inspections_Uuid",
                table: "container_doctor_inspections",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_container_inspection_photos_ContainerId_PhotoType",
                table: "container_inspection_photos",
                columns: new[] { "ContainerId", "PhotoType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_container_inspection_photos_EntryId",
                table: "container_inspection_photos",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_container_inspection_photos_ReviewedByUserId",
                table: "container_inspection_photos",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_container_inspection_photos_Uuid",
                table: "container_inspection_photos",
                column: "Uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_container_transport_tags_ContainerId",
                table: "container_transport_tags",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_container_transport_tags_EntryId",
                table: "container_transport_tags",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_container_transport_tags_TaggedByUserId",
                table: "container_transport_tags",
                column: "TaggedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_container_transport_tags_Uuid",
                table: "container_transport_tags",
                column: "Uuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_accreditation_history_users_ActorUserId",
                table: "accreditation_history",
                column: "ActorUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_billings_users_VerifiedByUserId",
                table: "billings",
                column: "VerifiedByUserId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_client_bills_warehouse_bookings_WarehouseBookingId",
                table: "client_bills",
                column: "WarehouseBookingId",
                principalTable: "warehouse_bookings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_timeline_events_users_ActorUserId",
                table: "timeline_events",
                column: "ActorUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_accreditation_history_users_ActorUserId",
                table: "accreditation_history");

            migrationBuilder.DropForeignKey(
                name: "FK_billings_users_VerifiedByUserId",
                table: "billings");

            migrationBuilder.DropForeignKey(
                name: "FK_client_bills_warehouse_bookings_WarehouseBookingId",
                table: "client_bills");

            migrationBuilder.DropForeignKey(
                name: "FK_timeline_events_users_ActorUserId",
                table: "timeline_events");

            migrationBuilder.DropTable(
                name: "container_doctor_inspections");

            migrationBuilder.DropTable(
                name: "container_inspection_photos");

            migrationBuilder.DropTable(
                name: "container_transport_tags");

            migrationBuilder.DropIndex(
                name: "IX_client_bills_WarehouseBookingId",
                table: "client_bills");

            migrationBuilder.DropIndex(
                name: "IX_billings_VerifiedByUserId",
                table: "billings");

            migrationBuilder.DropIndex(
                name: "IX_accreditation_history_ActorUserId",
                table: "accreditation_history");

            migrationBuilder.DropColumn(
                name: "WarehouseBookingId",
                table: "client_bills");

            migrationBuilder.DropColumn(
                name: "PaymentNotes",
                table: "billings");

            migrationBuilder.DropColumn(
                name: "PaymentProofContentType",
                table: "billings");

            migrationBuilder.DropColumn(
                name: "PaymentProofOriginalFileName",
                table: "billings");

            migrationBuilder.DropColumn(
                name: "PaymentProofStoredFileName",
                table: "billings");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "billings");

            migrationBuilder.DropColumn(
                name: "PaymentUploadedAt",
                table: "billings");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "billings");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "billings");

            migrationBuilder.DropColumn(
                name: "VerifiedByUserId",
                table: "billings");
        }
    }
}
