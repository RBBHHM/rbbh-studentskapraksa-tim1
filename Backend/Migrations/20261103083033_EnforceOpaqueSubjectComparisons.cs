using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOpaqueSubjectComparisons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Retain all identifiers verbatim; fail for operator reconciliation
            // rather than selecting an identity or deleting a duplicate.
            migrationBuilder.Sql("""
                IF EXISTS(SELECT 1 FROM AppUsers GROUP BY KeycloakId COLLATE Latin1_General_100_BIN2 HAVING COUNT(*) > 1)
                  OR EXISTS(SELECT 1 FROM RoleRequests GROUP BY RequesterSubject COLLATE Latin1_General_100_BIN2, ClientRequestId HAVING COUNT(*) > 1)
                  OR EXISTS(SELECT 1 FROM RoleRequests WHERE Status = N'pending' GROUP BY RequesterSubject COLLATE Latin1_General_100_BIN2 HAVING COUNT(*) > 1)
                  OR EXISTS(SELECT 1 FROM Notifications GROUP BY RecipientSubject COLLATE Latin1_General_100_BIN2, EventKey HAVING COUNT(*) > 1)
                    THROW 51009, 'Opaque identity uniqueness requires reconciliation before migration.', 1;
                """);
            migrationBuilder.AlterColumn<string>(
                name: "RequestedBySubject",
                table: "UnlockRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequesterSubject",
                table: "RoleRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "RecipientSubject",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "KeycloakId",
                table: "AppUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RequestedBySubject",
                table: "UnlockRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AlterColumn<string>(
                name: "RequesterSubject",
                table: "RoleRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientSubject",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AlterColumn<string>(
                name: "KeycloakId",
                table: "AppUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_BIN2");
        }
    }
}
