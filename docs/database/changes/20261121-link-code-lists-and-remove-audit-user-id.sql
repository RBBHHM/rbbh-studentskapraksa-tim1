SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121070153_LinkCodeListsByCategory'
)
BEGIN
    EXEC(N'IF EXISTS (
        SELECT 1
        FROM dbo.CodeListValues AS value
        JOIN dbo.CodeListDefinitions AS definition ON definition.Id = value.DefinitionId
        WHERE value.Category <> definition.Name
    )
        THROW 51000, ''Resolve conflicting code list categories before removing DefinitionId.'', 1;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121070153_LinkCodeListsByCategory'
)
BEGIN
    ALTER TABLE [CodeListValues] DROP CONSTRAINT [FK_CodeListValues_CodeListDefinitions_DefinitionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121070153_LinkCodeListsByCategory'
)
BEGIN
    DROP INDEX [IX_CodeListValues_DefinitionId] ON [CodeListValues];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121070153_LinkCodeListsByCategory'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'DefinitionId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [DefinitionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121070153_LinkCodeListsByCategory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121070153_LinkCodeListsByCategory', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121070226_RemoveAuditUserId'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditLogs]') AND [c].[name] = N'UserId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [AuditLogs] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [AuditLogs] DROP COLUMN [UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121070226_RemoveAuditUserId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121070226_RemoveAuditUserId', N'10.0.3');
END;

COMMIT;
GO

