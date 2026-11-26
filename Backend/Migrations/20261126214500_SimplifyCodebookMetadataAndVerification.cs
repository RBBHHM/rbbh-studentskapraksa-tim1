using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261126214500_SimplifyCodebookMetadataAndVerification")]
public sealed class SimplifyCodebookMetadataAndVerification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("VERIFICATION_ORIGIN_FINGERPRINT", "LEGAL_ENTITIES",
            type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.Sql("""
            UPDATE legal SET VERIFICATION_ORIGIN_FINGERPRINT = origin.FINGERPRINT
            FROM dbo.LEGAL_ENTITIES AS legal
            INNER JOIN dbo.LEGAL_ENTITY_VERIFICATION_ORIGINS AS origin ON origin.LEGAL_ENTITY_ID = legal.ID;

            UPDATE definition SET IS_SYSTEM_REQUIRED = 1
            FROM dbo.CODELIST_DEFINITIONS AS definition
            WHERE EXISTS (
                SELECT 1 FROM dbo.CODELIST_VALUES AS value
                WHERE value.DEFINITION_ID = definition.ID AND value.IS_SYSTEM_REQUIRED = 1);
            """);
        migrationBuilder.DropTable("LEGAL_ENTITY_VERIFICATION_ORIGINS");
        migrationBuilder.DropColumn("OWNER", "CODELIST_DEFINITIONS");
        migrationBuilder.DropColumn("VERSION", "CODELIST_DEFINITIONS");
        migrationBuilder.DropColumn("IS_SYSTEM_REQUIRED", "CODELIST_VALUES");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("OWNER", "CODELIST_DEFINITIONS",
            type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "registry");
        migrationBuilder.AddColumn<int>("VERSION", "CODELIST_DEFINITIONS",
            type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>("IS_SYSTEM_REQUIRED", "CODELIST_VALUES",
            type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.CreateTable("LEGAL_ENTITY_VERIFICATION_ORIGINS", table => new
        {
            LEGAL_ENTITY_ID = table.Column<long>("bigint", nullable: false),
            FINGERPRINT = table.Column<string>("nvarchar(64)", maxLength: 64, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_LEGAL_ENTITY_VERIFICATION_ORIGINS", origin => origin.LEGAL_ENTITY_ID);
            table.ForeignKey("FK_LEGAL_ENTITY_VERIFICATION_ORIGINS_LEGAL_ENTITIES_LEGAL_ENTITY_ID",
                origin => origin.LEGAL_ENTITY_ID, "LEGAL_ENTITIES", "ID", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.Sql("""
            INSERT INTO dbo.LEGAL_ENTITY_VERIFICATION_ORIGINS (LEGAL_ENTITY_ID, FINGERPRINT)
            SELECT ID, VERIFICATION_ORIGIN_FINGERPRINT
            FROM dbo.LEGAL_ENTITIES
            WHERE VERIFICATION_ORIGIN_FINGERPRINT IS NOT NULL;

            UPDATE value SET IS_SYSTEM_REQUIRED = definition.IS_SYSTEM_REQUIRED
            FROM dbo.CODELIST_VALUES AS value
            INNER JOIN dbo.CODELIST_DEFINITIONS AS definition ON definition.ID = value.DEFINITION_ID;
            """);
        migrationBuilder.DropColumn("VERIFICATION_ORIGIN_FINGERPRINT", "LEGAL_ENTITIES");
    }
}
