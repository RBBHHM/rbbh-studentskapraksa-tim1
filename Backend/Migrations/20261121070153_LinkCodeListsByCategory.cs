using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class LinkCodeListsByCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                EXEC(N'IF EXISTS (
                    SELECT 1
                    FROM dbo.CodeListValues AS value
                    JOIN dbo.CodeListDefinitions AS definition ON definition.Id = value.DefinitionId
                    WHERE value.Category <> definition.Name
                )
                    THROW 51000, ''Resolve conflicting code list categories before removing DefinitionId.'', 1;');
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_CodeListValues_CodeListDefinitions_DefinitionId",
                table: "CodeListValues");

            migrationBuilder.DropIndex(
                name: "IX_CodeListValues_DefinitionId",
                table: "CodeListValues");

            migrationBuilder.DropColumn(
                name: "DefinitionId",
                table: "CodeListValues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefinitionId",
                table: "CodeListValues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                EXEC(N'UPDATE value
                SET DefinitionId = definition.Id
                FROM dbo.CodeListValues AS value
                JOIN dbo.CodeListDefinitions AS definition ON definition.Name = value.Category;');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_CodeListValues_DefinitionId",
                table: "CodeListValues",
                column: "DefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CodeListValues_CodeListDefinitions_DefinitionId",
                table: "CodeListValues",
                column: "DefinitionId",
                principalTable: "CodeListDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
