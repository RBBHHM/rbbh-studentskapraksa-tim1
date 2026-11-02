using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class EnforceNormalizedApplicationUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Read-only preflight precedes every added column/index/constraint.
            // Abort for operator reconciliation; never delete/reassign production rows.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM AppUsers GROUP BY UPPER(LTRIM(RTRIM(Email))) HAVING COUNT(*) > 1)
                    THROW 51000, 'User identity preflight: duplicate normalized email requires approved reconciliation.', 1;
                IF EXISTS (SELECT 1 FROM AppUsers GROUP BY UPPER(LTRIM(RTRIM(Username))) HAVING COUNT(*) > 1)
                    THROW 51000, 'User identity preflight: duplicate normalized username requires approved reconciliation.', 1;
                IF EXISTS (SELECT 1 FROM UserRoles ur LEFT JOIN AppUsers u ON u.Id = ur.UserId WHERE u.Id IS NULL)
                    THROW 51000, 'User identity preflight: orphan role assignments require approved reconciliation.', 1;
                """);

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
                name: "IX_AppUsers_NormalizedEmail",
                table: "AppUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_NormalizedUsername",
                table: "AppUsers",
                column: "NormalizedUsername",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_AppUsers_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_AppUsers_UserId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_NormalizedEmail",
                table: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_NormalizedUsername",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedUsername",
                table: "AppUsers");
        }
    }
}
