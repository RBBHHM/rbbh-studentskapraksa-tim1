using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistryCreatorProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatorSubject",
                table: "RelatedPersons",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "RelatedPersons",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatorSubject",
                table: "LegalEntities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "LegalEntities",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorSubject",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "CreatorSubject",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LegalEntities");
        }
    }
}
