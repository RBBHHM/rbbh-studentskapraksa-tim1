-- Migracija: AddSupplementaryCapitalAndCapitalDate (20260929075336)
--
-- NAPOMENA (prije primjene na bazu koja već sadrži redove u tabeli [ClientLimits]):
-- Dodaju se dvije nove kolone modula Kapital: [SupplementaryCapital] (Dopunski kapital) i
-- [CapitalDate] (Datum kapitala). Za postojeće redove EF dodjeljuje privremeni default
-- (SupplementaryCapital = 0.0, CapitalDate = '0001-01-01'), jer ne postoji način da se ove
-- vrijednosti automatski izvedu iz postojećih podataka. Aplikacija od sada tretira oba polja
-- kao obavezna (validacija u ClientLimitService.Upsert), pa je potrebno da korisnici ručno
-- ažuriraju Dopunski kapital i Datum kapitala za sve postojeće klijente preko Kapital forme
-- nakon primjene ove migracije. Na praznoj/dev/test bazi (bez postojećih ClientLimits redova)
-- ovaj korak prolazi bez posljedica.

BEGIN TRANSACTION;
BEGIN TRY
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929075336_AddSupplementaryCapitalAndCapitalDate'
)
BEGIN
    ALTER TABLE [ClientLimits] ADD [CapitalDate] date NOT NULL DEFAULT '0001-01-01';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929075336_AddSupplementaryCapitalAndCapitalDate'
)
BEGIN
    ALTER TABLE [ClientLimits] ADD [SupplementaryCapital] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929075336_AddSupplementaryCapitalAndCapitalDate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929075336_AddSupplementaryCapitalAndCapitalDate', N'10.0.3');
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
