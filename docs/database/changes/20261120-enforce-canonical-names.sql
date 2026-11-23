SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DROP INDEX [IX_Roles_NormalizedName] ON [Roles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DROP INDEX [IX_CodeListDefinitions_NormalizedName] ON [CodeListDefinitions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DROP INDEX [IX_AppUsers_NormalizedEmail] ON [AppUsers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DROP INDEX [IX_AppUsers_NormalizedUsername] ON [AppUsers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'NormalizedName');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Roles] DROP COLUMN [NormalizedName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListDefinitions]') AND [c].[name] = N'NormalizedName');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [CodeListDefinitions] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [CodeListDefinitions] DROP COLUMN [NormalizedName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppUsers]') AND [c].[name] = N'NormalizedEmail');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [AppUsers] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [AppUsers] DROP COLUMN [NormalizedEmail];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppUsers]') AND [c].[name] = N'NormalizedUsername');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [AppUsers] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [AppUsers] DROP COLUMN [NormalizedUsername];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DROP INDEX [IX_Roles_Name] ON [Roles];
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Roles]') AND [c].[name] = N'Name');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Roles] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Roles] ALTER COLUMN [Name] nvarchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL;
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DROP INDEX [IX_CodeListDefinitions_Name] ON [CodeListDefinitions];
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListDefinitions]') AND [c].[name] = N'Name');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [CodeListDefinitions] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [CodeListDefinitions] ALTER COLUMN [Name] varchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL;
    CREATE UNIQUE INDEX [IX_CodeListDefinitions_Name] ON [CodeListDefinitions] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DROP INDEX [IX_AppUsers_Username] ON [AppUsers];
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppUsers]') AND [c].[name] = N'Username');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [AppUsers] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [AppUsers] ALTER COLUMN [Username] nvarchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL;
    CREATE UNIQUE INDEX [IX_AppUsers_Username] ON [AppUsers] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    DROP INDEX IF EXISTS [IX_AppUsers_Email] ON [dbo].[AppUsers];
    ALTER TABLE [dbo].[AppUsers]
        ALTER COLUMN [Email] nvarchar(200) COLLATE Latin1_General_100_CI_AS NOT NULL;
    CREATE UNIQUE INDEX [IX_AppUsers_Email] ON [dbo].[AppUsers] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [Roles] ADD CONSTRAINT [CK_Roles_Name_Trimmed] CHECK (DATALENGTH([Name]) = DATALENGTH(LTRIM(RTRIM([Name]))))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [CodeListDefinitions] ADD CONSTRAINT [CK_CodeListDefinitions_Name_Trimmed] CHECK (DATALENGTH([Name]) = DATALENGTH(LTRIM(RTRIM([Name]))))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [AppUsers] ADD CONSTRAINT [CK_AppUsers_Email_Trimmed] CHECK (DATALENGTH([Email]) = DATALENGTH(LTRIM(RTRIM([Email]))))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    EXEC(N'ALTER TABLE [AppUsers] ADD CONSTRAINT [CK_AppUsers_Username_Trimmed] CHECK (DATALENGTH([Username]) = DATALENGTH(LTRIM(RTRIM([Username]))))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120150620_EnforceCanonicalNamesWithoutDuplicateColumns', N'10.0.3');
END;

COMMIT;
GO

