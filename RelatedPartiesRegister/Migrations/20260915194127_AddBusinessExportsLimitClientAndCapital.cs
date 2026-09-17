using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessExportsLimitClientAndCapital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GCCName",
                table: "RelatedPersons",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GCCNumber",
                table: "RelatedPersons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumKapitala",
                table: "Limiti",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DopunskiKapital",
                table: "Limiti",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "LegalEntityId",
                table: "Limiti",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GCCName",
                table: "LegalEntities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GCCNumber",
                table: "LegalEntities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialRelationBasis",
                table: "LegalEntities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Limiti_LegalEntityId",
                table: "Limiti",
                column: "LegalEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Limiti_LegalEntities_LegalEntityId",
                table: "Limiti",
                column: "LegalEntityId",
                principalTable: "LegalEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Limiti_LegalEntities_LegalEntityId",
                table: "Limiti");

            migrationBuilder.DropIndex(
                name: "IX_Limiti_LegalEntityId",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "GCCName",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "GCCNumber",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "DatumKapitala",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "DopunskiKapital",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "LegalEntityId",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "GCCName",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "GCCNumber",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "SpecialRelationBasis",
                table: "LegalEntities");
        }
    }
}
