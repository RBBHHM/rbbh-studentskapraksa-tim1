using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class AddLimitImportIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImportFingerprint",
                table: "Limiti",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Limiti_ImportFingerprint",
                table: "Limiti",
                column: "ImportFingerprint",
                unique: true,
                filter: "[ImportFingerprint] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Limiti_ImportFingerprint",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "ImportFingerprint",
                table: "Limiti");
        }
    }
}
