using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCanonicalNamesWithoutDuplicateColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_CodeListDefinitions_NormalizedName",
                table: "CodeListDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_NormalizedEmail",
                table: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_NormalizedUsername",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "CodeListDefinitions");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedUsername",
                table: "AppUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CodeListDefinitions",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AppUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_CI_AS",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            // Some existing databases reached this migration without the legacy email index.
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS [IX_AppUsers_Email] ON [dbo].[AppUsers];
                ALTER TABLE [dbo].[AppUsers]
                    ALTER COLUMN [Email] nvarchar(200) COLLATE Latin1_General_100_CI_AS NOT NULL;
                CREATE UNIQUE INDEX [IX_AppUsers_Email] ON [dbo].[AppUsers] ([Email]);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_Name_Trimmed",
                table: "Roles",
                sql: "DATALENGTH([Name]) = DATALENGTH(LTRIM(RTRIM([Name])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CodeListDefinitions_Name_Trimmed",
                table: "CodeListDefinitions",
                sql: "DATALENGTH([Name]) = DATALENGTH(LTRIM(RTRIM([Name])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppUsers_Email_Trimmed",
                table: "AppUsers",
                sql: "DATALENGTH([Email]) = DATALENGTH(LTRIM(RTRIM([Email])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppUsers_Username_Trimmed",
                table: "AppUsers",
                sql: "DATALENGTH([Username]) = DATALENGTH(LTRIM(RTRIM([Username])))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_Name_Trimmed",
                table: "Roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CodeListDefinitions_Name_Trimmed",
                table: "CodeListDefinitions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppUsers_Email_Trimmed",
                table: "AppUsers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppUsers_Username_Trimmed",
                table: "AppUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CodeListDefinitions",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "AppUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldCollation: "Latin1_General_100_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Name])))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "CodeListDefinitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Name])))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "AppUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Email])))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUsername",
                table: "AppUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Username])))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeListDefinitions_NormalizedName",
                table: "CodeListDefinitions",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_NormalizedEmail",
                table: "AppUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_NormalizedUsername",
                table: "AppUsers",
                column: "NormalizedUsername",
                unique: true);
        }
    }
}
