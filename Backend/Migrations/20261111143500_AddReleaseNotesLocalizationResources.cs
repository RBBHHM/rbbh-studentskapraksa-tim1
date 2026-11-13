using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

#nullable disable

namespace RBBH.ConnectedParties.Migrations;

/// <summary>
/// Add new safe release-notes error messages to all runtime language packs.
/// Preserve owner-edited translations; the migration only inserts missing keys.
/// </summary>
[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261111143500_AddReleaseNotesLocalizationResources")]
public sealed class AddReleaseNotesLocalizationResources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @definition uniqueidentifier =
                (SELECT Id FROM CodeListDefinitions WHERE Name = N'LanguagePacks');
            IF @definition IS NULL
                THROW 51011, 'Runtime language-pack definition must be provisioned before resources.', 1;

            INSERT INTO CodeLists
                (Kategorija, Kod, Naziv, Aktivan, KreiranDatum, KreiraoKorisnik,
                 DefinitionId, IsSystemRequired, LanguageCode, ResourceKey, ResourceLabel)
            SELECT N'LanguagePacks', REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''),
                resource.ResourceKey, 1, SYSUTCDATETIME(), N'migration',
                @definition, 1, resource.LanguageCode, resource.ResourceKey, resource.Label
            FROM (VALUES
                (N'BS', N'errors.release.notes.unavailable',
                 N'Podaci o GitHub izdanjima trenutno nisu dostupni. Pokušajte ponovo.'),
                (N'EN', N'errors.release.notes.unavailable',
                 N'GitHub release notes are temporarily unavailable. Please try again.'),
                (N'DE', N'errors.release.notes.unavailable',
                 N'GitHub-Versionshinweise sind derzeit nicht verfügbar. Bitte versuchen Sie es erneut.'),
                (N'BS', N'errors.release.notes.invalid.environment',
                 N'Odabrano okruženje za prikaz izdanja nije ispravno.'),
                (N'EN', N'errors.release.notes.invalid.environment',
                 N'The selected release environment is invalid.'),
                (N'DE', N'errors.release.notes.invalid.environment',
                 N'Die ausgewählte Veröffentlichungsumgebung ist ungültig.')
            ) resource(LanguageCode, ResourceKey, Label)
            WHERE NOT EXISTS (
                SELECT 1 FROM CodeLists existing
                WHERE existing.Kategorija = N'LanguagePacks'
                  AND existing.LanguageCode = resource.LanguageCode
                  AND existing.ResourceKey = resource.ResourceKey
            );
            UPDATE CodeListDefinitions
            SET Version = Version + 1, ModifiedAt = SYSUTCDATETIME(),
                ModifiedBy = N'migration'
            WHERE Id = @definition;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Runtime resource labels are managed business data. Do not destroy edits
        // when rolling back the application binary.
    }
}
