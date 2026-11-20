using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class UseRegistryAuditConcurrencyAndLabelPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LegalEntities");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Permissions",
                newName: "Label");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Label",
                table: "Permissions",
                newName: "Description");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "RelatedPersons",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "LegalEntities",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }
    }
}
