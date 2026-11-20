using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRegistryColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaticniBroj",
                table: "LegalEntities",
                newName: "RegistrationNumber");

            migrationBuilder.RenameColumn(
                name: "Matbroj",
                table: "LegalEntities",
                newName: "BankCustomerNumber");

            migrationBuilder.RenameColumn(
                name: "RedoslijedPrikaza",
                table: "CodeListValues",
                newName: "DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "Opis",
                table: "CodeListValues",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Naziv",
                table: "CodeListValues",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "KreiraoKorisnik",
                table: "CodeListValues",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "KreiranDatum",
                table: "CodeListValues",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "IzmijenjenDatum",
                table: "CodeListValues",
                newName: "ModifiedAt");

            migrationBuilder.RenameColumn(
                name: "IzmijenioKorisnik",
                table: "CodeListValues",
                newName: "ModifiedBy");

            migrationBuilder.RenameColumn(
                name: "Aktivan",
                table: "CodeListValues",
                newName: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RegistrationNumber",
                table: "LegalEntities",
                newName: "MaticniBroj");

            migrationBuilder.RenameColumn(
                name: "BankCustomerNumber",
                table: "LegalEntities",
                newName: "Matbroj");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "CodeListValues",
                newName: "Naziv");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "CodeListValues",
                newName: "IzmijenioKorisnik");

            migrationBuilder.RenameColumn(
                name: "ModifiedAt",
                table: "CodeListValues",
                newName: "IzmijenjenDatum");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "CodeListValues",
                newName: "Aktivan");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "CodeListValues",
                newName: "RedoslijedPrikaza");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "CodeListValues",
                newName: "Opis");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "CodeListValues",
                newName: "KreiraoKorisnik");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CodeListValues",
                newName: "KreiranDatum");
        }
    }
}
