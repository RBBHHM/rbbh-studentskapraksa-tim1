using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueActivePhysicalIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_FBAId",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_JMBG",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_PassportNumber",
                table: "RelatedPersons");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPersons_FBAId",
                table: "RelatedPersons",
                column: "FBAId",
                unique: true,
                filter: "[IsActive] = 1 AND [FBAId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPersons_JMBG",
                table: "RelatedPersons",
                column: "JMBG",
                unique: true,
                filter: "[IsActive] = 1 AND [JMBG] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPersons_PassportNumber",
                table: "RelatedPersons",
                column: "PassportNumber",
                unique: true,
                filter: "[IsActive] = 1 AND [PassportNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_FBAId",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_JMBG",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_PassportNumber",
                table: "RelatedPersons");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPersons_FBAId",
                table: "RelatedPersons",
                column: "FBAId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPersons_JMBG",
                table: "RelatedPersons",
                column: "JMBG");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPersons_PassportNumber",
                table: "RelatedPersons",
                column: "PassportNumber");
        }
    }
}
