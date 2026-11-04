using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistryGccAndExposure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GccName",
                table: "LegalEntities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GccNumber",
                table: "LegalEntities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLargeExposure",
                table: "LegalEntities",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GccName",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "GccNumber",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "IsLargeExposure",
                table: "LegalEntities");
        }
    }
}
