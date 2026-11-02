using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodRequestOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentRequestId",
                table: "UnlockRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedBySubject",
                table: "UnlockRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "UnlockRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnlockRequests_ParentRequestId",
                table: "UnlockRequests",
                column: "ParentRequestId",
                unique: true,
                filter: "[ParentRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UnlockRequests_RequestedBySubject",
                table: "UnlockRequests",
                column: "RequestedBySubject");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnlockRequests_ParentRequestId",
                table: "UnlockRequests");

            migrationBuilder.DropIndex(
                name: "IX_UnlockRequests_RequestedBySubject",
                table: "UnlockRequests");

            migrationBuilder.DropColumn(
                name: "ParentRequestId",
                table: "UnlockRequests");

            migrationBuilder.DropColumn(
                name: "RequestedBySubject",
                table: "UnlockRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "UnlockRequests");
        }
    }
}
