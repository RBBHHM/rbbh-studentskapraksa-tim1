SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    DROP INDEX [IX_CodeListValues_NormalizedCategory_NormalizedCode] ON [CodeListValues];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'NormalizedCategory');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [NormalizedCategory];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'NormalizedCode');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [NormalizedCode];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[Kod]', N'Code', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[Kategorija]', N'Category', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[IX_CodeListValues_Kategorija_LanguageCode_ResourceKey]', N'IX_CodeListValues_Category_LanguageCode_ResourceKey', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[IX_CodeListValues_Kategorija]', N'IX_CodeListValues_Category', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    EXEC(N'ALTER TABLE [CodeListValues] ADD [NormalizedCategory] AS UPPER(LTRIM(RTRIM([Category]))) PERSISTED');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    EXEC(N'ALTER TABLE [CodeListValues] ADD [NormalizedCode] AS UPPER(LTRIM(RTRIM([Code]))) PERSISTED');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CodeListValues_NormalizedCategory_NormalizedCode] ON [CodeListValues] ([NormalizedCategory], [NormalizedCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150505_NormalizeCodeListKeyColumnNames'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120150505_NormalizeCodeListKeyColumnNames', N'10.0.3');
END;

COMMIT;
GO

