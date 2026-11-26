using Microsoft.EntityFrameworkCore.Migrations;

namespace RBBH.ConnectedParties.Api.Migrations;

public partial class StandardizeRegistrySchema
{
    private static void StageLegacyData(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        SELECT * INTO #registry_capital_revisions FROM dbo.CapitalRevisions;
        SELECT * INTO #registry_client_limits FROM dbo.ClientLimits;
        SELECT * INTO #registry_localization FROM dbo.CodeListLocalization;

        DROP VIEW IF EXISTS dbo.RequestedRoleDetails;
        DROP VIEW IF EXISTS dbo.RolePermissionDetails;

        UPDATE dbo.AuditLogs
        SET NewValues = CASE WHEN NewValues IS NULL AND OldValues IS NOT NULL THEN NULL
                 WHEN ISJSON(NewValues) = 1 AND LEFT(LTRIM(NewValues), 1) = '{'
                 THEN JSON_MODIFY(NewValues, '$._recordId', RecordId)
                 ELSE JSON_MODIFY(JSON_MODIFY(N'{}', '$.value', NewValues), '$._recordId', RecordId) END,
            OldValues = CASE WHEN NewValues IS NOT NULL OR OldValues IS NULL THEN OldValues
                 WHEN ISJSON(OldValues) = 1 AND LEFT(LTRIM(OldValues), 1) = '{'
                 THEN JSON_MODIFY(OldValues, '$._recordId', RecordId)
                 ELSE JSON_MODIFY(JSON_MODIFY(N'{}', '$.value', OldValues), '$._recordId', RecordId) END
        WHERE DATALENGTH(RecordId) > 0;
        """);

    private static void RestoreLocalizedValues(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT dbo.CODE_LIST_LOCALIZATION
            (CODE_LIST_ID, CATEGORY, LANGUAGE_CODE, RESOURCE_KEY, RESOURCE_LABEL, RESOURCE_HELPER, RESOURCE_PLACEHOLDER)
        SELECT CodeListID, Category, LanguageCode, ResourceKey, ResourceLabel, ResourceHelper, ResourcePlaceholder
        FROM #registry_localization;
        DROP TABLE #registry_localization;
        """);

    private static void SeedCapitalLabels(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DECLARE @newLabels TABLE
        (
            LanguageCode nvarchar(10),
            Label nvarchar(200),
            ValueKey varchar(50) COLLATE Latin1_General_100_CI_AS
        );
        INSERT @newLabels VALUES
            (N'BS', N'Novi kapital', REPLACE(CONVERT(varchar(36), NEWID()), '-', '')),
            (N'EN', N'New capital', REPLACE(CONVERT(varchar(36), NEWID()), '-', '')),
            (N'DE', N'Neues Kapital', REPLACE(CONVERT(varchar(36), NEWID()), '-', ''));

        DELETE label FROM @newLabels AS label
        WHERE EXISTS
        (
            SELECT 1 FROM dbo.CODE_LIST_LOCALIZATION AS existing
            WHERE existing.CATEGORY = 'LanguagePacks' AND existing.RESOURCE_KEY = N'capital.create'
              AND existing.LANGUAGE_CODE = label.LanguageCode
        );

        INSERT dbo.CODE_LIST_VALUES (CATEGORY, VALUE_KEY, NAME, IS_ACTIVE, CREATED_AT, CREATED_BY, IS_SYSTEM_REQUIRED)
        SELECT 'LanguagePacks', ValueKey, N'capital.create', 1, SYSUTCDATETIME(), 'migration', 1
        FROM @newLabels;

        INSERT dbo.CODE_LIST_LOCALIZATION (CODE_LIST_ID, CATEGORY, LANGUAGE_CODE, RESOURCE_KEY, RESOURCE_LABEL)
        SELECT value.ID, 'LanguagePacks', label.LanguageCode, N'capital.create', label.Label
        FROM @newLabels AS label
        INNER JOIN dbo.CODE_LIST_VALUES AS value ON value.CATEGORY = 'LanguagePacks' AND value.VALUE_KEY = label.ValueKey;

        UPDATE localization
        SET RESOURCE_LABEL = translations.NewLabel
        FROM dbo.CODE_LIST_LOCALIZATION AS localization
        INNER JOIN (VALUES
            (N'BS', N'capital.history-help', N'Raniji kapitali su samo za pregled i ne mogu se mijenjati. Svaka izmjena dodaje novu reviziju.', N'Raniji kapitali su dostupni samo za pregled.'),
            (N'EN', N'capital.history-help', N'Earlier capital is for reference only and cannot be edited. Every change adds a new revision.', N'Past capital periods are read-only.'),
            (N'DE', N'capital.history-help', N'Frühere Kapitalstände dienen nur als Referenz und können nicht bearbeitet werden. Jede Änderung erzeugt eine neue Revision.', N'Frühere Kapitalperioden können nur angezeigt werden.'),
            (N'BS', N'capital.history-empty', N'Još nema evidentiranih revizija kapitala.', N'Još nema prethodnih perioda kapitala.'),
            (N'EN', N'capital.history-empty', N'No capital revisions have been recorded yet.', N'No previous capital periods.'),
            (N'DE', N'capital.history-empty', N'Es wurden noch keine Kapitalrevisionen erfasst.', N'Es gibt noch keine früheren Kapitalperioden.')
        ) AS translations(LanguageCode, ResourceKey, OldLabel, NewLabel)
            ON translations.LanguageCode = localization.LANGUAGE_CODE
            AND translations.ResourceKey COLLATE Latin1_General_100_CI_AS = localization.RESOURCE_KEY
            AND translations.OldLabel = localization.RESOURCE_LABEL
        WHERE localization.CATEGORY = 'LanguagePacks';
        """);

    private static void ConsolidateCapitalAndLimits(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DECLARE @capitalOffset bigint = ISNULL((SELECT MAX(ID) FROM dbo.CAPITALS), 0);

        SET IDENTITY_INSERT dbo.CAPITALS ON;
        INSERT dbo.CAPITALS (ID, IS_ACTIVE, VALID_FROM, VALID_TILL, REGULATORY_CAPITAL, CORE_CAPITAL,
                             SUPPLEMENTARY_CAPITAL, CREATED_BY, CREATED_AT)
        SELECT @capitalOffset + Id, 0, EffectiveDate, CONVERT(date, '99991231'), RegulatoryCapital, CoreCapital,
               SupplementaryCapital, RecordedBy, RecordedAt
        FROM #registry_capital_revisions;
        SET IDENTITY_INSERT dbo.CAPITALS OFF;

        UPDATE audit
        SET NEW_VALUES = JSON_MODIFY(audit.NEW_VALUES, '$._recordId', CONVERT(nvarchar(20), @capitalOffset + revision.Id))
        FROM dbo.AUDIT_LOGS AS audit
        INNER JOIN #registry_capital_revisions AS revision
            ON revision.Id = TRY_CONVERT(bigint, JSON_VALUE(audit.NEW_VALUES, '$._recordId'))
        WHERE audit.TABLE_NAME = N'Capital' AND audit.ACTION = N'UPSERT';

        WITH ordered AS
        (
            SELECT ID, LEAD(VALID_FROM) OVER (ORDER BY VALID_FROM, ID) AS NEXT_START
            FROM dbo.CAPITALS
        )
        UPDATE capital
        SET IS_ACTIVE = CASE WHEN ordered.NEXT_START IS NULL THEN 1 ELSE 0 END,
            VALID_TILL = CASE WHEN ordered.NEXT_START IS NULL THEN CONVERT(date, '99991231')
                              WHEN ordered.NEXT_START = CONVERT(date, '00010101') THEN ordered.NEXT_START
                              ELSE DATEADD(day, -1, ordered.NEXT_START) END
        FROM dbo.CAPITALS AS capital INNER JOIN ordered ON ordered.ID = capital.ID;

        UPDATE limits
        SET CAPITAL_ID = capital.ID
        FROM dbo.LIMITS AS limits
        CROSS APPLY
        (
            SELECT TOP (1) ID FROM dbo.CAPITALS
            WHERE CONVERT(date, limits.CREATED_AT) BETWEEN VALID_FROM AND VALID_TILL
            ORDER BY VALID_FROM DESC, ID DESC
        ) AS capital;

        INSERT dbo.LIMITS
            (LIMIT_TYPE, APPROVED_LIMIT, CAPITAL_ID, LEGAL_ENTITY_ID, EXPECTED_UTILIZATION_DATE,
             MAXIMUM_EXPECTED_UTILIZATION, COMMENT, CREATED_BY, CREATED_AT, MODIFIED_BY, MODIFIED_AT)
        SELECT N'LEGACY_CLIENT_LIMIT', source.ExposureLimit, capital.ID, source.LegalEntityId, source.CapitalDate,
               source.CurrentExposure,
               (SELECT source.Id AS LegacyId, source.Currency, source.IsLimitBreached, source.IsActive,
                       source.RegulatoryCapital, source.CoreCapital, source.SupplementaryCapital, source.CapitalDate
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
               source.CreatedBy, source.CreatedAt, source.ModifiedBy, source.ModifiedAt
        FROM #registry_client_limits AS source
        OUTER APPLY
        (
            SELECT TOP (1) ID FROM dbo.CAPITALS
            WHERE VALID_FROM = CONVERT(date, source.CapitalDate)
              AND REGULATORY_CAPITAL = source.RegulatoryCapital AND CORE_CAPITAL = source.CoreCapital
              AND SUPPLEMENTARY_CAPITAL = source.SupplementaryCapital
            ORDER BY ID DESC
        ) AS capital;

        DROP TABLE #registry_capital_revisions;
        DROP TABLE #registry_client_limits;
        """);

    private static void RecreateDetailsViews(MigrationBuilder migrationBuilder)
    {
        Execute(migrationBuilder, """
            CREATE VIEW dbo.REQUESTED_ROLE_DETAILS AS
            SELECT item.ID, item.REQUEST_ID, item.ROLE_ID, request.USERNAME AS REQUESTED_BY,
                   request.FIRST_NAME, request.LAST_NAME, role.NAME AS ROLE_NAME,
                   request.CREATED_AT AS REQUESTED_AT, request.STATUS, request.NOTE,
                   request.PROCESSED_BY, request.PROCESSED_AT, request.DECISION_NOTE
            FROM dbo.REQUESTED_ROLE AS item
            INNER JOIN dbo.ROLE_REQUESTS AS request ON request.ID = item.REQUEST_ID
            INNER JOIN dbo.ROLES AS role ON role.ID = item.ROLE_ID;
            """);
        Execute(migrationBuilder, """
            CREATE VIEW dbo.ROLE_PERMISSION_DETAILS AS
            SELECT item.ID, item.ROLE_ID, item.PERMISSION_ID, role.NAME AS ROLE_NAME,
                   permission.CODE AS PERMISSION_CODE, permission.MODULE
            FROM dbo.ROLE_PERMISSIONS AS item
            INNER JOIN dbo.ROLES AS role ON role.ID = item.ROLE_ID
            INNER JOIN dbo.PERMISSIONS AS permission ON permission.ID = item.PERMISSION_ID;
            """);
    }

    private static void RebuildOrderedTables(MigrationBuilder migrationBuilder)
    {
        Rebuild(migrationBuilder, "APP_USERS", "[ID], [KEYCLOAK_ID], [USERNAME], [FIRST_NAME], [LAST_NAME], [EMAIL], [IS_ACTIVE], [LOGIN_CONFIRMED], [CREATED_AT], [CREATED_BY], [MODIFIED_AT], [MODIFIED_BY]", """
                [ID] bigint NOT NULL,
                [KEYCLOAK_ID] nvarchar(100) COLLATE Latin1_General_100_BIN2 NOT NULL,
                [USERNAME] nvarchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL,
                [FIRST_NAME] nvarchar(100) NOT NULL,
                [LAST_NAME] nvarchar(100) NOT NULL,
                [EMAIL] nvarchar(200) COLLATE Latin1_General_100_CI_AS NOT NULL,
                [IS_ACTIVE] bit NOT NULL,
                [LOGIN_CONFIRMED] bit NOT NULL,
                [CREATED_AT] datetime2 NOT NULL,
                [CREATED_BY] nvarchar(100) NOT NULL,
                [MODIFIED_AT] datetime2 NULL,
                [MODIFIED_BY] nvarchar(100) NULL
                """, identity: false);
        Rebuild(migrationBuilder, "AUDIT_LOGS", "[ID], [ACTION], [USER], [TABLE_NAME], [OLD_VALUES], [NEW_VALUES], [TIMESTAMP]", """
                [ID] bigint NOT NULL IDENTITY,
                [ACTION] nvarchar(50) NOT NULL,
                [USER] nvarchar(100) NOT NULL,
                [TABLE_NAME] nvarchar(100) NOT NULL,
                [OLD_VALUES] nvarchar(max) NULL,
                [NEW_VALUES] nvarchar(max) NULL,
                [TIMESTAMP] datetime2 NOT NULL
                """, identity: true);
        Rebuild(migrationBuilder, "CAPITALS", "[ID], [IS_ACTIVE], [VALID_TILL], [REGULATORY_CAPITAL], [CORE_CAPITAL], [SUPPLEMENTARY_CAPITAL], [CREATED_BY], [CREATED_AT], [MODIFIED_BY], [MODIFIED_AT], [VALID_FROM]", """
                [ID] bigint NOT NULL IDENTITY,
                [IS_ACTIVE] bit NOT NULL,
                [VALID_TILL] date NOT NULL,
                [REGULATORY_CAPITAL] decimal(18,2) NOT NULL,
                [CORE_CAPITAL] decimal(18,2) NOT NULL,
                [SUPPLEMENTARY_CAPITAL] decimal(18,2) NOT NULL,
                [CREATED_BY] nvarchar(100) NOT NULL,
                [CREATED_AT] datetime2 NOT NULL,
                [MODIFIED_BY] nvarchar(100) NULL,
                [MODIFIED_AT] datetime2 NULL,
                [VALID_FROM] date NOT NULL
                """, identity: true);
        Rebuild(migrationBuilder, "LIMITS", "[ID], [LIMIT_TYPE], [APPROVED_LIMIT], [CAPITAL_ID], [LEGAL_ENTITY_ID], [EXPECTED_UTILIZATION_DATE], [MAXIMUM_EXPECTED_UTILIZATION], [COMMENT], [CREATED_BY], [CREATED_AT], [MODIFIED_BY], [MODIFIED_AT]", """
                [ID] int NOT NULL IDENTITY,
                [LIMIT_TYPE] nvarchar(100) NOT NULL,
                [APPROVED_LIMIT] decimal(18,2) NULL,
                [CAPITAL_ID] bigint NULL,
                [LEGAL_ENTITY_ID] bigint NOT NULL,
                [EXPECTED_UTILIZATION_DATE] datetime2 NULL,
                [MAXIMUM_EXPECTED_UTILIZATION] decimal(18,2) NULL,
                [COMMENT] nvarchar(1000) NULL,
                [CREATED_BY] nvarchar(100) NOT NULL,
                [CREATED_AT] datetime2 NOT NULL,
                [MODIFIED_BY] nvarchar(100) NULL,
                [MODIFIED_AT] datetime2 NULL
                """, identity: true);
    }

    private static void Rebuild(MigrationBuilder migrationBuilder, string table, string columns, string definitions, bool identity)
    {
        var identityOn = identity ? $"SET IDENTITY_INSERT dbo.[{table}] ON;" : "";
        var identityOff = identity ? $"SET IDENTITY_INSERT dbo.[{table}] OFF;" : "";
        Execute(migrationBuilder, $"""
            SELECT {columns} INTO #registry_ordered_copy FROM dbo.[{table}];
            DROP TABLE dbo.[{table}];
            CREATE TABLE dbo.[{table}] ({definitions});
            {identityOn}
            INSERT dbo.[{table}] ({columns}) SELECT {columns} FROM #registry_ordered_copy;
            {identityOff}
            """);
    }

    private static void CreateOrderedTableIndexes(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE UNIQUE INDEX [IX_APP_USERS_EMAIL] ON [APP_USERS] ([EMAIL]);
        CREATE UNIQUE INDEX [IX_APP_USERS_KEYCLOAK_ID] ON [APP_USERS] ([KEYCLOAK_ID]);
        CREATE UNIQUE INDEX [IX_APP_USERS_USERNAME] ON [APP_USERS] ([USERNAME]);
        CREATE INDEX [IX_AUDIT_LOGS_TABLE_NAME] ON [AUDIT_LOGS] ([TABLE_NAME]);
        CREATE INDEX [IX_AUDIT_LOGS_TIMESTAMP] ON [AUDIT_LOGS] ([TIMESTAMP]);
        CREATE INDEX [IX_AUDIT_LOGS_USER] ON [AUDIT_LOGS] ([USER]);
        CREATE UNIQUE INDEX [IX_CAPITALS_IS_ACTIVE] ON [CAPITALS] ([IS_ACTIVE]) WHERE [IS_ACTIVE] = 1;
        CREATE INDEX [IX_CAPITALS_VALID_FROM_VALID_TILL] ON [CAPITALS] ([VALID_FROM], [VALID_TILL]);
        CREATE INDEX [IX_LIMITS_CAPITAL_ID] ON [LIMITS] ([CAPITAL_ID]);
        CREATE INDEX [IX_LIMITS_LEGAL_ENTITY_ID] ON [LIMITS] ([LEGAL_ENTITY_ID]);
        CREATE INDEX [IX_LIMITS_LIMIT_TYPE] ON [LIMITS] ([LIMIT_TYPE]);
        """);

    private static void Execute(MigrationBuilder migrationBuilder, string sql) =>
        migrationBuilder.Sql("EXEC(N'" + sql.Replace("'", "''", StringComparison.Ordinal) + "')");
}
