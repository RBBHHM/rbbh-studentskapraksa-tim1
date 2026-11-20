BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120135607_RemoveRedundantSubjectLinkTimestamp'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AppUsers]') AND [c].[name] = N'KeycloakSubjectLinkedAt');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [AppUsers] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [AppUsers] DROP COLUMN [KeycloakSubjectLinkedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120135607_RemoveRedundantSubjectLinkTimestamp'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120135607_RemoveRedundantSubjectLinkTimestamp', N'10.0.3');
END;

COMMIT;
GO

