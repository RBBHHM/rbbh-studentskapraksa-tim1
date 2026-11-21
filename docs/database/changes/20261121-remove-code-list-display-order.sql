SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121070511_RemoveCodeListDisplayOrder'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CodeListValues]') AND [c].[name] = N'DisplayOrder');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [CodeListValues] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [CodeListValues] DROP COLUMN [DisplayOrder];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121070511_RemoveCodeListDisplayOrder'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121070511_RemoveCodeListDisplayOrder', N'10.0.3');
END;

COMMIT;
GO

