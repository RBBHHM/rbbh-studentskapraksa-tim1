using Microsoft.EntityFrameworkCore.Migrations;

namespace RBBH.ConnectedParties.Api.Migrations;

public partial class ConsolidateCodebookLanguagePacks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS
            (
                SELECT 1 FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LanguagePacks'
            ) AND EXISTS
            (
                SELECT 1 FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS'
            )
                THROW 51011, 'Both legacy and canonical language pack definitions exist. Reconcile them before upgrading.', 1;

            SELECT * INTO #codebook_definitions FROM dbo.CODELIST_DEFINITIONS;
            SELECT value.*, localization.LANGUAGE_CODE, localization.RESOURCE_KEY, localization.RESOURCE_LABEL,
                   localization.RESOURCE_HELPER, localization.RESOURCE_PLACEHOLDER
            INTO #codebook_values
            FROM dbo.CODE_LIST_VALUES AS value
            LEFT JOIN dbo.CODE_LIST_LOCALIZATION AS localization ON localization.CODE_LIST_ID = value.ID;

            DROP TABLE dbo.CODE_LIST_LOCALIZATION;
            DROP TABLE dbo.CODE_LIST_VALUES;
            DROP TABLE dbo.CODELIST_DEFINITIONS;
            """);

        CreateTables(migrationBuilder);
        migrationBuilder.Sql("""
            UPDATE #codebook_definitions
            SET NAME = CASE WHEN NAME = 'LanguagePacks' THEN 'LANGUAGE_PACKS' ELSE 'LANGUAGES' END
            WHERE NAME IN ('Languages', 'LanguagePacks');
            UPDATE #codebook_values
            SET CATEGORY = CASE WHEN CATEGORY = 'LanguagePacks' THEN 'LANGUAGE_PACKS' ELSE 'LANGUAGES' END
            WHERE CATEGORY IN ('Languages', 'LanguagePacks');

            SET IDENTITY_INSERT dbo.CODELIST_DEFINITIONS ON;
            INSERT dbo.CODELIST_DEFINITIONS (ID, NAME, DESCRIPTION, IS_ACTIVE, IS_SYSTEM_REQUIRED, VERSION,
                                           CREATED_BY, CREATED_AT, MODIFIED_BY, MODIFIED_AT)
            SELECT ID, NAME, DESCRIPTION, IS_ACTIVE, IS_SYSTEM_REQUIRED, VERSION,
                   CREATED_BY, CREATED_AT, MODIFIED_BY, MODIFIED_AT
            FROM #codebook_definitions;
            SET IDENTITY_INSERT dbo.CODELIST_DEFINITIONS OFF;

            INSERT dbo.CODELIST_DEFINITIONS (NAME, IS_ACTIVE, IS_SYSTEM_REQUIRED, VERSION, CREATED_BY, CREATED_AT)
            SELECT value.CATEGORY, 1, 0, 1, N'migration', SYSUTCDATETIME()
            FROM #codebook_values AS value
            WHERE NOT EXISTS (SELECT 1 FROM dbo.CODELIST_DEFINITIONS AS definition WHERE definition.NAME = value.CATEGORY)
            GROUP BY value.CATEGORY;

            SET IDENTITY_INSERT dbo.CODELIST_VALUES ON;
            INSERT dbo.CODELIST_VALUES (ID, DEFINITION_ID, CATEGORY, VALUE_KEY, NAME, DESCRIPTION, IS_SYSTEM_REQUIRED,
                                       ID1, ID12, ID13, ID16, ID19, ID20, CREATED_BY, CREATED_AT, MODIFIED_BY, MODIFIED_AT)
            SELECT value.ID, definition.ID, definition.NAME, value.VALUE_KEY, value.NAME, value.DESCRIPTION, value.IS_SYSTEM_REQUIRED,
                   value.IS_ACTIVE, value.LANGUAGE_CODE, value.RESOURCE_KEY, value.RESOURCE_LABEL, value.RESOURCE_HELPER,
                   value.RESOURCE_PLACEHOLDER, value.CREATED_BY, value.CREATED_AT, value.MODIFIED_BY, value.MODIFIED_AT
            FROM #codebook_values AS value
            INNER JOIN dbo.CODELIST_DEFINITIONS AS definition ON definition.NAME = value.CATEGORY;
            SET IDENTITY_INSERT dbo.CODELIST_VALUES OFF;

            DROP TABLE #codebook_values;
            DROP TABLE #codebook_definitions;
            """);

        SetUpLanguageDefinitions(migrationBuilder);
        SeedMissingTranslations(migrationBuilder);
    }

    private static void CreateTables(MigrationBuilder migrationBuilder)
    {
        var metadata = string.Join(",\n", Enumerable.Range(1, 20).Select(slot => $"""
            [COLUMN_NAME_{slot}] nvarchar(128) NULL,
            [COLUMN_DESCRIPTION_{slot}] nvarchar(4000) NULL,
            [COLUMN_POSITION_{slot}] int NULL
            """));
        migrationBuilder.Sql($"""
            CREATE TABLE dbo.CODELIST_DEFINITIONS (
                ID bigint IDENTITY NOT NULL CONSTRAINT PK_CODELIST_DEFINITIONS PRIMARY KEY,
                NAME varchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL,
                DESCRIPTION nvarchar(500) NULL,
                OWNER nvarchar(100) NOT NULL DEFAULT N'registry',
                IS_ACTIVE bit NOT NULL,
                IS_SYSTEM_REQUIRED bit NOT NULL,
                VERSION int NOT NULL,
                {metadata},
                CREATED_BY nvarchar(100) NOT NULL,
                CREATED_AT datetime2 NOT NULL,
                MODIFIED_BY nvarchar(100) NULL,
                MODIFIED_AT datetime2 NULL,
                CONSTRAINT CK_CodeListDefinitions_Name_Trimmed
                    CHECK (DATALENGTH(NAME) = DATALENGTH(LTRIM(RTRIM(NAME))))
            );
            CREATE UNIQUE INDEX IX_CODELIST_DEFINITIONS_NAME ON dbo.CODELIST_DEFINITIONS (NAME);

            CREATE TABLE dbo.CODELIST_VALUES (
                ID int IDENTITY NOT NULL CONSTRAINT PK_CODELIST_VALUES PRIMARY KEY,
                DEFINITION_ID bigint NULL,
                CATEGORY varchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL,
                VALUE_KEY varchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
                NAME nvarchar(200) NOT NULL,
                DESCRIPTION nvarchar(500) NULL,
                IS_SYSTEM_REQUIRED bit NOT NULL,
                ID1 bit NOT NULL,
                ID2 bit NULL,
                ID3 int NULL, ID4 int NULL, ID5 int NULL,
                ID6 decimal(18,2) NULL, ID7 decimal(18,2) NULL, ID8 decimal(19,6) NULL,
                ID9 date NULL, ID10 date NULL, ID11 datetime2 NULL,
                ID12 nvarchar(10) COLLATE Latin1_General_100_CI_AS NULL,
                ID13 nvarchar(200) COLLATE Latin1_General_100_CI_AS NULL,
                ID14 nvarchar(100) NULL, ID15 nvarchar(100) NULL,
                ID16 nvarchar(4000) NULL, ID17 nvarchar(4000) NULL, ID18 nvarchar(4000) NULL,
                ID19 nvarchar(4000) NULL, ID20 nvarchar(4000) NULL,
                CREATED_BY varchar(100) NOT NULL,
                CREATED_AT datetime2 NOT NULL,
                MODIFIED_BY varchar(100) NULL,
                MODIFIED_AT datetime2 NULL,
                CONSTRAINT CK_CodeListValues_Category_Trimmed CHECK (DATALENGTH(CATEGORY) = DATALENGTH(LTRIM(RTRIM(CATEGORY)))),
                CONSTRAINT CK_CodeListValues_ValueKey_Trimmed CHECK (DATALENGTH(VALUE_KEY) = DATALENGTH(LTRIM(RTRIM(VALUE_KEY)))),
                CONSTRAINT FK_CODELIST_VALUES_CODELIST_DEFINITIONS_DEFINITION_ID
                    FOREIGN KEY (DEFINITION_ID) REFERENCES dbo.CODELIST_DEFINITIONS (ID)
            );
            CREATE INDEX IX_CODELIST_VALUES_CATEGORY ON dbo.CODELIST_VALUES (CATEGORY);
            CREATE INDEX IX_CODELIST_VALUES_DEFINITION_ID ON dbo.CODELIST_VALUES (DEFINITION_ID);
            CREATE UNIQUE INDEX IX_CODELIST_VALUES_CATEGORY_VALUE_KEY ON dbo.CODELIST_VALUES (CATEGORY, VALUE_KEY);
            CREATE UNIQUE INDEX IX_CODELIST_VALUES_LANGUAGE_RESOURCE ON dbo.CODELIST_VALUES (ID12, ID13)
                WHERE CATEGORY = 'LANGUAGE_PACKS' AND ID12 IS NOT NULL AND ID13 IS NOT NULL;
            """);
    }

    private static void SetUpLanguageDefinitions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE dbo.CODELIST_DEFINITIONS
            SET COLUMN_NAME_1 = 'IS_ACTIVE', COLUMN_DESCRIPTION_1 = N'Enables the language or resource.', COLUMN_POSITION_1 = 1,
                VERSION = VERSION + 1
            WHERE NAME IN ('LANGUAGES', 'LANGUAGE_PACKS');

            UPDATE dbo.CODELIST_DEFINITIONS
            SET COLUMN_NAME_12 = 'LANGUAGE_CODE', COLUMN_DESCRIPTION_12 = N'Language code from LANGUAGES.', COLUMN_POSITION_12 = 2,
                COLUMN_NAME_13 = 'RESOURCE_KEY', COLUMN_DESCRIPTION_13 = N'Stable resource key used by the application.', COLUMN_POSITION_13 = 3,
                COLUMN_NAME_14 = 'CONTEXT_INFO', COLUMN_DESCRIPTION_14 = N'Screen or usage context.', COLUMN_POSITION_14 = 4,
                COLUMN_NAME_16 = 'LABEL_TEXT', COLUMN_DESCRIPTION_16 = N'Displayed label or message.', COLUMN_POSITION_16 = 5,
                COLUMN_NAME_19 = 'HELP_TEXT', COLUMN_DESCRIPTION_19 = N'Optional instructions.', COLUMN_POSITION_19 = 6,
                COLUMN_NAME_20 = 'PLACEHOLDER_TEXT', COLUMN_DESCRIPTION_20 = N'Optional field placeholder.', COLUMN_POSITION_20 = 7
            WHERE NAME = 'LANGUAGE_PACKS';

            UPDATE dbo.CODELIST_DEFINITIONS
            SET COLUMN_NAME_12 = 'LANGUAGE_CODE', COLUMN_DESCRIPTION_12 = N'Short culture code.', COLUMN_POSITION_12 = 2,
                COLUMN_NAME_13 = 'LANGUAGE_NAME', COLUMN_DESCRIPTION_13 = N'Name displayed in the language picker.', COLUMN_POSITION_13 = 3
            WHERE NAME = 'LANGUAGES';

            UPDATE dbo.CODELIST_VALUES SET ID12 = VALUE_KEY, ID13 = NAME
            WHERE CATEGORY = 'LANGUAGES';
            """);
    }

    private static void SeedMissingTranslations(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(ConsolidateCodebookLanguagePacks).Assembly.GetManifestResourceStream("CodebookLanguageCatalog.json")
            ?? throw new InvalidOperationException("The migration language catalog is missing.");
        using var reader = new StreamReader(stream);
        var seed = reader.ReadToEnd().Replace("'", "''", StringComparison.Ordinal);
        migrationBuilder.Sql($"""
            DECLARE @resources nvarchar(max) = N'{seed}';
            DECLARE @definition bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS');
            IF @definition IS NULL
                THROW 51011, 'The LANGUAGE_PACKS definition is required.', 1;

            INSERT dbo.CODELIST_VALUES (DEFINITION_ID, CATEGORY, VALUE_KEY, NAME, IS_SYSTEM_REQUIRED,
                                        ID1, ID12, ID13, ID14, ID16, CREATED_BY, CREATED_AT)
            SELECT @definition, 'LANGUAGE_PACKS', REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), item.ResourceKey, 1,
                   1, item.LanguageCode, item.ResourceKey, LEFT(item.ResourceKey, CHARINDEX('.', item.ResourceKey + '.') - 1),
                   item.Label, 'migration', SYSUTCDATETIME()
            FROM OPENJSON(@resources) WITH
            (
                LanguageCode nvarchar(10) '$.l', ResourceKey nvarchar(200) '$.k', Label nvarchar(4000) '$.v'
            ) AS item
            WHERE NOT EXISTS
            (
                SELECT 1 FROM dbo.CODELIST_VALUES AS existing
                WHERE existing.CATEGORY = 'LANGUAGE_PACKS'
                  AND existing.ID12 = item.LanguageCode COLLATE Latin1_General_100_CI_AS
                  AND existing.ID13 = item.ResourceKey COLLATE Latin1_General_100_CI_AS
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore a pre-migration database backup to recover the previous codebook schema.");
}
