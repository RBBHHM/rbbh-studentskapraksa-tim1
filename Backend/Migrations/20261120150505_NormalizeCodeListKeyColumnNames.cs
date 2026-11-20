using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCodeListKeyColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CodeListValues_NormalizedCategory_NormalizedCode",
                table: "CodeListValues");

            migrationBuilder.DropColumn(name: "NormalizedCategory", table: "CodeListValues");
            migrationBuilder.DropColumn(name: "NormalizedCode", table: "CodeListValues");

            migrationBuilder.RenameColumn(
                name: "Kod",
                table: "CodeListValues",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "Kategorija",
                table: "CodeListValues",
                newName: "Category");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Kategorija_LanguageCode_ResourceKey",
                table: "CodeListValues",
                newName: "IX_CodeListValues_Category_LanguageCode_ResourceKey");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Kategorija",
                table: "CodeListValues",
                newName: "IX_CodeListValues_Category");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCategory", table: "CodeListValues", type: "nvarchar(100)",
                maxLength: 100, nullable: true, computedColumnSql: "UPPER(LTRIM(RTRIM([Category])))", stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCode", table: "CodeListValues", type: "nvarchar(50)",
                maxLength: 50, nullable: true, computedColumnSql: "UPPER(LTRIM(RTRIM([Code])))", stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeListValues_NormalizedCategory_NormalizedCode", table: "CodeListValues",
                columns: new[] { "NormalizedCategory", "NormalizedCode" }, unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CodeListValues_NormalizedCategory_NormalizedCode",
                table: "CodeListValues");

            migrationBuilder.DropColumn(name: "NormalizedCategory", table: "CodeListValues");
            migrationBuilder.DropColumn(name: "NormalizedCode", table: "CodeListValues");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "CodeListValues",
                newName: "Kod");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "CodeListValues",
                newName: "Kategorija");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Category_LanguageCode_ResourceKey",
                table: "CodeListValues",
                newName: "IX_CodeListValues_Kategorija_LanguageCode_ResourceKey");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Category",
                table: "CodeListValues",
                newName: "IX_CodeListValues_Kategorija");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCategory", table: "CodeListValues", type: "nvarchar(100)",
                maxLength: 100, nullable: true, computedColumnSql: "UPPER(LTRIM(RTRIM([Kategorija])))", stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCode", table: "CodeListValues", type: "nvarchar(50)",
                maxLength: 50, nullable: true, computedColumnSql: "UPPER(LTRIM(RTRIM([Kod])))", stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeListValues_NormalizedCategory_NormalizedCode", table: "CodeListValues",
                columns: new[] { "NormalizedCategory", "NormalizedCode" }, unique: true);
        }
    }
}
