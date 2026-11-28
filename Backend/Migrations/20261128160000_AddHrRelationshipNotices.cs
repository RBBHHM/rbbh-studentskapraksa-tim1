using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261128160000_AddHrRelationshipNotices")]
public sealed class AddHrRelationshipNotices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("HR_EXPIRY_NOTIFIED_FOR", "RELATED_PERSONS", type: "datetime2", nullable: true);
        migrationBuilder.CreateIndex("IX_RELATED_PERSONS_HR_EXPIRY", "RELATED_PERSONS", new[] { "IS_ACTIVE", "DATE_TO" });
        migrationBuilder.Sql("""
            UPDATE dbo.RELATED_PERSONS SET HR_EXPIRY_NOTIFIED_FOR = DATE_TO
            WHERE DATE_TO < CONVERT(date, SYSUTCDATETIME());

            IF NOT EXISTS (SELECT 1 FROM dbo.ROLES WHERE NAME = 'HR')
                INSERT dbo.ROLES (ID, NAME, IS_ACTIVE, CREATED_BY, CREATED_AT)
                VALUES (NEXT VALUE FOR dbo.RolesIdSequence, 'HR', 1, 'migration', SYSUTCDATETIME());

            DELETE grantRow FROM dbo.ROLE_PERMISSIONS AS grantRow
            INNER JOIN dbo.ROLES AS roleRow ON roleRow.ID = grantRow.ROLE_ID WHERE roleRow.NAME = 'HR';

            DECLARE @packs bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS');
            IF @packs IS NULL RETURN;
            DECLARE @labels TABLE (LanguageCode nvarchar(10) COLLATE Latin1_General_100_CI_AS,
                ResourceKey nvarchar(200) COLLATE Latin1_General_100_CI_AS, Label nvarchar(4000));
            INSERT @labels VALUES
            (N'BS', N'rolecatalog.delete-blocked', N'Prebaci korisnike prije brisanja'),
            (N'BS', N'notifications.hr.relationship.added.title', N'Novo povezano fizičko lice'),
            (N'BS', N'notifications.hr.relationship.added.body', N'Fizičko lice: {fullName}' + NCHAR(10) + N'Osnov povezanosti: {basis}' + NCHAR(10) + N'Važi od: {from}' + NCHAR(10) + N'Važi do: {until}'),
            (N'BS', N'notifications.hr.relationship.ended.title', N'Prestao važiti osnov povezanosti'),
            (N'BS', N'notifications.hr.relationship.ended.body', N'Fizičko lice: {fullName}' + NCHAR(10) + N'Osnov povezanosti: {basis}' + NCHAR(10) + N'Datum prestanka: {until}'),
            (N'EN', N'rolecatalog.delete-blocked', N'Move users before deletion'),
            (N'EN', N'notifications.hr.relationship.added.title', N'New related individual'),
            (N'EN', N'notifications.hr.relationship.added.body', N'Individual: {fullName}' + NCHAR(10) + N'Basis of relationship: {basis}' + NCHAR(10) + N'Valid from: {from}' + NCHAR(10) + N'Valid to: {until}'),
            (N'EN', N'notifications.hr.relationship.ended.title', N'Basis of relationship ended'),
            (N'EN', N'notifications.hr.relationship.ended.body', N'Individual: {fullName}' + NCHAR(10) + N'Basis of relationship: {basis}' + NCHAR(10) + N'End date: {until}'),
            (N'DE', N'rolecatalog.delete-blocked', N'Benutzer vor dem Löschen übertragen'),
            (N'DE', N'notifications.hr.relationship.added.title', N'Neue verbundene Person'),
            (N'DE', N'notifications.hr.relationship.added.body', N'Person: {fullName}' + NCHAR(10) + N'Beziehungsgrund: {basis}' + NCHAR(10) + N'Gültig ab: {from}' + NCHAR(10) + N'Gültig bis: {until}'),
            (N'DE', N'notifications.hr.relationship.ended.title', N'Beziehungsgrund abgelaufen'),
            (N'DE', N'notifications.hr.relationship.ended.body', N'Person: {fullName}' + NCHAR(10) + N'Beziehungsgrund: {basis}' + NCHAR(10) + N'Enddatum: {until}'),
            (N'EN', N'users.role.fl', N'Individuals');

            UPDATE entry SET ID16 = seed.Label, MODIFIED_AT = SYSUTCDATETIME(), MODIFIED_BY = 'migration'
            FROM dbo.CODELIST_VALUES AS entry INNER JOIN @labels AS seed
              ON entry.ID12 = seed.LanguageCode AND entry.ID13 = seed.ResourceKey
            WHERE entry.DEFINITION_ID = @packs AND (seed.ResourceKey = 'rolecatalog.delete-blocked'
                OR (seed.ResourceKey = 'users.role.fl' AND entry.ID16 IN (N'Natural persons', N'FL')));

            INSERT dbo.CODELIST_VALUES (DEFINITION_ID, ID1, ID12, ID13, ID14, ID15, ID16, ID17, CREATED_BY, CREATED_AT)
            SELECT @packs, 1, seed.LanguageCode, seed.ResourceKey, N'HR', N'', seed.Label, N'', 'migration', SYSUTCDATETIME()
            FROM @labels AS seed WHERE NOT EXISTS (
                SELECT 1 FROM dbo.CODELIST_VALUES AS entry WHERE entry.DEFINITION_ID = @packs
                AND entry.ID12 = seed.LanguageCode AND entry.ID13 = seed.ResourceKey);
            UPDATE dbo.CODELIST_DEFINITIONS SET MODIFIED_AT = SYSUTCDATETIME() WHERE ID = @packs;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_RELATED_PERSONS_HR_EXPIRY", "RELATED_PERSONS");
        migrationBuilder.DropColumn("HR_EXPIRY_NOTIFIED_FOR", "RELATED_PERSONS");
    }
}
