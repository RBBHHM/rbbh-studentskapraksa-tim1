using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261126154500_CanonicalCodebooksAndCsvResources")]
public sealed class CanonicalCodebooksAndCsvResources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @aliases TABLE (
                OldName varchar(100) COLLATE Latin1_General_100_CI_AS PRIMARY KEY,
                NewName varchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL);
            INSERT @aliases VALUES ('VrstaLimita', 'VRSTE_LIMITA'), ('OsnovPovezanosti', 'OSNOV_POVEZANOSTI'),
                ('OsnovPosebnogOdnosa', 'OSNOV_POSEBNOG_ODNOSA'), ('Srodstvo', 'SRODSTVO'),
                ('TipLica', 'TIP_LICA'), ('TipRola', 'TIP_ROLA'), ('TipDokumenta', 'TIP_DOKUMENTA'), ('Status', 'STATUS');

            DECLARE @names TABLE (
                OldName varchar(100) COLLATE Latin1_General_100_CI_AS PRIMARY KEY,
                CanonicalName varchar(200) COLLATE Latin1_General_100_CI_AS NOT NULL,
                FinalName varchar(100) COLLATE Latin1_General_100_CI_AS NULL);
            INSERT @names (OldName, CanonicalName)
            SELECT source.Name, COALESCE(alias.NewName, '') FROM
            (
                SELECT NAME AS Name FROM dbo.CODELIST_DEFINITIONS
                UNION
                SELECT value.CATEGORY FROM dbo.CODELIST_VALUES AS value
                WHERE NOT EXISTS (SELECT 1 FROM dbo.CODELIST_DEFINITIONS WHERE NAME = value.CATEGORY)
            ) AS source
            LEFT JOIN @aliases AS alias ON alias.OldName = source.Name;

            DECLARE @position int = 1;
            WHILE @position <= 100
            BEGIN
                UPDATE names
                SET CanonicalName = CanonicalName +
                    CASE WHEN token.[Character] COLLATE Latin1_General_100_BIN2 LIKE '[a-zA-Z0-9]'
                    THEN
                        CASE WHEN RIGHT(CanonicalName, 1) <> '_' AND token.[Character] COLLATE Latin1_General_100_BIN2 LIKE '[A-Z]'
                          AND (token.[Previous] COLLATE Latin1_General_100_BIN2 LIKE '[a-z0-9]'
                            OR (token.[Previous] COLLATE Latin1_General_100_BIN2 LIKE '[A-Z]' AND token.[Following] COLLATE Latin1_General_100_BIN2 LIKE '[a-z]'))
                          THEN '_' ELSE '' END + UPPER(token.[Character] COLLATE Latin1_General_100_CI_AS)
                    WHEN CanonicalName <> '' AND RIGHT(CanonicalName, 1) <> '_' THEN '_'
                    ELSE '' END
                FROM @names AS names
                CROSS APPLY (SELECT SUBSTRING(names.OldName, @position, 1) AS [Character],
                                    SUBSTRING(names.OldName, @position - 1, 1) AS [Previous],
                                    SUBSTRING(names.OldName, @position + 1, 1) AS [Following]) AS token
                WHERE @position <= LEN(names.OldName)
                    AND NOT EXISTS (SELECT 1 FROM @aliases WHERE OldName = names.OldName);
                SET @position += 1;
            END;
            UPDATE @names SET CanonicalName = LEFT(CanonicalName, LEN(CanonicalName) - 1) WHERE RIGHT(CanonicalName, 1) = '_';
            IF EXISTS (SELECT 1 FROM @names WHERE LEN(CanonicalName) NOT BETWEEN 1 AND 100)
                THROW 51011, 'A codebook name cannot be normalized to the supported length.', 1;

            -- Prefer a definition already using the canonical name. Keep other codebooks
            -- and their values under unique names instead of merging unrelated definitions.
            ;WITH ranked AS
            (
                SELECT names.OldName, ROW_NUMBER() OVER (PARTITION BY names.CanonicalName ORDER BY
                    CASE WHEN names.OldName = names.CanonicalName THEN 0
                         WHEN alias.OldName IS NOT NULL THEN 1 ELSE 2 END, names.OldName) AS Priority
                FROM @names AS names LEFT JOIN @aliases AS alias ON alias.OldName = names.OldName
            )
            UPDATE names SET FinalName = names.CanonicalName
            FROM @names AS names JOIN ranked ON ranked.OldName = names.OldName
            WHERE ranked.Priority = 1;

            DECLARE @oldName varchar(100), @canonical varchar(100), @finalName varchar(100), @suffix varchar(12), @next int;
            WHILE EXISTS (SELECT 1 FROM @names WHERE FinalName IS NULL)
            BEGIN
                SELECT TOP (1) @oldName = OldName, @canonical = CanonicalName
                FROM @names WHERE FinalName IS NULL ORDER BY CanonicalName, OldName;
                SET @next = 1;
                WHILE 1 = 1
                BEGIN
                    SET @next += 1;
                    SET @suffix = CONCAT('_', @next);
                    SET @finalName = LEFT(@canonical, 100 - LEN(@suffix)) + @suffix;
                    IF NOT EXISTS (SELECT 1 FROM @names WHERE @finalName IN (OldName, FinalName))
                        BREAK;
                END;
                UPDATE @names SET FinalName = @finalName WHERE OldName = @oldName;
            END;

            UPDATE definition SET NAME = names.FinalName, VERSION = VERSION + 1
            FROM dbo.CODELIST_DEFINITIONS AS definition JOIN @names AS names ON names.OldName = definition.NAME
            WHERE definition.NAME COLLATE Latin1_General_100_BIN2 <> names.FinalName COLLATE Latin1_General_100_BIN2;
            UPDATE value SET CATEGORY = names.FinalName
            FROM dbo.CODELIST_VALUES AS value JOIN @names AS names ON names.OldName = value.CATEGORY
            WHERE value.CATEGORY COLLATE Latin1_General_100_BIN2 <> names.FinalName COLLATE Latin1_General_100_BIN2;

            IF NOT EXISTS (SELECT 1 FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'VRSTE_LIMITA')
                INSERT dbo.CODELIST_DEFINITIONS (NAME, IS_ACTIVE, IS_SYSTEM_REQUIRED, VERSION, CREATED_BY, CREATED_AT)
                VALUES ('VRSTE_LIMITA', 1, 1, 1, N'migration', SYSUTCDATETIME());

            DECLARE @limitTypes bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'VRSTE_LIMITA');
            UPDATE dbo.CODELIST_VALUES SET DEFINITION_ID = @limitTypes WHERE CATEGORY = 'VRSTE_LIMITA' AND DEFINITION_ID IS NULL;
            -- Keep existing canonical values on duplicate keys, and preserve the legacy rows
            -- with conflicting keys in their renamed codebook.
            UPDATE legacy SET CATEGORY = 'VRSTE_LIMITA', DEFINITION_ID = @limitTypes
            FROM dbo.CODELIST_VALUES AS legacy
            JOIN @names AS names ON names.FinalName = legacy.CATEGORY
            WHERE names.OldName = 'VrstaLimita' AND names.FinalName <> 'VRSTE_LIMITA'
                AND NOT EXISTS (SELECT 1 FROM dbo.CODELIST_VALUES AS canonical
                    WHERE canonical.CATEGORY = 'VRSTE_LIMITA' AND canonical.VALUE_KEY = legacy.VALUE_KEY);
            INSERT dbo.CODELIST_VALUES (DEFINITION_ID, CATEGORY, VALUE_KEY, NAME, IS_SYSTEM_REQUIRED, ID1, CREATED_BY, CREATED_AT)
            SELECT @limitTypes, 'VRSTE_LIMITA', types.Code, types.Label, 0, 1, 'migration', SYSUTCDATETIME()
            FROM (VALUES ('MM', N'MM'), ('SATAFXS', N'SATA FX spot'), ('SATAFXD', N'SATA FX derivati'),
                ('SATAOVL', N'SATA overnight limit'), ('OVL_CUSTODY', N'OVL Custody'), ('SFL_NET', N'SFL Net'),
                ('LG', N'LG'), ('LT', N'LT'), ('ST', N'ST'), ('SPRL', N'SPRL'), ('LEASING', N'Leasing')) AS types(Code, Label)
            WHERE NOT EXISTS (SELECT 1 FROM dbo.CODELIST_VALUES WHERE CATEGORY = 'VRSTE_LIMITA' AND VALUE_KEY = types.Code COLLATE Latin1_General_100_CI_AS);
            """);

        migrationBuilder.Sql("""
            DECLARE @packs bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS');
            DELETE FROM dbo.CODELIST_VALUES WHERE CATEGORY = 'LANGUAGE_PACKS'
                AND ID13 IN (N'capital.asof-date', N'capital.asof-show', N'capital.recorded-at');
            UPDATE dbo.CODELIST_DEFINITIONS SET VERSION = VERSION + 1 WHERE ID = @packs;
            INSERT dbo.CODELIST_VALUES (DEFINITION_ID, CATEGORY, VALUE_KEY, NAME, IS_SYSTEM_REQUIRED,
                ID1, ID12, ID13, ID14, ID16, CREATED_BY, CREATED_AT)
            SELECT @packs, 'LANGUAGE_PACKS', REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), copy.ResourceKey, 1,
                1, copy.LanguageCode, copy.ResourceKey, N'Codebooks', copy.Label, 'migration', SYSUTCDATETIME()
            FROM (VALUES
                (N'BS', N'codes.csv.type-text', N'Tekst'),
                (N'BS', N'codes.csv.type-boolean', N'Da/ne'),
                (N'BS', N'codes.csv.type-integer', N'Cijeli broj'),
                (N'BS', N'codes.csv.type-decimal', N'Decimalni broj'),
                (N'BS', N'codes.csv.type-date', N'Datum'),
                (N'BS', N'codes.csv.type-datetime', N'Datum i vrijeme'),
                (N'BS', N'codes.csv.template', N'Preuzmi CSV predložak'),
                (N'BS', N'codes.csv.import', N'Import CSV vrijednosti'),
                (N'BS', N'codes.csv.confirm', N'Uvezi vrijednosti'),
                (N'BS', N'codes.csv.summary', N'Redova: {0} · Spremno za uvoz: {1} · Već postoji: {2}'),
                (N'BS', N'codes.csv.done', N'Uvezeno: {0}. Preskočeno postojećih: {1}.'),
                (N'BS', N'codes.csv.file', N'Odaberite nepraznu .csv datoteku do 10 MB.'),
                (N'BS', N'codes.csv.encoding', N'Datoteka mora biti UTF-8 CSV bez kontrolnih znakova.'),
                (N'BS', N'codes.csv.schema', N'Definicija šifrarnika sadrži neispravne ili ponovljene nazive kolona.'),
                (N'BS', N'codes.csv.syntax', N'Red {0}: provjerite navodnike i razdvajanje polja.'),
                (N'BS', N'codes.csv.header-duplicate', N'Kolona „{0}” ponavlja se u zaglavlju.'),
                (N'BS', N'codes.csv.header-extra', N'Zaglavlje sadrži neočekivanu kolonu „{0}”.'),
                (N'BS', N'codes.csv.header-missing', N'U zaglavlju nedostaje kolona „{0}”.'),
                (N'BS', N'codes.csv.cells', N'Red {0}: broj vrijednosti se ne podudara sa zaglavljem.'),
                (N'BS', N'codes.csv.required', N'Red {0}, kolona „{1}”: vrijednost je obavezna. Primjer: {2}.'),
                (N'BS', N'codes.csv.invalid', N'Red {0}, kolona „{1}”: neispravan format ili dužina. Primjer: {2}.'),
                (N'BS', N'codes.csv.language', N'Red {0}: jezik „{1}” mora biti aktivan u šifrarniku LANGUAGES.'),
                (N'BS', N'codes.csv.resource', N'Red {0}: ključ resursa mora biti u formatu poput „example.field”.'),
                (N'BS', N'codes.csv.file-duplicate', N'Red {0}: ključ „{1}” ponavlja se u datoteci.'),
                (N'BS', N'codes.csv.limit', N'CSV smije imati najviše 5.000 redova vrijednosti.'),
                (N'BS', N'codes.csv.data', N'Dodajte barem jedan red vrijednosti ispod zaglavlja.'),
                (N'BS', N'codes.csv.example-skipped', N'Primjer iz predloška je preskočen.'),
                (N'EN', N'codes.csv.type-text', N'Text'),
                (N'EN', N'codes.csv.type-boolean', N'Boolean'),
                (N'EN', N'codes.csv.type-integer', N'Integer'),
                (N'EN', N'codes.csv.type-decimal', N'Decimal'),
                (N'EN', N'codes.csv.type-date', N'Date'),
                (N'EN', N'codes.csv.type-datetime', N'Date and time'),
                (N'EN', N'codes.csv.template', N'Download CSV template'),
                (N'EN', N'codes.csv.import', N'Import CSV values'),
                (N'EN', N'codes.csv.confirm', N'Import values'),
                (N'EN', N'codes.csv.summary', N'Rows: {0} · Ready to import: {1} · Already exists: {2}'),
                (N'EN', N'codes.csv.done', N'Imported: {0}. Existing rows skipped: {1}.'),
                (N'EN', N'codes.csv.file', N'Choose a non-empty .csv file up to 10 MB.'),
                (N'EN', N'codes.csv.encoding', N'Use a UTF-8 CSV file without control characters.'),
                (N'EN', N'codes.csv.schema', N'The codebook definition contains invalid or duplicate column names.'),
                (N'EN', N'codes.csv.syntax', N'Row {0}: check the quotes and field delimiters.'),
                (N'EN', N'codes.csv.header-duplicate', N'Column “{0}” appears more than once in the header.'),
                (N'EN', N'codes.csv.header-extra', N'The header contains an unexpected column “{0}”.'),
                (N'EN', N'codes.csv.header-missing', N'Column “{0}” is missing from the header.'),
                (N'EN', N'codes.csv.cells', N'Row {0}: the number of values does not match the header.'),
                (N'EN', N'codes.csv.required', N'Row {0}, column “{1}”: a value is required. Example: {2}.'),
                (N'EN', N'codes.csv.invalid', N'Row {0}, column “{1}”: invalid format or length. Example: {2}.'),
                (N'EN', N'codes.csv.language', N'Row {0}: language “{1}” must be active in LANGUAGES.'),
                (N'EN', N'codes.csv.resource', N'Row {0}: use a resource key such as “example.field”.'),
                (N'EN', N'codes.csv.file-duplicate', N'Row {0}: key “{1}” occurs more than once in the file.'),
                (N'EN', N'codes.csv.limit', N'A CSV may contain up to 5,000 data rows.'),
                (N'EN', N'codes.csv.data', N'Add at least one data row below the header.'),
                (N'EN', N'codes.csv.example-skipped', N'The template example was skipped.'),
                (N'DE', N'codes.csv.type-text', N'Text'),
                (N'DE', N'codes.csv.type-boolean', N'Ja/Nein'),
                (N'DE', N'codes.csv.type-integer', N'Ganzzahl'),
                (N'DE', N'codes.csv.type-decimal', N'Dezimalzahl'),
                (N'DE', N'codes.csv.type-date', N'Datum'),
                (N'DE', N'codes.csv.type-datetime', N'Datum und Uhrzeit'),
                (N'DE', N'codes.csv.template', N'CSV-Vorlage herunterladen'),
                (N'DE', N'codes.csv.import', N'CSV-Werte importieren'),
                (N'DE', N'codes.csv.confirm', N'Werte importieren'),
                (N'DE', N'codes.csv.summary', N'Zeilen: {0} · Bereit zum Import: {1} · Bereits vorhanden: {2}'),
                (N'DE', N'codes.csv.done', N'Importiert: {0}. Vorhandene Zeilen übersprungen: {1}.'),
                (N'DE', N'codes.csv.file', N'Wählen Sie eine nicht leere .csv-Datei bis 10 MB.'),
                (N'DE', N'codes.csv.encoding', N'Verwenden Sie eine UTF-8-CSV-Datei ohne Steuerzeichen.'),
                (N'DE', N'codes.csv.schema', N'Die Katalogdefinition enthält ungültige oder doppelte Spaltennamen.'),
                (N'DE', N'codes.csv.syntax', N'Zeile {0}: Prüfen Sie Anführungszeichen und Feldtrennzeichen.'),
                (N'DE', N'codes.csv.header-duplicate', N'Spalte „{0}“ kommt im Kopf mehrfach vor.'),
                (N'DE', N'codes.csv.header-extra', N'Der Kopf enthält die unerwartete Spalte „{0}“.'),
                (N'DE', N'codes.csv.header-missing', N'Spalte „{0}“ fehlt im Kopf.'),
                (N'DE', N'codes.csv.cells', N'Zeile {0}: Die Anzahl der Werte entspricht nicht dem Kopf.'),
                (N'DE', N'codes.csv.required', N'Zeile {0}, Spalte „{1}“: Ein Wert ist erforderlich. Beispiel: {2}.'),
                (N'DE', N'codes.csv.invalid', N'Zeile {0}, Spalte „{1}“: Ungültiges Format oder ungültige Länge. Beispiel: {2}.'),
                (N'DE', N'codes.csv.language', N'Zeile {0}: Die Sprache „{1}“ muss in LANGUAGES aktiv sein.'),
                (N'DE', N'codes.csv.resource', N'Zeile {0}: Verwenden Sie einen Ressourcenschlüssel wie „example.field“.'),
                (N'DE', N'codes.csv.file-duplicate', N'Zeile {0}: Schlüssel „{1}“ kommt in der Datei mehrfach vor.'),
                (N'DE', N'codes.csv.limit', N'Eine CSV-Datei darf bis zu 5.000 Datenzeilen enthalten.'),
                (N'DE', N'codes.csv.data', N'Fügen Sie mindestens eine Datenzeile unter dem Kopf hinzu.'),
                (N'DE', N'codes.csv.example-skipped', N'Die Beispielzeile der Vorlage wurde übersprungen.')
            ) AS copy(LanguageCode, ResourceKey, Label)
            WHERE NOT EXISTS (SELECT 1 FROM dbo.CODELIST_VALUES AS existing
                WHERE existing.CATEGORY = 'LANGUAGE_PACKS' AND existing.ID12 = copy.LanguageCode COLLATE Latin1_General_100_CI_AS
                AND existing.ID13 = copy.ResourceKey COLLATE Latin1_General_100_CI_AS);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore a pre-migration database backup to recover the previous codebook names and resources.");
}
