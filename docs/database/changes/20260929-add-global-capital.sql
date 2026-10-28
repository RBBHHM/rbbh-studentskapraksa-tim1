-- Migracija: AddGlobalCapital (20260929102823)
--
-- Kreira novu tabelu [Capitals] — jedan globalni (singleton) zapis kapitala banke
-- (Osnovni, Regulatorni, Dopunski kapital + Datum kapitala), bez veze na pravno lice.
-- Ovaj zapis se koristi kao pomoćna vrijednost za auto-popunjavanje istih polja na
-- svim redovima modula Limiti. Tabela [ClientLimits] (koja i dalje služi Report modulu
-- po klijentu) i tabela [Limiti] ostaju netaknute ovom migracijom.

BEGIN TRANSACTION;
BEGIN TRY
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929102823_AddGlobalCapital'
)
BEGIN
    CREATE TABLE [Capitals] (
        [Id] uniqueidentifier NOT NULL,
        [RegulatoryCapital] decimal(18,2) NOT NULL,
        [CoreCapital] decimal(18,2) NOT NULL,
        [SupplementaryCapital] decimal(18,2) NOT NULL,
        [CapitalDate] date NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Capitals] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929102823_AddGlobalCapital'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929102823_AddGlobalCapital', N'10.0.3');
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
