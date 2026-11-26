using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261126183000_RemoveAuditRecordReferencesAndLocalizeCodebooks")]
public sealed class RemoveAuditRecordReferencesAndLocalizeCodebooks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE audit
            SET OLD_VALUES = CASE WHEN beforeJson.Cleaned IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM OPENJSON(beforeJson.Cleaned) WHERE [key] <> 'value' OR [type] <> 0)
                    THEN NULL ELSE COALESCE(beforeJson.Cleaned, audit.OLD_VALUES) END,
                NEW_VALUES = CASE WHEN afterJson.Cleaned IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM OPENJSON(afterJson.Cleaned) WHERE [key] <> 'value' OR [type] <> 0)
                    THEN NULL ELSE COALESCE(afterJson.Cleaned, audit.NEW_VALUES) END
            FROM dbo.AUDIT_LOGS AS audit
            CROSS APPLY (VALUES (CASE WHEN ISJSON(audit.OLD_VALUES) = 1 THEN audit.OLD_VALUES END,
                                 CASE WHEN ISJSON(audit.NEW_VALUES) = 1 THEN audit.NEW_VALUES END))
                AS snapshots(BeforeJson, AfterJson)
            CROSS APPLY (VALUES (CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(snapshots.BeforeJson) WHERE [key] = '_recordId')
                THEN JSON_MODIFY(snapshots.BeforeJson, '$._recordId', NULL) END)) AS beforeJson(Cleaned)
            CROSS APPLY (VALUES (CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(snapshots.AfterJson) WHERE [key] = '_recordId')
                THEN JSON_MODIFY(snapshots.AfterJson, '$._recordId', NULL) END)) AS afterJson(Cleaned)
            WHERE beforeJson.Cleaned IS NOT NULL OR afterJson.Cleaned IS NOT NULL;

            -- Legacy AppUser profile edits serialized all four text fields even when only one changed.
            DECLARE @profileFields TABLE (Path nvarchar(32) PRIMARY KEY);
            INSERT @profileFields VALUES (N'$.Username'), (N'$.FirstName'), (N'$.LastName'), (N'$.Email');
            DECLARE @path nvarchar(32);
            WHILE EXISTS (SELECT 1 FROM @profileFields)
            BEGIN
                SELECT TOP (1) @path = Path FROM @profileFields ORDER BY Path;
                UPDATE audit
                SET OLD_VALUES = JSON_MODIFY(payload.BeforeJson, @path, NULL),
                    NEW_VALUES = JSON_MODIFY(payload.AfterJson, @path, NULL)
                FROM dbo.AUDIT_LOGS AS audit
                CROSS APPLY (VALUES (CASE WHEN ISJSON(audit.OLD_VALUES) = 1 THEN audit.OLD_VALUES END,
                                     CASE WHEN ISJSON(audit.NEW_VALUES) = 1 THEN audit.NEW_VALUES END))
                    AS payload(BeforeJson, AfterJson)
                CROSS APPLY (VALUES (JSON_VALUE(payload.BeforeJson, @path), JSON_VALUE(payload.AfterJson, @path)))
                    AS field(BeforeText, AfterText)
                WHERE audit.TABLE_NAME = 'AppUser' AND audit.ACTION = 'UPDATE'
                    AND field.BeforeText COLLATE Latin1_General_100_BIN2 = field.AfterText COLLATE Latin1_General_100_BIN2
                    AND DATALENGTH(field.BeforeText) = DATALENGTH(field.AfterText);
                DELETE FROM @profileFields WHERE Path = @path;
            END;
            """);

        migrationBuilder.Sql("""
            DECLARE @definition bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS');
            INSERT dbo.CODELIST_VALUES (DEFINITION_ID, CATEGORY, VALUE_KEY, NAME, IS_SYSTEM_REQUIRED,
                ID1, ID12, ID13, ID14, ID16, CREATED_BY, CREATED_AT)
            SELECT @definition, 'LANGUAGE_PACKS', REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), copy.ResourceKey, 1,
                1, copy.LanguageCode, copy.ResourceKey, N'Codebooks', copy.Label, 'migration', SYSUTCDATETIME()
            FROM (VALUES
                (N'BS', N'codes.definition-duplicate', N'Šifrarnik s tim nazivom već postoji.'),
                (N'BS', N'codes.definition-uncertain', N'Kreiranje šifrarnika nije potvrđeno. Osvježite popis i provjerite je li šifrarnik kreiran.'),
                (N'BS', N'codes.language-name', N'Naziv jezika'),
                (N'BS', N'codes.active.column-description', N'Određuje je li zapis dostupan u aplikaciji.'),
                (N'BS', N'codes.language-code.column-description', N'Kratka šifra jezika iz šifrarnika LANGUAGES.'),
                (N'BS', N'codes.language-name.column-description', N'Naziv koji se prikazuje pri odabiru jezika.'),
                (N'BS', N'codes.resource-key.column-description', N'Ključ po kojem aplikacija pronalazi tekst.'),
                (N'BS', N'codes.context-info.column-description', N'Stranica ili mjesto na kojem se tekst koristi.'),
                (N'BS', N'codes.label-text.column-description', N'Oznaka ili poruka koja se prikazuje korisniku.'),
                (N'BS', N'codes.help-text.column-description', N'Opcionalne upute za korisnika.'),
                (N'BS', N'codes.placeholder-text.column-description', N'Opcionalni tekst prikazan u praznom polju.'),
                (N'EN', N'codes.definition-duplicate', N'A codebook with this name already exists.'),
                (N'EN', N'codes.definition-uncertain', N'Creation could not be confirmed. Refresh the list to check whether the codebook was created.'),
                (N'EN', N'codes.language-name', N'Language name'),
                (N'EN', N'codes.active.column-description', N'Determines whether the entry is available in the application.'),
                (N'EN', N'codes.language-code.column-description', N'Short language code from the LANGUAGES codebook.'),
                (N'EN', N'codes.language-name.column-description', N'Name shown in the language picker.'),
                (N'EN', N'codes.resource-key.column-description', N'Key the application uses to look up the text.'),
                (N'EN', N'codes.context-info.column-description', N'Page or location where the text is used.'),
                (N'EN', N'codes.label-text.column-description', N'Label or message displayed to the user.'),
                (N'EN', N'codes.help-text.column-description', N'Optional instructions for the user.'),
                (N'EN', N'codes.placeholder-text.column-description', N'Optional text shown in an empty field.'),
                (N'DE', N'codes.definition-duplicate', N'Ein Katalog mit diesem Namen existiert bereits.'),
                (N'DE', N'codes.definition-uncertain', N'Die Erstellung konnte nicht bestätigt werden. Aktualisieren Sie die Liste, um zu prüfen, ob der Katalog erstellt wurde.'),
                (N'DE', N'codes.language-name', N'Sprachname'),
                (N'DE', N'codes.active.column-description', N'Legt fest, ob der Eintrag in der Anwendung verfügbar ist.'),
                (N'DE', N'codes.language-code.column-description', N'Kurzer Sprachcode aus dem Katalog LANGUAGES.'),
                (N'DE', N'codes.language-name.column-description', N'Name in der Sprachauswahl.'),
                (N'DE', N'codes.resource-key.column-description', N'Schlüssel, über den die Anwendung den Text findet.'),
                (N'DE', N'codes.context-info.column-description', N'Seite oder Stelle, an der der Text verwendet wird.'),
                (N'DE', N'codes.label-text.column-description', N'Bezeichnung oder Meldung, die angezeigt wird.'),
                (N'DE', N'codes.help-text.column-description', N'Optionale Hinweise für Benutzer.'),
                (N'DE', N'codes.placeholder-text.column-description', N'Optionaler Text in einem leeren Feld.')
            ) AS copy(LanguageCode, ResourceKey, Label)
            WHERE NOT EXISTS (SELECT 1 FROM dbo.CODELIST_VALUES AS existing
                WHERE existing.CATEGORY = 'LANGUAGE_PACKS' AND existing.ID12 = copy.LanguageCode COLLATE Latin1_General_100_CI_AS
                AND existing.ID13 = copy.ResourceKey COLLATE Latin1_General_100_CI_AS);
            IF @@ROWCOUNT > 0
                UPDATE dbo.CODELIST_DEFINITIONS SET VERSION = VERSION + 1 WHERE ID = @definition;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("The removed audit metadata cannot be recovered.");
}
