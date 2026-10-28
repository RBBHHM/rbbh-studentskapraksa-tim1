-- Migracija: RedesignLimitiClientLinkage (20260928123241)
--
-- NAPOMENA (VAŽNO prije primjene na bazu koja već sadrži podatke u tabeli [Limiti]):
-- Limit zapisi su do sada bili samostalni katalog (Naziv slobodan tekst, bez veze na
-- konkretno pravno lice). Ova migracija uvodi obavezan FK [LegalEntityId] -> [LegalEntities].
-- Ne postoji način da EF/migracija automatski mapira postojeće redove (slobodan tekst Naziv)
-- na tačan LegalEntityId, pa se novoj koloni dodjeljuje privremeni default '00000000-0000-0000-0000-000000000000'.
-- Korak ALTER TABLE ... ADD CONSTRAINT [FK_Limiti_LegalEntities_LegalEntityId] će PUCATI
-- ako u tabeli [Limiti] postoji bilo koji red, jer '00000000-0000-0000-0000-000000000000' ne
-- postoji u [LegalEntities]. Prije primjene ove migracije na bazu koja ima postojeće Limiti
-- redove, potrebno je:
--   1) izvesti/zabilježiti postojeće Limiti podatke (ako su bitni),
--   2) obrisati sve redove iz [Limiti] (DELETE FROM [Limiti];) ili ih ručno ažurirati sa
--      važećim [LegalEntityId] vrijednostima iz [LegalEntities],
--   3) tek onda pokrenuti ovu migraciju.
-- Na praznoj/dev/test bazi (bez postojećih Limiti redova) ovaj korak prolazi bez problema.

BEGIN TRANSACTION;
BEGIN TRY
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    DROP INDEX [IX_Limiti_Naziv] ON [Limiti];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Limiti]') AND [c].[name] = N'IznosLimita');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Limiti] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Limiti] DROP COLUMN [IznosLimita];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Limiti]') AND [c].[name] = N'Naziv');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Limiti] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Limiti] DROP COLUMN [Naziv];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Limiti]') AND [c].[name] = N'OsnovniKapital');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Limiti] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Limiti] DROP COLUMN [OsnovniKapital];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Limiti]') AND [c].[name] = N'RaspoloziviLimit');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Limiti] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Limiti] DROP COLUMN [RaspoloziviLimit];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Limiti]') AND [c].[name] = N'RegulatorniKapital');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Limiti] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Limiti] DROP COLUMN [RegulatorniKapital];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Limiti]') AND [c].[name] = N'Utilizacija');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Limiti] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Limiti] DROP COLUMN [Utilizacija];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    EXEC sp_rename N'[Limiti].[RokUtilizacije]', N'RokOcekivaneUtilizacije', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    EXEC sp_rename N'[Limiti].[KorigovaniLimit]', N'OdobreniLimit', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    ALTER TABLE [Limiti] ADD [LegalEntityId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    ALTER TABLE [Limiti] ADD [MaksimalnaOcekivanaUtilizacija] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    CREATE INDEX [IX_Limiti_LegalEntityId] ON [Limiti] ([LegalEntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    ALTER TABLE [Limiti] ADD CONSTRAINT [FK_Limiti_LegalEntities_LegalEntityId] FOREIGN KEY ([LegalEntityId]) REFERENCES [LegalEntities] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260928123241_RedesignLimitiClientLinkage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260928123241_RedesignLimitiClientLinkage', N'10.0.3');
END;

COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorState INT = ERROR_STATE();
    THROW 50000, @ErrorMessage, @ErrorState;
END CATCH;
GO
