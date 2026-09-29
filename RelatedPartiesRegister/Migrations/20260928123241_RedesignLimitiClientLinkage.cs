using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class RedesignLimitiClientLinkage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Limiti_Naziv",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "IznosLimita",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "Naziv",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "OsnovniKapital",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "RaspoloziviLimit",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "RegulatorniKapital",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "Utilizacija",
                table: "Limiti");

            migrationBuilder.RenameColumn(
                name: "RokUtilizacije",
                table: "Limiti",
                newName: "RokOcekivaneUtilizacije");

            migrationBuilder.RenameColumn(
                name: "KorigovaniLimit",
                table: "Limiti",
                newName: "OdobreniLimit");

            migrationBuilder.AddColumn<Guid>(
                name: "LegalEntityId",
                table: "Limiti",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "MaksimalnaOcekivanaUtilizacija",
                table: "Limiti",
                type: "decimal(18,2)",
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
                name: "LegalEntityId",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "MaksimalnaOcekivanaUtilizacija",
                table: "Limiti");

            migrationBuilder.RenameColumn(
                name: "RokOcekivaneUtilizacije",
                table: "Limiti",
                newName: "RokUtilizacije");

            migrationBuilder.RenameColumn(
                name: "OdobreniLimit",
                table: "Limiti",
                newName: "KorigovaniLimit");

            migrationBuilder.AddColumn<decimal>(
                name: "IznosLimita",
                table: "Limiti",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Naziv",
                table: "Limiti",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "OsnovniKapital",
                table: "Limiti",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RaspoloziviLimit",
                table: "Limiti",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RegulatorniKapital",
                table: "Limiti",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Utilizacija",
                table: "Limiti",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Limiti_Naziv",
                table: "Limiti",
                column: "Naziv");
        }
    }
}
