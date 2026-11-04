using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class AddRuntimeLocalizationCodebooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fail before DDL on conflicting legacy keys; never delete or merge business values.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM CodeLists GROUP BY UPPER(LTRIM(RTRIM(Kategorija))), UPPER(LTRIM(RTRIM(Kod))) HAVING COUNT(*) > 1)
                    THROW 51004, 'Codebook normalized-key preflight failed; reconcile duplicates before deployment.', 1;
                """);
            migrationBuilder.AddColumn<Guid>(
                name: "DefinitionId",
                table: "CodeLists",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRequired",
                table: "CodeLists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "CodeLists",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceHelper",
                table: "CodeLists",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceKey",
                table: "CodeLists",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "Latin1_General_100_CI_AS");

            migrationBuilder.AddColumn<string>(
                name: "ResourceLabel",
                table: "CodeLists",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourcePlaceholder",
                table: "CodeLists",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRequired",
                table: "CodeListDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "CodeListDefinitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "CodeListDefinitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CodeListDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCategory",
                table: "CodeLists",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Kategorija])))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCode",
                table: "CodeLists",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Kod])))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeLists_DefinitionId",
                table: "CodeLists",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeLists_Kategorija_LanguageCode_ResourceKey",
                table: "CodeLists",
                columns: new[] { "Kategorija", "LanguageCode", "ResourceKey" },
                unique: true,
                filter: "[LanguageCode] IS NOT NULL AND [ResourceKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CodeLists_NormalizedCategory_NormalizedCode",
                table: "CodeLists",
                columns: new[] { "NormalizedCategory", "NormalizedCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CodeLists_CodeListDefinitions_DefinitionId",
                table: "CodeLists",
                column: "DefinitionId",
                principalTable: "CodeListDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Defaults are application-local names, not a DP_ naming requirement. Existing rows remain untouched.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM CodeListDefinitions WHERE Name = 'Languages')
                    INSERT CodeListDefinitions (Id,Name,Description,IsActive,CreatedAt,CreatedBy,IsSystemRequired,Version)
                    VALUES (NEWID(),'Languages',N'Supported application languages',1,SYSUTCDATETIME(),'migration',1,1);
                IF NOT EXISTS (SELECT 1 FROM CodeListDefinitions WHERE Name = 'LanguagePacks')
                    INSERT CodeListDefinitions (Id,Name,Description,IsActive,CreatedAt,CreatedBy,IsSystemRequired,Version)
                    VALUES (NEWID(),'LanguagePacks',N'Runtime labels, helpers and placeholders',1,SYSUTCDATETIME(),'migration',1,1);
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE UPPER(LTRIM(RTRIM(Kategorija)))='LANGUAGES' AND UPPER(LTRIM(RTRIM(Kod)))='BS')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired)
                    SELECT 'Languages','BS',N'Bosanski',1,SYSUTCDATETIME(),'migration',Id,1 FROM CodeListDefinitions WHERE Name='Languages';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE UPPER(LTRIM(RTRIM(Kategorija)))='LANGUAGES' AND UPPER(LTRIM(RTRIM(Kod)))='EN')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired)
                    SELECT 'Languages','EN',N'English',1,SYSUTCDATETIME(),'migration',Id,1 FROM CodeListDefinitions WHERE Name='Languages';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE UPPER(LTRIM(RTRIM(Kategorija)))='LANGUAGES' AND UPPER(LTRIM(RTRIM(Kod)))='DE')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired)
                    SELECT 'Languages','DE',N'Deutsch',1,SYSUTCDATETIME(),'migration',Id,1 FROM CodeListDefinitions WHERE Name='Languages';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'errors.request.invalid.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.invalid.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'errors.request.invalid.title',N'Zahtjev nije ispravan.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'errors.request.invalid.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.invalid.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'errors.request.invalid.title',N'The request is invalid.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'errors.request.invalid.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.invalid.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'errors.request.invalid.title',N'Die Anfrage ist ungültig.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'errors.request.invalid.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.invalid.detail',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'errors.request.invalid.detail',N'Provjerite unesene podatke i pokušajte ponovo.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'errors.request.invalid.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.invalid.detail',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'errors.request.invalid.detail',N'Check the supplied values and try again.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'errors.request.invalid.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.invalid.detail',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'errors.request.invalid.detail',N'Prüfen Sie Ihre Eingaben und versuchen Sie es erneut.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'errors.request.unavailable.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.unavailable.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'errors.request.unavailable.title',N'Zahtjev trenutno nije moguće završiti.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'errors.request.unavailable.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.unavailable.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'errors.request.unavailable.title',N'The request cannot be completed right now.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'errors.request.unavailable.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.unavailable.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'errors.request.unavailable.title',N'Die Anfrage kann derzeit nicht abgeschlossen werden.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'errors.request.unavailable.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.unavailable.detail',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'errors.request.unavailable.detail',N'Pokušajte ponovo. Ako se problem ponovi, podršci pošaljite navedeni ID.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'errors.request.unavailable.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.unavailable.detail',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'errors.request.unavailable.detail',N'Try again. If the problem persists, give support the supplied ID.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'errors.request.unavailable.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.request.unavailable.detail',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'errors.request.unavailable.detail',N'Versuchen Sie es erneut. Falls das Problem weiter besteht, geben Sie dem Support die angezeigte ID.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'errors.localization.invalid.key')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.localization.invalid.key',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'errors.localization.invalid.key',N'Novi ključ mora koristiti mala slova i semantičku dot notaciju.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'errors.localization.invalid.key')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.localization.invalid.key',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'errors.localization.invalid.key',N'New keys must use lowercase semantic dot notation.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'errors.localization.invalid.key')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.localization.invalid.key',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'errors.localization.invalid.key',N'Neue Schlüssel müssen kleingeschriebene semantische Punktnotation verwenden.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'errors.localization.invalid.resource')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.localization.invalid.resource',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'errors.localization.invalid.resource',N'Provjerite vrijednosti jezičkog resursa.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'errors.localization.invalid.resource')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.localization.invalid.resource',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'errors.localization.invalid.resource',N'Check the resource values.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'errors.localization.invalid.resource')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.localization.invalid.resource',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'errors.localization.invalid.resource',N'Prüfen Sie die Ressourcenwerte.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'errors.localization.language.unavailable')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.localization.language.unavailable',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'errors.localization.language.unavailable',N'Odabrani jezik nije dostupan.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'errors.localization.language.unavailable')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.localization.language.unavailable',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'errors.localization.language.unavailable',N'The selected language is unavailable.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'errors.localization.language.unavailable')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.localization.language.unavailable',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'errors.localization.language.unavailable',N'Die gewählte Sprache ist nicht verfügbar.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'errors.codelist.system.required')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.codelist.system.required',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'errors.codelist.system.required',N'Obavezni sistemski resurs nije moguće deaktivirati.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'errors.codelist.system.required')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.codelist.system.required',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'errors.codelist.system.required',N'A required system resource cannot be deactivated.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'errors.codelist.system.required')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'errors.codelist.system.required',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'errors.codelist.system.required',N'Eine erforderliche Systemressource kann nicht deaktiviert werden.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.role.request.submitted.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.submitted.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.role.request.submitted.title',N'Novi zahtjev za pristup' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.role.request.submitted.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.submitted.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.role.request.submitted.title',N'New access request' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.role.request.submitted.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.submitted.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.role.request.submitted.title',N'Neue Zugriffsanfrage' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.role.request.submitted.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.submitted.body',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.role.request.submitted.body',N'Novi zahtjev čeka pregled administratora.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.role.request.submitted.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.submitted.body',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.role.request.submitted.body',N'A new request is awaiting administrator review.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.role.request.submitted.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.submitted.body',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.role.request.submitted.body',N'Eine neue Anfrage wartet auf die Prüfung durch einen Administrator.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.role.request.approved.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.approved.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.role.request.approved.title',N'Zahtjev za pristup odobren' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.role.request.approved.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.approved.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.role.request.approved.title',N'Access request approved' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.role.request.approved.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.approved.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.role.request.approved.title',N'Zugriffsanfrage genehmigt' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.role.request.approved.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.approved.body',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.role.request.approved.body',N'Vaš zahtjev je odobren. Otvorite profil za trenutne dozvole.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.role.request.approved.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.approved.body',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.role.request.approved.body',N'Your request was approved. Open your profile to view current permissions.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.role.request.approved.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.approved.body',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.role.request.approved.body',N'Ihre Anfrage wurde genehmigt. Öffnen Sie Ihr Profil, um die aktuellen Berechtigungen anzuzeigen.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.role.request.rejected.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.rejected.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.role.request.rejected.title',N'Zahtjev za pristup odbijen' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.role.request.rejected.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.rejected.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.role.request.rejected.title',N'Access request rejected' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.role.request.rejected.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.rejected.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.role.request.rejected.title',N'Zugriffsanfrage abgelehnt' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.role.request.rejected.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.rejected.body',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.role.request.rejected.body',N'Vaš zahtjev je odbijen. Detalji su dostupni u profilu.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.role.request.rejected.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.rejected.body',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.role.request.rejected.body',N'Your request was rejected. Details are available in your profile.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.role.request.rejected.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',CONVERT(varchar(32),REPLACE(CONVERT(varchar(36),NEWID()),'-','')),N'notifications.role.request.rejected.body',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.role.request.rejected.body',N'Ihre Anfrage wurde abgelehnt. Details finden Sie in Ihrem Profil.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CodeLists_CodeListDefinitions_DefinitionId",
                table: "CodeLists");

            migrationBuilder.DropIndex(
                name: "IX_CodeLists_DefinitionId",
                table: "CodeLists");

            migrationBuilder.DropIndex(
                name: "IX_CodeLists_Kategorija_LanguageCode_ResourceKey",
                table: "CodeLists");

            migrationBuilder.DropIndex(
                name: "IX_CodeLists_NormalizedCategory_NormalizedCode",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "NormalizedCategory",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "NormalizedCode",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "DefinitionId",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "IsSystemRequired",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "ResourceHelper",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "ResourceKey",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "ResourceLabel",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "ResourcePlaceholder",
                table: "CodeLists");

            migrationBuilder.DropColumn(
                name: "IsSystemRequired",
                table: "CodeListDefinitions");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "CodeListDefinitions");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "CodeListDefinitions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CodeListDefinitions");
        }
    }
}
