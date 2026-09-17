using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCapitalFromLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatumKapitala",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "DopunskiKapital",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "OsnovniKapital",
                table: "Limiti");

            migrationBuilder.DropColumn(
                name: "RegulatorniKapital",
                table: "Limiti");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<decimal>(
                name: "OsnovniKapital",
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
        }
    }
}
