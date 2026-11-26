using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261126201000_EnableCustomCodebookColumns")]
public sealed class EnableCustomCodebookColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("USES_CUSTOM_COLUMNS", "CODELIST_DEFINITIONS", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.Sql("""
            DECLARE @definition bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS');
            INSERT dbo.CODELIST_VALUES (DEFINITION_ID, CATEGORY, VALUE_KEY, NAME, IS_SYSTEM_REQUIRED,
                ID1, ID12, ID13, ID14, ID16, CREATED_BY, CREATED_AT)
            SELECT @definition, 'LANGUAGE_PACKS', REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), copy.ResourceKey, 1,
                1, copy.LanguageCode, copy.ResourceKey, N'Codebooks', copy.Label, 'migration', SYSUTCDATETIME()
            FROM (VALUES
                (N'BS', N'rolecatalog.details', N'Detalji'),
                (N'BS', N'rolecatalog.permission-module', N'Grupa dozvola'),
                (N'EN', N'rolecatalog.details', N'Details'),
                (N'EN', N'rolecatalog.permission-module', N'Permission group'),
                (N'DE', N'rolecatalog.details', N'Details'),
                (N'DE', N'rolecatalog.permission-module', N'Berechtigungsgruppe'),
                (N'BS', N'codes.edit-schema', N'Uredi kolone'),
                (N'BS', N'codes.storage-column', N'Kolona i tip podataka'),
                (N'BS', N'codes.column-enabled', N'Uključi kolonu'),
                (N'BS', N'codes.column-name', N'Naziv kolone'),
                (N'BS', N'codes.column-position', N'Redoslijed (1–20)'),
                (N'BS', N'codes.schema-invalid', N'Dodajte barem jednu kolonu. Svaka kolona treba jedinstven naziv i redoslijed od 1 do 20.'),
                (N'BS', N'codes.schema-protected', N'Kolone šifrarnika s vrijednostima možete preimenovati ili premjestiti. Brisanje kolona je blokirano radi očuvanja podataka.'),
                (N'BS', N'codes.concurrent', N'Šifrarnik je promijenjen. Zatvorite formu, osvježite šifrarnik i pokušajte ponovo.'),
                (N'BS', N'codes.cell-invalid', N'Provjerite tip, format i dužinu unesenih vrijednosti.'),
                (N'BS', N'codes.csv.row-duplicate', N'Red {0} ponavlja prethodnu vrijednost.'),
                (N'BS', N'codes.cell-required', N'Popunite barem jednu kolonu.'),
                (N'BS', N'codes.value-uncertain', N'Spremanje nije potvrđeno. Osvježite šifrarnik i provjerite vrijednost prije ponovnog pokušaja.'),
                (N'EN', N'codes.edit-schema', N'Edit columns'),
                (N'EN', N'codes.storage-column', N'Column and data type'),
                (N'EN', N'codes.column-enabled', N'Include column'),
                (N'EN', N'codes.column-name', N'Column name'),
                (N'EN', N'codes.column-position', N'Position (1–20)'),
                (N'EN', N'codes.schema-invalid', N'Add at least one column. Each column needs a unique name and position from 1 to 20.'),
                (N'EN', N'codes.schema-protected', N'Columns in a codebook with values can be renamed or reordered. Removing columns is blocked to preserve data.'),
                (N'EN', N'codes.concurrent', N'The codebook has changed. Close the form, refresh the codebook and try again.'),
                (N'EN', N'codes.cell-invalid', N'Check the type, format and length of the entered values.'),
                (N'EN', N'codes.csv.row-duplicate', N'Row {0} duplicates a previous value.'),
                (N'EN', N'codes.cell-required', N'Enter a value in at least one column.'),
                (N'EN', N'codes.value-uncertain', N'Saving could not be confirmed. Refresh the codebook and check the value before retrying.'),
                (N'DE', N'codes.edit-schema', N'Spalten bearbeiten'),
                (N'DE', N'codes.storage-column', N'Spalte und Datentyp'),
                (N'DE', N'codes.column-enabled', N'Spalte aufnehmen'),
                (N'DE', N'codes.column-name', N'Spaltenname'),
                (N'DE', N'codes.column-position', N'Position (1–20)'),
                (N'DE', N'codes.schema-invalid', N'Fügen Sie mindestens eine Spalte hinzu. Jede Spalte benötigt einen eindeutigen Namen und eine Position von 1 bis 20.'),
                (N'DE', N'codes.schema-protected', N'Spalten eines Katalogs mit Werten können umbenannt oder neu angeordnet werden. Das Entfernen von Spalten ist zum Schutz der Daten gesperrt.'),
                (N'DE', N'codes.concurrent', N'Der Katalog wurde geändert. Schließen Sie das Formular, laden Sie den Katalog neu und versuchen Sie es erneut.'),
                (N'DE', N'codes.cell-invalid', N'Prüfen Sie Typ, Format und Länge der eingegebenen Werte.'),
                (N'DE', N'codes.csv.row-duplicate', N'Zeile {0} wiederholt einen früheren Wert.'),
                (N'DE', N'codes.cell-required', N'Füllen Sie mindestens eine Spalte aus.'),
                (N'DE', N'codes.value-uncertain', N'Das Speichern konnte nicht bestätigt werden. Aktualisieren Sie den Katalog und prüfen Sie den Wert vor einem neuen Versuch.')
            ) AS copy(LanguageCode, ResourceKey, Label)
            WHERE NOT EXISTS (SELECT 1 FROM dbo.CODELIST_VALUES AS existing
                WHERE existing.CATEGORY = 'LANGUAGE_PACKS' AND existing.ID12 = copy.LanguageCode COLLATE Latin1_General_100_CI_AS
                AND existing.ID13 = copy.ResourceKey COLLATE Latin1_General_100_CI_AS);
            IF @@ROWCOUNT > 0
                UPDATE dbo.CODELIST_DEFINITIONS SET VERSION = VERSION + 1 WHERE ID = @definition;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Custom codebook definitions require their original column settings.");
}
