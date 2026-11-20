SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150238_UseUnlockRequestAuditConcurrency'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[UnlockRequests]') AND [c].[name] = N'Version');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [UnlockRequests] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [UnlockRequests] DROP COLUMN [Version];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120150238_UseUnlockRequestAuditConcurrency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120150238_UseUnlockRequestAuditConcurrency', N'10.0.3');
END;

COMMIT;
GO

