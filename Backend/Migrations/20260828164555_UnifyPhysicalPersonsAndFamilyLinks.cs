using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class UnifyPhysicalPersonsAndFamilyLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FamilyRelationshipType",
                table: "RelatedPersons",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedToPersonId",
                table: "RelatedPersons",
                type: "uniqueidentifier",
                nullable: true);

            // Postojeći članovi porodice postaju punopravna fizička lica u istoj tabeli.
            // ID se zadržava, a stara ParentFamilyMemberId veza postaje samoreferentna
            // RelatedToPersonId veza. Stari redovi se samo deaktiviraju radi sigurnog rollbacka.
            migrationBuilder.Sql(
                """
                INSERT INTO [RelatedPersons]
                (
                    [Id], [FirstName], [LastName], [Residency], [JMBG], [PassportNumber], [FBAId],
                    [GCCNumber], [GCCName], [RelationBasis], [RelationDescription],
                    [SpecialRelationBasis], [DateFrom], [DateTo], [IsIdentifiedStaff],
                    [ConnectedWithBank], [SpecialRelationshipWithBank], [SpecialContract],
                    [MalusClawback], [DeclarationNoFamilyMembers], [Status], [IsActive],
                    [CreatedAt], [CreatedBy], [ModifiedAt], [ModifiedBy], [VerifiedBy], [VerifiedAt],
                    [FamilyRelationshipType], [RelatedToPersonId]
                )
                SELECT
                    fm.[Id], fm.[FirstName], fm.[LastName], fm.[Residency], fm.[JMBG],
                    fm.[PassportNumber], fm.[FBAId], N'0', N'Migrirani porodični zapis',
                    N'PORODICNA_VEZA', fm.[RelationshipType], N'UZA_PORODICA',
                    fm.[CreatedAt], NULL, 0, 1, 0, 0, 0, 1, N'Draft', fm.[IsActive],
                    fm.[CreatedAt], fm.[CreatedBy], fm.[ModifiedAt], fm.[ModifiedBy], NULL, NULL,
                    fm.[RelationshipType], COALESCE(fm.[ParentFamilyMemberId], fm.[RelatedPersonId])
                FROM [FamilyMembers] fm
                WHERE fm.[IsActive] = 1
                  AND NOT EXISTS (SELECT 1 FROM [RelatedPersons] rp WHERE rp.[Id] = fm.[Id]);

                UPDATE [FamilyMembers]
                SET [IsActive] = 0,
                    [ModifiedAt] = SYSUTCDATETIME(),
                    [ModifiedBy] = N'UnifyPhysicalPersonsAndFamilyLinks'
                WHERE [IsActive] = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RelatedPersons_RelatedToPersonId",
                table: "RelatedPersons",
                column: "RelatedToPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_RelatedPersons_RelatedPersons_RelatedToPersonId",
                table: "RelatedPersons",
                column: "RelatedToPersonId",
                principalTable: "RelatedPersons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RelatedPersons_RelatedPersons_RelatedToPersonId",
                table: "RelatedPersons");

            migrationBuilder.Sql(
                """
                UPDATE fm
                SET fm.[IsActive] = 1,
                    fm.[ModifiedAt] = SYSUTCDATETIME(),
                    fm.[ModifiedBy] = N'Rollback-UnifyPhysicalPersons'
                FROM [FamilyMembers] fm
                INNER JOIN [RelatedPersons] rp ON rp.[Id] = fm.[Id]
                WHERE rp.[SpecialRelationBasis] = N'UZA_PORODICA';

                DELETE rp
                FROM [RelatedPersons] rp
                INNER JOIN [FamilyMembers] fm ON fm.[Id] = rp.[Id]
                WHERE rp.[SpecialRelationBasis] = N'UZA_PORODICA';
                """);

            migrationBuilder.DropIndex(
                name: "IX_RelatedPersons_RelatedToPersonId",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "FamilyRelationshipType",
                table: "RelatedPersons");

            migrationBuilder.DropColumn(
                name: "RelatedToPersonId",
                table: "RelatedPersons");
        }
    }
}
