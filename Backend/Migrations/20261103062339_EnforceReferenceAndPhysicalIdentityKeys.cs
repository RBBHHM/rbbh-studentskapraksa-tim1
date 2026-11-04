using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class EnforceReferenceAndPhysicalIdentityKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM CodeListDefinitions GROUP BY UPPER(LTRIM(RTRIM(Name))) HAVING COUNT(*) > 1)
                    THROW 51005, 'Codebook definition normalized-name preflight failed; reconcile duplicates before deployment.', 1;
                IF EXISTS (SELECT 1 FROM RelatedPersons WHERE IsActive = 1 AND PassportNumber IS NOT NULL GROUP BY UPPER(LTRIM(RTRIM(PassportNumber))) HAVING COUNT(*) > 1)
                    THROW 51006, 'Active physical passport preflight failed; reconcile duplicates before deployment.', 1;
                IF EXISTS (SELECT 1 FROM RelatedPersons WHERE IsActive = 1 AND FBAId IS NOT NULL GROUP BY UPPER(LTRIM(RTRIM(FBAId))) HAVING COUNT(*) > 1)
                    THROW 51007, 'Active physical FBA preflight failed; reconcile duplicates before deployment.', 1;
                """);
            migrationBuilder.AddColumn<string>(
                name: "NormalizedFbaId",
                table: "RelatedPersons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([FBAId])))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPassport",
                table: "RelatedPersons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([PassportNumber])))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "CodeListDefinitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Name])))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPersons_NormalizedFbaId",
                table: "RelatedPersons",
                column: "NormalizedFbaId",
                unique: true,
                filter: "[IsActive] = 1 AND [FBAId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPersons_NormalizedPassport",
                table: "RelatedPersons",
                column: "NormalizedPassport",
                unique: true,
                filter: "[IsActive] = 1 AND [PassportNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CodeListDefinitions_NormalizedName",
                table: "CodeListDefinitions",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_NormalizedFbaId",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_NormalizedPassport",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_CodeListDefinitions_NormalizedName",
                table: "CodeListDefinitions");

            migrationBuilder.DropColumn(
                name: "NormalizedFbaId",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "NormalizedPassport",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "CodeListDefinitions");
        }
    }
}
