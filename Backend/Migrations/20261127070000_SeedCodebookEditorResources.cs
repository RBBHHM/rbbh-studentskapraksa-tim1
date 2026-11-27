using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261127070000_SeedCodebookEditorResources")]
public sealed class SeedCodebookEditorResources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @packs bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS');

            INSERT dbo.CODELIST_VALUES (DEFINITION_ID, ID1, ID12, ID13, ID14, ID15, ID16, ID17, CREATED_BY, CREATED_AT)
            SELECT @packs, 1, seed.LanguageCode, seed.ResourceKey, N'Codebooks', N'', seed.Label, N'', 'migration', SYSUTCDATETIME()
            FROM (VALUES
                (N'BS', N'codes.activate-category', N'Aktiviraj šifrarnik'),
                (N'BS', N'codes.deactivate-category', N'Deaktiviraj šifrarnik'),
                (N'BS', N'codes.status-protected', N'Obavezni sistemski šifrarnik mora ostati aktivan.'),
                (N'BS', N'codes.type-text-limit', N'Do {0} znakova'),
                (N'BS', N'codes.type-integer-limit', N'Cijeli brojevi od −2.147.483.648 do 2.147.483.647'),
                (N'BS', N'codes.type-decimal-limit', N'Do {0} cijelih i {1} decimalnih cifara'),
                (N'BS', N'codes.type-date-limit', N'Kalendar bez vremena · GGGG-MM-DD'),
                (N'BS', N'codes.type-datetime-limit', N'Datum i vrijeme · GGGG-MM-DD HH:mm:ss'),
                (N'BS', N'codes.type-boolean-limit', N'Da ili ne · true/false ili 1/0'),
                (N'BS', N'codes.type-active-limit', N'Da ili ne · određuje status reda'),
                (N'BS', N'codes.schema-name-limit', N'Naziv kolone · do 128 znakova'),
                (N'BS', N'codes.schema-description-limit', N'Opis kolone · do 4000 znakova'),
                (N'BS', N'codes.schema-add-column', N'Dodaj kolonu'),
                (N'BS', N'codes.schema-move-up', N'Pomjeri kolonu gore'),
                (N'BS', N'codes.schema-move-down', N'Pomjeri kolonu dolje'),
                (N'BS', N'codes.schema-remove-column', N'Ukloni kolonu'),
                (N'BS', N'codes.schema-empty', N'Dodajte barem jednu kolonu klikom na tip podatka.'),
                (N'BS', N'codes.schema-name-invalid', N'Unesite jedinstven naziv za svaku kolonu. Nazivi ne smiju početi znakovima =, +, - ili @.'),
                (N'BS', N'codes.value-code-used', N'Ovaj kod već postoji u šifrarniku. Unesite drugi kod.'),
                (N'BS', N'codes.value-inactive-category', N'Šifrarnik je neaktivan. U njega se ne mogu dodavati vrijednosti.'),
                (N'BS', N'codes.value-in-use', N'Vrijednost je u upotrebi i ne može se deaktivirati.'),
                (N'BS', N'codes.csv.preview-title', N'Pregled CSV uvoza'),
                (N'BS', N'codes.csv.confirm-valid', N'Uvezi ispravne redove'),
                (N'BS', N'codes.csv.partial-help', N'Uvest će se samo ispravni redovi. Ispravite označene greške u CSV-u pa ponovo učitajte fajl.'),
                (N'BS', N'codes.csv.partial-done', N'Uvezeno: {0}. Već postoji: {1}. Preskočeni su redovi s greškama.'),
                (N'BS', N'codes.csv.invalid-rows', N'Redovi s greškama: {0}.'),
                (N'EN', N'codes.activate-category', N'Activate codebook'),
                (N'EN', N'codes.deactivate-category', N'Deactivate codebook'),
                (N'EN', N'codes.status-protected', N'A required system codebook must remain active.'),
                (N'EN', N'codes.type-text-limit', N'Up to {0} characters'),
                (N'EN', N'codes.type-integer-limit', N'Whole numbers from −2,147,483,648 to 2,147,483,647'),
                (N'EN', N'codes.type-decimal-limit', N'Up to {0} integer digits and {1} decimal places'),
                (N'EN', N'codes.type-date-limit', N'Calendar date · YYYY-MM-DD'),
                (N'EN', N'codes.type-datetime-limit', N'Date and time · YYYY-MM-DD HH:mm:ss'),
                (N'EN', N'codes.type-boolean-limit', N'Yes or no · true/false or 1/0'),
                (N'EN', N'codes.type-active-limit', N'Yes or no · controls the row status'),
                (N'EN', N'codes.schema-name-limit', N'Column name · up to 128 characters'),
                (N'EN', N'codes.schema-description-limit', N'Column description · up to 4000 characters'),
                (N'EN', N'codes.schema-add-column', N'Add a column'),
                (N'EN', N'codes.schema-move-up', N'Move column up'),
                (N'EN', N'codes.schema-move-down', N'Move column down'),
                (N'EN', N'codes.schema-remove-column', N'Remove column'),
                (N'EN', N'codes.schema-empty', N'Add at least one column by choosing a data type.'),
                (N'EN', N'codes.schema-name-invalid', N'Enter a unique name for each column. Names cannot start with =, +, - or @.'),
                (N'EN', N'codes.value-code-used', N'This code already exists in this codebook. Enter another code.'),
                (N'EN', N'codes.value-inactive-category', N'This codebook is inactive. Values cannot be added to it.'),
                (N'EN', N'codes.value-in-use', N'This value is in use and cannot be deactivated.'),
                (N'EN', N'codes.csv.preview-title', N'Review CSV import'),
                (N'EN', N'codes.csv.confirm-valid', N'Import valid rows'),
                (N'EN', N'codes.csv.partial-help', N'Only valid rows will be imported. Correct the highlighted errors in the CSV and upload it again.'),
                (N'EN', N'codes.csv.partial-done', N'Imported: {0}. Already exists: {1}. Rows with errors were skipped.'),
                (N'EN', N'codes.csv.invalid-rows', N'Rows with errors: {0}.'),
                (N'DE', N'codes.activate-category', N'Schlüsselliste aktivieren'),
                (N'DE', N'codes.deactivate-category', N'Schlüsselliste deaktivieren'),
                (N'DE', N'codes.status-protected', N'Eine erforderliche Systemliste muss aktiv bleiben.'),
                (N'DE', N'codes.type-text-limit', N'Bis zu {0} Zeichen'),
                (N'DE', N'codes.type-integer-limit', N'Ganze Zahlen von −2.147.483.648 bis 2.147.483.647'),
                (N'DE', N'codes.type-decimal-limit', N'Bis zu {0} Vorkommastellen und {1} Nachkommastellen'),
                (N'DE', N'codes.type-date-limit', N'Kalenderdatum · JJJJ-MM-TT'),
                (N'DE', N'codes.type-datetime-limit', N'Datum und Uhrzeit · JJJJ-MM-TT HH:mm:ss'),
                (N'DE', N'codes.type-boolean-limit', N'Ja oder Nein · true/false oder 1/0'),
                (N'DE', N'codes.type-active-limit', N'Ja oder Nein · steuert den Zeilenstatus'),
                (N'DE', N'codes.schema-name-limit', N'Spaltenname · bis zu 128 Zeichen'),
                (N'DE', N'codes.schema-description-limit', N'Spaltenbeschreibung · bis zu 4000 Zeichen'),
                (N'DE', N'codes.schema-add-column', N'Spalte hinzufügen'),
                (N'DE', N'codes.schema-move-up', N'Spalte nach oben verschieben'),
                (N'DE', N'codes.schema-move-down', N'Spalte nach unten verschieben'),
                (N'DE', N'codes.schema-remove-column', N'Spalte entfernen'),
                (N'DE', N'codes.schema-empty', N'Wählen Sie einen Datentyp aus, um mindestens eine Spalte hinzuzufügen.'),
                (N'DE', N'codes.schema-name-invalid', N'Geben Sie für jede Spalte einen eindeutigen Namen ein. Namen dürfen nicht mit =, +, - oder @ beginnen.'),
                (N'DE', N'codes.value-code-used', N'Dieser Code ist bereits in der Schlüsselliste vorhanden. Geben Sie einen anderen Code ein.'),
                (N'DE', N'codes.value-inactive-category', N'Diese Schlüsselliste ist inaktiv. Es können keine Werte hinzugefügt werden.'),
                (N'DE', N'codes.value-in-use', N'Dieser Wert wird verwendet und kann nicht deaktiviert werden.'),
                (N'DE', N'codes.csv.preview-title', N'CSV-Import prüfen'),
                (N'DE', N'codes.csv.confirm-valid', N'Gültige Zeilen importieren'),
                (N'DE', N'codes.csv.partial-help', N'Nur gültige Zeilen werden importiert. Korrigieren Sie die angezeigten Fehler in der CSV-Datei und laden Sie sie erneut hoch.'),
                (N'DE', N'codes.csv.partial-done', N'Importiert: {0}. Bereits vorhanden: {1}. Fehlerhafte Zeilen wurden übersprungen.'),
                (N'DE', N'codes.csv.invalid-rows', N'Fehlerhafte Zeilen: {0}.')
            ) AS seed(LanguageCode, ResourceKey, Label)
            WHERE @packs IS NOT NULL AND NOT EXISTS (
                SELECT 1 FROM dbo.CODELIST_VALUES existing
                WHERE existing.DEFINITION_ID = @packs AND existing.ID12 = seed.LanguageCode
                    AND existing.ID13 = seed.ResourceKey
            );

            IF @@ROWCOUNT > 0
                UPDATE dbo.CODELIST_DEFINITIONS SET MODIFIED_AT = SYSUTCDATETIME() WHERE ID = @packs;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve translations because administrators may have edited them after the upgrade.
    }
}
