SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120202030_UseRegistryAuditConcurrencyAndLabelPermissions'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RelatedPersons]') AND [c].[name] = N'Version');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [RelatedPersons] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [RelatedPersons] DROP COLUMN [Version];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120202030_UseRegistryAuditConcurrencyAndLabelPermissions'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LegalEntities]') AND [c].[name] = N'Version');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [LegalEntities] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [LegalEntities] DROP COLUMN [Version];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120202030_UseRegistryAuditConcurrencyAndLabelPermissions'
)
BEGIN
    EXEC sp_rename N'[Permissions].[Description]', N'Label', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120202030_UseRegistryAuditConcurrencyAndLabelPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120202030_UseRegistryAuditConcurrencyAndLabelPermissions', N'10.0.3');
END;

COMMIT;
GO

