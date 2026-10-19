using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20261020120000_DeliveryComplianceDaReview")]
public partial class DeliveryComplianceDaReview : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "DaReviewedAtUtc",
            table: "delivery_compliance_events",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "DaReviewedByUserId",
            table: "delivery_compliance_events",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DaReviewNotes",
            table: "delivery_compliance_events",
            type: "varchar(2000)",
            maxLength: 2000,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_delivery_compliance_events_DaReviewedAtUtc",
            table: "delivery_compliance_events",
            column: "DaReviewedAtUtc");

        migrationBuilder.AddForeignKey(
            name: "FK_delivery_compliance_events_users_DaReviewedByUserId",
            table: "delivery_compliance_events",
            column: "DaReviewedByUserId",
            principalTable: "users",
            principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_delivery_compliance_events_users_DaReviewedByUserId",
            table: "delivery_compliance_events");

        migrationBuilder.DropIndex(
            name: "IX_delivery_compliance_events_DaReviewedAtUtc",
            table: "delivery_compliance_events");

        migrationBuilder.DropColumn(name: "DaReviewNotes", table: "delivery_compliance_events");
        migrationBuilder.DropColumn(name: "DaReviewedByUserId", table: "delivery_compliance_events");
        migrationBuilder.DropColumn(name: "DaReviewedAtUtc", table: "delivery_compliance_events");
    }
}
