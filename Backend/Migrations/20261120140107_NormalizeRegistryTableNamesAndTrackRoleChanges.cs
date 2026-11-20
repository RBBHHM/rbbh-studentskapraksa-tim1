using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRegistryTableNamesAndTrackRoleChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CodeLists_CodeListDefinitions_DefinitionId",
                table: "CodeLists");

            migrationBuilder.DropForeignKey(
                name: "FK_Limiti_LegalEntities_LegalEntityId",
                table: "Limiti");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Limiti",
                table: "Limiti");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CodeLists",
                table: "CodeLists");

            migrationBuilder.RenameTable(
                name: "Limiti",
                newName: "Limits");

            migrationBuilder.RenameTable(
                name: "CodeLists",
                newName: "CodeListValues");

            migrationBuilder.RenameColumn(
                name: "TipLimita",
                table: "Limits",
                newName: "LimitType");

            migrationBuilder.RenameColumn(
                name: "RokOcekivaneUtilizacije",
                table: "Limits",
                newName: "ExpectedUtilizationDate");

            migrationBuilder.RenameColumn(
                name: "OdobreniLimit",
                table: "Limits",
                newName: "ApprovedLimit");

            migrationBuilder.RenameColumn(
                name: "MaksimalnaOcekivanaUtilizacija",
                table: "Limits",
                newName: "MaximumExpectedUtilization");

            migrationBuilder.RenameColumn(
                name: "Komentar",
                table: "Limits",
                newName: "Comment");

            migrationBuilder.RenameIndex(
                name: "IX_Limiti_TipLimita",
                table: "Limits",
                newName: "IX_Limits_LimitType");

            migrationBuilder.RenameIndex(
                name: "IX_Limiti_LegalEntityId",
                table: "Limits",
                newName: "IX_Limits_LegalEntityId");

            migrationBuilder.RenameIndex(
                name: "IX_Limiti_ImportFingerprint",
                table: "Limits",
                newName: "IX_Limits_ImportFingerprint");

            migrationBuilder.RenameIndex(
                name: "IX_CodeLists_NormalizedCategory_NormalizedCode",
                table: "CodeListValues",
                newName: "IX_CodeListValues_NormalizedCategory_NormalizedCode");

            migrationBuilder.RenameIndex(
                name: "IX_CodeLists_Kategorija_LanguageCode_ResourceKey",
                table: "CodeListValues",
                newName: "IX_CodeListValues_Kategorija_LanguageCode_ResourceKey");

            migrationBuilder.RenameIndex(
                name: "IX_CodeLists_Kategorija",
                table: "CodeListValues",
                newName: "IX_CodeListValues_Kategorija");

            migrationBuilder.RenameIndex(
                name: "IX_CodeLists_DefinitionId",
                table: "CodeListValues",
                newName: "IX_CodeListValues_DefinitionId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "UserRoles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "UserRoles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Limits",
                table: "Limits",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CodeListValues",
                table: "CodeListValues",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_CodeListValues_CodeListDefinitions_DefinitionId",
                table: "CodeListValues",
                column: "DefinitionId",
                principalTable: "CodeListDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Limits_LegalEntities_LegalEntityId",
                table: "Limits",
                column: "LegalEntityId",
                principalTable: "LegalEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CodeListValues_CodeListDefinitions_DefinitionId",
                table: "CodeListValues");

            migrationBuilder.DropForeignKey(
                name: "FK_Limits_LegalEntities_LegalEntityId",
                table: "Limits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Limits",
                table: "Limits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CodeListValues",
                table: "CodeListValues");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "UserRoles");

            migrationBuilder.RenameTable(
                name: "Limits",
                newName: "Limiti");

            migrationBuilder.RenameTable(
                name: "CodeListValues",
                newName: "CodeLists");

            migrationBuilder.RenameColumn(
                name: "MaximumExpectedUtilization",
                table: "Limiti",
                newName: "MaksimalnaOcekivanaUtilizacija");

            migrationBuilder.RenameColumn(
                name: "LimitType",
                table: "Limiti",
                newName: "TipLimita");

            migrationBuilder.RenameColumn(
                name: "ExpectedUtilizationDate",
                table: "Limiti",
                newName: "RokOcekivaneUtilizacije");

            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "Limiti",
                newName: "Komentar");

            migrationBuilder.RenameColumn(
                name: "ApprovedLimit",
                table: "Limiti",
                newName: "OdobreniLimit");

            migrationBuilder.RenameIndex(
                name: "IX_Limits_LimitType",
                table: "Limiti",
                newName: "IX_Limiti_TipLimita");

            migrationBuilder.RenameIndex(
                name: "IX_Limits_LegalEntityId",
                table: "Limiti",
                newName: "IX_Limiti_LegalEntityId");

            migrationBuilder.RenameIndex(
                name: "IX_Limits_ImportFingerprint",
                table: "Limiti",
                newName: "IX_Limiti_ImportFingerprint");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_NormalizedCategory_NormalizedCode",
                table: "CodeLists",
                newName: "IX_CodeLists_NormalizedCategory_NormalizedCode");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Kategorija_LanguageCode_ResourceKey",
                table: "CodeLists",
                newName: "IX_CodeLists_Kategorija_LanguageCode_ResourceKey");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_Kategorija",
                table: "CodeLists",
                newName: "IX_CodeLists_Kategorija");

            migrationBuilder.RenameIndex(
                name: "IX_CodeListValues_DefinitionId",
                table: "CodeLists",
                newName: "IX_CodeLists_DefinitionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Limiti",
                table: "Limiti",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CodeLists",
                table: "CodeLists",
                column: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_CodeLists_CodeListDefinitions_DefinitionId",
                table: "CodeLists",
                column: "DefinitionId",
                principalTable: "CodeListDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Limiti_LegalEntities_LegalEntityId",
                table: "Limiti",
                column: "LegalEntityId",
                principalTable: "LegalEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
