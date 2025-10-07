using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class ModelUpdate_ReportsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StartAt",
                table: "Sessions",
                column: "StartAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionItems_AddedAt",
                table: "SessionItems",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Status",
                table: "Rooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaidAt",
                table: "Payments",
                column: "PaidAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_StartAt",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_SessionItems_AddedAt",
                table: "SessionItems");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_Status",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaidAt",
                table: "Payments");
        }
    }
}
