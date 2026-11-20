SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120144108_RemoveLegacyPeriodLockDepartment'
)
BEGIN
    EXEC(N'IF EXISTS (SELECT 1 FROM dbo.PeriodLocks WITH (TABLOCKX, HOLDLOCK) WHERE Department IS NOT NULL)
        THROW 51000, ''Reconcile legacy department locks before removing PeriodLocks.Department.'', 1;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120144108_RemoveLegacyPeriodLockDepartment'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PeriodLocks]') AND [c].[name] = N'Department');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [PeriodLocks] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [PeriodLocks] DROP COLUMN [Department];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120144108_RemoveLegacyPeriodLockDepartment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120144108_RemoveLegacyPeriodLockDepartment', N'10.0.3');
END;

COMMIT;
GO

