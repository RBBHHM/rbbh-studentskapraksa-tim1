using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class SplitCodeListLocalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CodeListValues_Category_LanguageCode_ResourceKey",
                table: "CodeListValues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CodeListValues_Code_Trimmed",
                table: "CodeListValues");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "CodeListValues",
                newName: "ValueKey");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Category_Code",
                table: "CodeListValues",
                newName: "IX_CodeListValues_Category_ValueKey");

            migrationBuilder.CreateTable(
                name: "CodeListLocalization",
                columns: table => new
                {
                    CodeListID = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ResourceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, collation: "Latin1_General_100_CI_AS"),
                    ResourceLabel = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResourceHelper = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResourcePlaceholder = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeListLocalization", x => x.CodeListID);
                    table.ForeignKey(
                        name: "FK_CodeListLocalization_CodeListValues_CodeListID",
                        column: x => x.CodeListID,
                        principalTable: "CodeListValues",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                EXEC(N'INSERT INTO [dbo].[CodeListLocalization]
                    ([CodeListID], [Category], [LanguageCode], [ResourceKey], [ResourceLabel], [ResourceHelper], [ResourcePlaceholder])
                SELECT [ID], [Category], [LanguageCode], [ResourceKey], [ResourceLabel], [ResourceHelper], [ResourcePlaceholder]
                FROM [dbo].[CodeListValues]
                WHERE [LanguageCode] IS NOT NULL OR [ResourceKey] IS NOT NULL OR [ResourceLabel] IS NOT NULL
                    OR [ResourceHelper] IS NOT NULL OR [ResourcePlaceholder] IS NOT NULL');
                """);

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "CodeListValues");

            migrationBuilder.DropColumn(
                name: "ResourceHelper",
                table: "CodeListValues");

            migrationBuilder.DropColumn(
                name: "ResourceKey",
                table: "CodeListValues");

            migrationBuilder.DropColumn(
                name: "ResourceLabel",
                table: "CodeListValues");

            migrationBuilder.DropColumn(
                name: "ResourcePlaceholder",
                table: "CodeListValues");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CodeListValues_ValueKey_Trimmed",
                table: "CodeListValues",
                sql: "DATALENGTH([ValueKey]) = DATALENGTH(LTRIM(RTRIM([ValueKey])))");

            migrationBuilder.CreateIndex(
                name: "IX_CodeListLocalization_Category_LanguageCode_ResourceKey",
                table: "CodeListLocalization",
                columns: new[] { "Category", "LanguageCode", "ResourceKey" },
                unique: true,
                filter: "[LanguageCode] IS NOT NULL AND [ResourceKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CodeListValues_ValueKey_Trimmed",
                table: "CodeListValues");

            migrationBuilder.RenameColumn(
                name: "ValueKey",
                table: "CodeListValues",
                newName: "Code");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Category_ValueKey",
                table: "CodeListValues",
                newName: "IX_CodeListValues_Category_Code");

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "CodeListValues",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceHelper",
                table: "CodeListValues",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceKey",
                table: "CodeListValues",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "Latin1_General_100_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "ResourceLabel",
                table: "CodeListValues",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourcePlaceholder",
                table: "CodeListValues",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                EXEC(N'UPDATE [values]
                SET [LanguageCode] = [localization].[LanguageCode],
                    [ResourceKey] = [localization].[ResourceKey],
                    [ResourceLabel] = [localization].[ResourceLabel],
                    [ResourceHelper] = [localization].[ResourceHelper],
                    [ResourcePlaceholder] = [localization].[ResourcePlaceholder]
                FROM [dbo].[CodeListValues] AS [values]
                INNER JOIN [dbo].[CodeListLocalization] AS [localization]
                    ON [localization].[CodeListID] = [values].[ID]');
                """);

            migrationBuilder.DropTable(
                name: "CodeListLocalization");

            migrationBuilder.CreateIndex(
                name: "IX_CodeListValues_Category_LanguageCode_ResourceKey",
                table: "CodeListValues",
                columns: new[] { "Category", "LanguageCode", "ResourceKey" },
                unique: true,
                filter: "[LanguageCode] IS NOT NULL AND [ResourceKey] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CodeListValues_Code_Trimmed",
                table: "CodeListValues",
                sql: "DATALENGTH([Code]) = DATALENGTH(LTRIM(RTRIM([Code])))");
        }
    }
}
