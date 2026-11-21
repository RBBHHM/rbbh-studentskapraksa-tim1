SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    DROP INDEX [IX_CodeListValues_Category_LanguageCode_ResourceKey] ON [CodeListValues];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    ALTER TABLE [CodeListValues] DROP CONSTRAINT [CK_CodeListValues_Code_Trimmed];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[Code]', N'ValueKey', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[IX_CodeListValues_Category_Code]', N'IX_CodeListValues_Category_ValueKey', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    CREATE TABLE [CodeListLocalization] (
        [CodeListID] int NOT NULL,
        [Category] varchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL,
        [LanguageCode] nvarchar(10) NULL,
        [ResourceKey] nvarchar(200) COLLATE Latin1_General_100_CI_AS NULL,
        [ResourceLabel] nvarchar(4000) NULL,
        [ResourceHelper] nvarchar(4000) NULL,
        [ResourcePlaceholder] nvarchar(500) NULL,
        CONSTRAINT [PK_CodeListLocalization] PRIMARY KEY ([CodeListID]),
        CONSTRAINT [FK_CodeListLocalization_CodeListValues_CodeListID] FOREIGN KEY ([CodeListID]) REFERENCES [CodeListValues] ([ID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    EXEC(N'INSERT INTO [dbo].[CodeListLocalization]
        ([CodeListID], [Category], [LanguageCode], [ResourceKey], [ResourceLabel], [ResourceHelper], [ResourcePlaceholder])
    SELECT [ID], [Category], [LanguageCode], [ResourceKey], [ResourceLabel], [ResourceHelper], [ResourcePlaceholder]
    FROM [dbo].[CodeListValues]
    WHERE [LanguageCode] IS NOT NULL OR [ResourceKey] IS NOT NULL OR [ResourceLabel] IS NOT NULL
        OR [ResourceHelper] IS NOT NULL OR [ResourcePlaceholder] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'LanguageCode');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [LanguageCode];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'ResourceHelper');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [ResourceHelper];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'ResourceKey');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [ResourceKey];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'ResourceLabel');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [ResourceLabel];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'ResourcePlaceholder');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [ResourcePlaceholder];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    EXEC(N'ALTER TABLE [CodeListValues] ADD CONSTRAINT [CK_CodeListValues_ValueKey_Trimmed] CHECK (DATALENGTH([ValueKey]) = DATALENGTH(LTRIM(RTRIM([ValueKey]))))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CodeListLocalization_Category_LanguageCode_ResourceKey] ON [CodeListLocalization] ([Category], [LanguageCode], [ResourceKey]) WHERE [LanguageCode] IS NOT NULL AND [ResourceKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121080233_SplitCodeListLocalization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121080233_SplitCodeListLocalization', N'10.0.3');
END;

COMMIT;
GO
