SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120151233_RemoveLimitImportFingerprint'
)
BEGIN
    DROP INDEX [IX_Limits_ImportFingerprint] ON [Limits];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120151233_RemoveLimitImportFingerprint'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Limits]') AND [c].[name] = N'ImportFingerprint');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Limits] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Limits] DROP COLUMN [ImportFingerprint];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120151233_RemoveLimitImportFingerprint'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120151233_RemoveLimitImportFingerprint', N'10.0.3');
END;

COMMIT;
GO

