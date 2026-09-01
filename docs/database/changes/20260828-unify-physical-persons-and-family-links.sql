BEGIN TRANSACTION;
BEGIN TRY
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828164555_UnifyPhysicalPersonsAndFamilyLinks'
)
BEGIN
    ALTER TABLE [RelatedPersons] ADD [FamilyRelationshipType] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828164555_UnifyPhysicalPersonsAndFamilyLinks'
)
BEGIN
    ALTER TABLE [RelatedPersons] ADD [RelatedToPersonId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828164555_UnifyPhysicalPersonsAndFamilyLinks'
)
BEGIN
    INSERT INTO [RelatedPersons]
    (
        [Id], [FirstName], [LastName], [Residency], [JMBG], [PassportNumber], [FBAId],
        [GCCNumber], [GCCName], [RelationBasis], [RelationDescription],
        [SpecialRelationBasis], [DateFrom], [DateTo], [IsIdentifiedStaff],
        [ConnectedWithBank], [SpecialRelationshipWithBank], [SpecialContract],
        [MalusClawback], [DeclarationNoFamilyMembers], [Status], [IsActive],
        [CreatedAt], [CreatedBy], [ModifiedAt], [ModifiedBy], [VerifiedBy], [VerifiedAt],
        [FamilyRelationshipType], [RelatedToPersonId]
    )
    SELECT
        fm.[Id], fm.[FirstName], fm.[LastName], fm.[Residency], fm.[JMBG],
        fm.[PassportNumber], fm.[FBAId], N'0', N'Migrirani porodični zapis',
        N'PORODICNA_VEZA', fm.[RelationshipType], N'UZA_PORODICA',
        fm.[CreatedAt], NULL, 0, 1, 0, 0, 0, 1, N'Draft', fm.[IsActive],
        fm.[CreatedAt], fm.[CreatedBy], fm.[ModifiedAt], fm.[ModifiedBy], NULL, NULL,
        fm.[RelationshipType], COALESCE(fm.[ParentFamilyMemberId], fm.[RelatedPersonId])
    FROM [FamilyMembers] fm
    WHERE fm.[IsActive] = 1
      AND NOT EXISTS (SELECT 1 FROM [RelatedPersons] rp WHERE rp.[Id] = fm.[Id]);

    UPDATE [FamilyMembers]
    SET [IsActive] = 0,
        [ModifiedAt] = SYSUTCDATETIME(),
        [ModifiedBy] = N'UnifyPhysicalPersonsAndFamilyLinks'
    WHERE [IsActive] = 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828164555_UnifyPhysicalPersonsAndFamilyLinks'
)
BEGIN
    CREATE INDEX [IX_RelatedPersons_RelatedToPersonId] ON [RelatedPersons] ([RelatedToPersonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828164555_UnifyPhysicalPersonsAndFamilyLinks'
)
BEGIN
    ALTER TABLE [RelatedPersons] ADD CONSTRAINT [FK_RelatedPersons_RelatedPersons_RelatedToPersonId] FOREIGN KEY ([RelatedToPersonId]) REFERENCES [RelatedPersons] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828164555_UnifyPhysicalPersonsAndFamilyLinks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828164555_UnifyPhysicalPersonsAndFamilyLinks', N'10.0.3');
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

