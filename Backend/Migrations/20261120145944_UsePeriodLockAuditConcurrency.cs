using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class UsePeriodLockAuditConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "PeriodLocks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "PeriodLocks",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }
    }
}
