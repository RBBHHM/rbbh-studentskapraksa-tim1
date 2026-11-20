BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[LegalEntities].[MaticniBroj]', N'RegistrationNumber', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[LegalEntities].[Matbroj]', N'BankCustomerNumber', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[RedoslijedPrikaza]', N'DisplayOrder', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[Opis]', N'Description', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[Naziv]', N'Name', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[KreiraoKorisnik]', N'CreatedBy', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[KreiranDatum]', N'CreatedAt', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[IzmijenjenDatum]', N'ModifiedAt', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[IzmijenioKorisnik]', N'ModifiedBy', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    EXEC sp_rename N'[CodeListValues].[Aktivan]', N'IsActive', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261120140426_NormalizeRegistryColumnNames'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261120140426_NormalizeRegistryColumnNames', N'10.0.3');
END;

COMMIT;
GO

