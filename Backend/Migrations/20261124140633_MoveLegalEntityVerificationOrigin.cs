using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class MoveLegalEntityVerificationOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalEntityVerificationOrigins",
                columns: table => new
                {
                    LegalEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalEntityVerificationOrigins", x => x.LegalEntityId);
                    table.ForeignKey(
                        name: "FK_LegalEntityVerificationOrigins_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO dbo.LegalEntityVerificationOrigins (LegalEntityId, Subject)
                SELECT Id, CreatorSubject
                FROM dbo.LegalEntities
                WHERE NULLIF(LTRIM(RTRIM(CreatorSubject)), N'') IS NOT NULL;

                IF COL_LENGTH(N'dbo.LegalEntities', N'Version') IS NOT NULL
                    ALTER TABLE dbo.LegalEntities DROP COLUMN [Version];
                """);

            migrationBuilder.DropColumn(
                name: "ConnectionDescription",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "CreatorSubject",
                table: "LegalEntities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConnectionDescription",
                table: "LegalEntities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatorSubject",
                table: "LegalEntities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE legal
                    SET CreatorSubject = origin.Subject
                FROM dbo.LegalEntities AS legal
                INNER JOIN dbo.LegalEntityVerificationOrigins AS origin ON origin.LegalEntityId = legal.Id;
                """);

            migrationBuilder.DropTable(name: "LegalEntityVerificationOrigins");
        }
    }
}
