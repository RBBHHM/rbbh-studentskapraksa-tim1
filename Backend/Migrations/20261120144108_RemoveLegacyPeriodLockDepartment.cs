using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyPeriodLockDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                EXEC(N'IF EXISTS (SELECT 1 FROM dbo.PeriodLocks WITH (TABLOCKX, HOLDLOCK) WHERE Department IS NOT NULL)
                    THROW 51000, ''Reconcile legacy department locks before removing PeriodLocks.Department.'', 1;');
                """);

            migrationBuilder.DropColumn(
                name: "Department",
                table: "PeriodLocks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "PeriodLocks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
