using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class UseCanonicalCodeAndRegistryIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_NormalizedFbaId",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_NormalizedPassport",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_CodeListValues_NormalizedCategory_NormalizedCode",
                table: "CodeListValues");

            migrationBuilder.DropColumn(
                name: "NormalizedFbaId",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "NormalizedPassport",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "NormalizedCategory",
                table: "CodeListValues");

            migrationBuilder.DropColumn(
                name: "NormalizedCode",
                table: "CodeListValues");

            migrationBuilder.AlterColumn<string>(
                name: "PassportNumber",
                table: "RelatedPersons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FBAId",
                table: "RelatedPersons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "CodeListValues",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "CodeListValues",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RelatedPersons_FBAId_Trimmed",
                table: "RelatedPersons",
                sql: "DATALENGTH([FBAId]) = DATALENGTH(LTRIM(RTRIM([FBAId])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RelatedPersons_PassportNumber_Trimmed",
                table: "RelatedPersons",
                sql: "DATALENGTH([PassportNumber]) = DATALENGTH(LTRIM(RTRIM([PassportNumber])))");

            migrationBuilder.CreateIndex(
                name: "IX_CodeListValues_Category_Code",
                table: "CodeListValues",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CodeListValues_Category_Trimmed",
                table: "CodeListValues",
                sql: "DATALENGTH([Category]) = DATALENGTH(LTRIM(RTRIM([Category])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CodeListValues_Code_Trimmed",
                table: "CodeListValues",
                sql: "DATALENGTH([Code]) = DATALENGTH(LTRIM(RTRIM([Code])))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RelatedPersons_FBAId_Trimmed",
                table: "RelatedPersons");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RelatedPersons_PassportNumber_Trimmed",
                table: "RelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_CodeListValues_Category_Code",
                table: "CodeListValues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CodeListValues_Category_Trimmed",
                table: "CodeListValues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CodeListValues_Code_Trimmed",
                table: "CodeListValues");

            migrationBuilder.AlterColumn<string>(
                name: "PassportNumber",
                table: "RelatedPersons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AlterColumn<string>(
                name: "FBAId",
                table: "RelatedPersons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "CodeListValues",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50,
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "CodeListValues",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_CI_AS");

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
                name: "NormalizedCategory",
                table: "CodeListValues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Category])))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCode",
                table: "CodeListValues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Code])))",
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
                name: "IX_CodeListValues_NormalizedCategory_NormalizedCode",
                table: "CodeListValues",
                columns: new[] { "NormalizedCategory", "NormalizedCode" },
                unique: true);
        }
    }
}
