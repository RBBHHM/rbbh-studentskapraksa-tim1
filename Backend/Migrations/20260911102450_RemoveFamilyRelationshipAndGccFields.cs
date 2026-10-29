using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFamilyRelationshipAndGccFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FamilyRelationshipType",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "GCCName",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "GCCNumber",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "GccName",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "GccNumber",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "RelationshipType",
                table: "FamilyMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FamilyRelationshipType",
                table: "RelatedPersons",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GCCName",
                table: "RelatedPersons",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GCCNumber",
                table: "RelatedPersons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GccName",
                table: "LegalEntities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GccNumber",
                table: "LegalEntities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelationshipType",
                table: "FamilyMembers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}
