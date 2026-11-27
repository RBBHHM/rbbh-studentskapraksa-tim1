using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261127072000_SeparateCodebookRowStatus")]
public sealed class SeparateCodebookRowStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("IS_ACTIVE", "CODELIST_VALUES", type: "bit", nullable: false, defaultValue: true);
        migrationBuilder.Sql("UPDATE dbo.CODELIST_VALUES SET IS_ACTIVE = ID1;");
        migrationBuilder.AlterColumn<bool>("ID1", "CODELIST_VALUES", type: "bit", nullable: true,
            oldClrType: typeof(bool), oldType: "bit");
        migrationBuilder.AlterColumn<string>("ID15", "CODELIST_VALUES", type: "nvarchar(100)", maxLength: 100,
            nullable: true, collation: "Latin1_General_100_CI_AS", oldClrType: typeof(string),
            oldType: "nvarchar(100)", oldMaxLength: 100, oldCollation: "Latin1_General_100_CI_AS");
        migrationBuilder.AlterColumn<string>("ID17", "CODELIST_VALUES", type: "nvarchar(4000)", maxLength: 4000,
            nullable: true, oldClrType: typeof(string), oldType: "nvarchar(4000)", oldMaxLength: 4000);
        migrationBuilder.Sql("""
            UPDATE dbo.CODELIST_VALUES SET ID15 = NULL WHERE ID15 = N'';
            UPDATE dbo.CODELIST_VALUES SET ID17 = NULL WHERE ID17 = N'';
            UPDATE v SET ID1 = NULL
            FROM dbo.CODELIST_VALUES v
            JOIN dbo.CODELIST_DEFINITIONS d ON d.ID = v.DEFINITION_ID
            WHERE d.COLUMN_NAME_1 IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore a backup to reverse independent codebook status and boolean values.");
}
