using System;
using AgriCheck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AgriCheckDbContext))]
[Migration("20260913030000_ClientBillAgencyBillingLink")]
public partial class ClientBillAgencyBillingLink : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "AgencyBillingId",
            table: "client_bills",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_client_bills_AgencyBillingId",
            table: "client_bills",
            column: "AgencyBillingId",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_client_bills_billings_AgencyBillingId",
            table: "client_bills",
            column: "AgencyBillingId",
            principalTable: "billings",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_client_bills_billings_AgencyBillingId",
            table: "client_bills");

        migrationBuilder.DropIndex(
            name: "IX_client_bills_AgencyBillingId",
            table: "client_bills");

        migrationBuilder.DropColumn(
            name: "AgencyBillingId",
            table: "client_bills");
    }
}
