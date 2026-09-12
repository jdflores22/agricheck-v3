using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriCheck.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccreditationSubmissionUserUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE s FROM accreditation_submissions s
                INNER JOIN (
                    SELECT UserId, MAX(Id) AS KeepId
                    FROM accreditation_submissions
                    GROUP BY UserId
                    HAVING COUNT(*) > 1
                ) d ON s.UserId = d.UserId AND s.Id <> d.KeepId;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_accreditation_submissions_users_UserId",
                table: "accreditation_submissions");

            migrationBuilder.DropIndex(
                name: "IX_accreditation_submissions_UserId",
                table: "accreditation_submissions");

            migrationBuilder.CreateIndex(
                name: "IX_accreditation_submissions_UserId",
                table: "accreditation_submissions",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_accreditation_submissions_users_UserId",
                table: "accreditation_submissions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_accreditation_submissions_users_UserId",
                table: "accreditation_submissions");

            migrationBuilder.DropIndex(
                name: "IX_accreditation_submissions_UserId",
                table: "accreditation_submissions");

            migrationBuilder.CreateIndex(
                name: "IX_accreditation_submissions_UserId",
                table: "accreditation_submissions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_accreditation_submissions_users_UserId",
                table: "accreditation_submissions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
