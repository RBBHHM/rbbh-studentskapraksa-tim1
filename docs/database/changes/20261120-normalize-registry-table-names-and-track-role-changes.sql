BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [CodeLists] DROP CONSTRAINT [FK_CodeLists_CodeListDefinitions_DefinitionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [Limiti] DROP CONSTRAINT [FK_Limiti_LegalEntities_LegalEntityId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [Limiti] DROP CONSTRAINT [PK_Limiti];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [CodeLists] DROP CONSTRAINT [PK_CodeLists];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[Limiti]', N'Limits', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[CodeLists]', N'CodeListValues', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[Limits].[TipLimita]', N'LimitType', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[Limits].[RokOcekivaneUtilizacije]', N'ExpectedUtilizationDate', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[Limits].[OdobreniLimit]', N'ApprovedLimit', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[Limits].[MaksimalnaOcekivanaUtilizacija]', N'MaximumExpectedUtilization', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[Limits].[Komentar]', N'Comment', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[Limits].[IX_Limiti_TipLimita]', N'IX_Limits_LimitType', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[Limits].[IX_Limiti_LegalEntityId]', N'IX_Limits_LegalEntityId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[Limits].[IX_Limiti_ImportFingerprint]', N'IX_Limits_ImportFingerprint', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[IX_CodeLists_NormalizedCategory_NormalizedCode]', N'IX_CodeListValues_NormalizedCategory_NormalizedCode', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[IX_CodeLists_Kategorija_LanguageCode_ResourceKey]', N'IX_CodeListValues_Kategorija_LanguageCode_ResourceKey', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[IX_CodeLists_Kategorija]', N'IX_CodeListValues_Kategorija', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[IX_CodeLists_DefinitionId]', N'IX_CodeListValues_DefinitionId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [UserRoles] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [UserRoles] ADD [ModifiedBy] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [Limits] ADD CONSTRAINT [PK_Limits] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [CodeListValues] ADD CONSTRAINT [PK_CodeListValues] PRIMARY KEY ([ID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [CodeListValues] ADD CONSTRAINT [FK_CodeListValues_CodeListDefinitions_DefinitionId] FOREIGN KEY ([DefinitionId]) REFERENCES [CodeListDefinitions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    ALTER TABLE [Limits] ADD CONSTRAINT [FK_Limits_LegalEntities_LegalEntityId] FOREIGN KEY ([LegalEntityId]) REFERENCES [LegalEntities] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120140107_NormalizeRegistryTableNamesAndTrackRoleChanges', N'10.0.3');
END;

COMMIT;
GO

