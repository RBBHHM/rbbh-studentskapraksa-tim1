using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantSubjectLinkTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeycloakSubjectLinkedAt",
                table: "AppUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "KeycloakSubjectLinkedAt",
                table: "AppUsers",
                type: "datetime2",
                nullable: true);
        }
    }
}
