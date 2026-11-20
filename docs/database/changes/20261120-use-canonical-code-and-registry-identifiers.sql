SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DROP INDEX [IX_RelatedPersons_NormalizedFbaId] ON [RelatedPersons];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DROP INDEX [IX_RelatedPersons_NormalizedPassport] ON [RelatedPersons];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DROP INDEX [IX_CodeListValues_NormalizedCategory_NormalizedCode] ON [CodeListValues];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RelatedPersons]') AND [c].[name] = N'NormalizedFbaId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [RelatedPersons] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [RelatedPersons] DROP COLUMN [NormalizedFbaId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RelatedPersons]') AND [c].[name] = N'NormalizedPassport');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [RelatedPersons] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [RelatedPersons] DROP COLUMN [NormalizedPassport];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'NormalizedCategory');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [NormalizedCategory];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'NormalizedCode');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [NormalizedCode];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DROP INDEX [IX_RelatedPersons_PassportNumber] ON [RelatedPersons];
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RelatedPersons]') AND [c].[name] = N'PassportNumber');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [RelatedPersons] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [RelatedPersons] ALTER COLUMN [PassportNumber] nvarchar(50) COLLATE Latin1_General_100_CI_AS NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_RelatedPersons_PassportNumber] ON [RelatedPersons] ([PassportNumber]) WHERE [IsActive] = 1 AND [PassportNumber] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DROP INDEX [IX_RelatedPersons_FBAId] ON [RelatedPersons];
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RelatedPersons]') AND [c].[name] = N'FBAId');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [RelatedPersons] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [RelatedPersons] ALTER COLUMN [FBAId] nvarchar(50) COLLATE Latin1_General_100_CI_AS NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_RelatedPersons_FBAId] ON [RelatedPersons] ([FBAId]) WHERE [IsActive] = 1 AND [FBAId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'Code');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [CodeListValues] ALTER COLUMN [Code] varchar(50) COLLATE Latin1_General_100_CI_AS NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    DROP INDEX [IX_CodeListValues_Category] ON [CodeListValues];
    DROP INDEX [IX_CodeListValues_Category_LanguageCode_ResourceKey] ON [CodeListValues];
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'Category');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [CodeListValues] ALTER COLUMN [Category] varchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL;
    CREATE INDEX [IX_CodeListValues_Category] ON [CodeListValues] ([Category]);
    EXEC(N'CREATE UNIQUE INDEX [IX_CodeListValues_Category_LanguageCode_ResourceKey] ON [CodeListValues] ([Category], [LanguageCode], [ResourceKey]) WHERE [LanguageCode] IS NOT NULL AND [ResourceKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    EXEC(N'ALTER TABLE [RelatedPersons] ADD CONSTRAINT [CK_RelatedPersons_FBAId_Trimmed] CHECK (DATALENGTH([FBAId]) = DATALENGTH(LTRIM(RTRIM([FBAId]))))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    EXEC(N'ALTER TABLE [RelatedPersons] ADD CONSTRAINT [CK_RelatedPersons_PassportNumber_Trimmed] CHECK (DATALENGTH([PassportNumber]) = DATALENGTH(LTRIM(RTRIM([PassportNumber]))))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CodeListValues_Category_Code] ON [CodeListValues] ([Category], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    EXEC(N'ALTER TABLE [CodeListValues] ADD CONSTRAINT [CK_CodeListValues_Category_Trimmed] CHECK (DATALENGTH([Category]) = DATALENGTH(LTRIM(RTRIM([Category]))))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    EXEC(N'ALTER TABLE [CodeListValues] ADD CONSTRAINT [CK_CodeListValues_Code_Trimmed] CHECK (DATALENGTH([Code]) = DATALENGTH(LTRIM(RTRIM([Code]))))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120150913_UseCanonicalCodeAndRegistryIdentifiers', N'10.0.3');
END;

COMMIT;
GO

