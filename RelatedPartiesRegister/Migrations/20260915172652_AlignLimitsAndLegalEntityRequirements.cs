using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignLimitsAndLegalEntityRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RaspoloziviLimit",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "ConnectedWithBank",
                table: "LegalEntities");

            migrationBuilder.RenameColumn(
                name: "Utilizacija",
                table: "Limiti",
                newName: "MaksimalnoOcekivanaUtilizacija");

            migrationBuilder.Sql(
                "UPDATE [LegalEntities] SET [DateFrom] = COALESCE([CreatedAt], SYSUTCDATETIME()) WHERE [DateFrom] IS NULL");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateFrom",
                table: "LegalEntities",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaksimalnoOcekivanaUtilizacija",
                table: "Limiti",
                newName: "Utilizacija");

            migrationBuilder.AddColumn<decimal>(
                name: "RaspoloziviLimit",
                table: "Limiti",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateFrom",
                table: "LegalEntities",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<bool>(
                name: "ConnectedWithBank",
                table: "LegalEntities",
                type: "bit",
                nullable: true);
        }
    }
}
