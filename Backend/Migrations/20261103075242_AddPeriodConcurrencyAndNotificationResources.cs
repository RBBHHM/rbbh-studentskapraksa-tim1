using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodConcurrencyAndNotificationResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "PeriodLocks",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
            // Add new semantic templates without replacing operator-edited/historical labels.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.locked.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.locked.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.locked.title',N'Period zaključan' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.locked.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.locked.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.locked.title',N'Period locked' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.locked.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.locked.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.locked.title',N'Periode gesperrt' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.locked.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.locked.body',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.locked.body',N'Period {month}/{year} je zaključan. Izvršilac: {actor}.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.locked.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.locked.body',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.locked.body',N'Period {month}/{year} is locked. Actor: {actor}.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.locked.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.locked.body',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.locked.body',N'Die Periode {month}/{year} ist gesperrt. Ausgeführt von: {actor}.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.unlocked.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.unlocked.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.unlocked.title',N'Period otključan' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.unlocked.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.unlocked.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.unlocked.title',N'Period unlocked' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.unlocked.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.unlocked.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.unlocked.title',N'Periode entsperrt' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.unlocked.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.unlocked.body',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.unlocked.body',N'Period {month}/{year} je otključan. Izvršilac: {actor}.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.unlocked.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.unlocked.body',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.unlocked.body',N'Period {month}/{year} is unlocked. Actor: {actor}.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.unlocked.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.unlocked.body',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.unlocked.body',N'Die Periode {month}/{year} ist entsperrt. Ausgeführt von: {actor}.' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.requested.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.requested.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.requested.title',N'Zahtjev za otključavanje perioda' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.requested.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.requested.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.requested.title',N'Period unlock request' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.requested.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.requested.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.requested.title',N'Anfrage zur Entsperrung der Periode' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.requested.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.requested.body',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.requested.body',N'Podnosilac: {requester}' + NCHAR(10) + N'Period: {month}/{year}' + NCHAR(10) + N'Razlog: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.requested.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.requested.body',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.requested.body',N'Requester: {requester}' + NCHAR(10) + N'Period: {month}/{year}' + NCHAR(10) + N'Reason: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.requested.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.requested.body',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.requested.body',N'Antragsteller: {requester}' + NCHAR(10) + N'Periode: {month}/{year}' + NCHAR(10) + N'Grund: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.response.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.response.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.response.title',N'Dopuna zahtjeva za period' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.response.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.response.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.response.title',N'Period request response' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.response.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.response.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.response.title',N'Ergänzung zur Periodenanfrage' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.response.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.response.body',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.response.body',N'Podnosilac: {requester}' + NCHAR(10) + N'Period: {month}/{year}' + NCHAR(10) + N'Dopuna: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.response.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.response.body',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.response.body',N'Requester: {requester}' + NCHAR(10) + N'Period: {month}/{year}' + NCHAR(10) + N'Response: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.response.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.response.body',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.response.body',N'Antragsteller: {requester}' + NCHAR(10) + N'Periode: {month}/{year}' + NCHAR(10) + N'Ergänzung: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.rejected.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.rejected.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.rejected.title',N'Zahtjev za period odbijen' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.rejected.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.rejected.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.rejected.title',N'Period request rejected' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.rejected.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.rejected.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.rejected.title',N'Periodenanfrage abgelehnt' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.rejected.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.rejected.body',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.rejected.body',N'Period: {month}/{year}' + NCHAR(10) + N'Obradio: {actor}' + NCHAR(10) + N'Razlog: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.rejected.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.rejected.body',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.rejected.body',N'Period: {month}/{year}' + NCHAR(10) + N'Processed by: {actor}' + NCHAR(10) + N'Reason: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.rejected.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.rejected.body',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.rejected.body',N'Periode: {month}/{year}' + NCHAR(10) + N'Bearbeitet von: {actor}' + NCHAR(10) + N'Grund: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.needsinfo.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.needsinfo.title',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.needsinfo.title',N'Potrebna dopuna zahtjeva' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.needsinfo.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.needsinfo.title',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.needsinfo.title',N'More information required' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.needsinfo.title')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.needsinfo.title',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.needsinfo.title',N'Weitere Angaben erforderlich' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.period.needsinfo.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.needsinfo.body',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.period.needsinfo.body',N'Period: {month}/{year}' + NCHAR(10) + N'Obradio: {actor}' + NCHAR(10) + N'Pitanje: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.period.needsinfo.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.needsinfo.body',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.period.needsinfo.body',N'Period: {month}/{year}' + NCHAR(10) + N'Processed by: {actor}' + NCHAR(10) + N'Question: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.period.needsinfo.body')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.period.needsinfo.body',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.period.needsinfo.body',N'Periode: {month}/{year}' + NCHAR(10) + N'Bearbeitet von: {actor}' + NCHAR(10) + N'Frage: {message}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.role.request.submitted.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.role.request.submitted.detail',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.role.request.submitted.detail',N'Podnosilac: {requester}' + NCHAR(10) + N'Role: {roles}' + NCHAR(10) + N'Napomena: {note}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.role.request.submitted.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.role.request.submitted.detail',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.role.request.submitted.detail',N'Requester: {requester}' + NCHAR(10) + N'Roles: {roles}' + NCHAR(10) + N'Note: {note}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.role.request.submitted.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.role.request.submitted.detail',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.role.request.submitted.detail',N'Antragsteller: {requester}' + NCHAR(10) + N'Rollen: {roles}' + NCHAR(10) + N'Hinweis: {note}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.role.request.approved.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.role.request.approved.detail',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.role.request.approved.detail',N'Vaš zahtjev je odobren.' + NCHAR(10) + N'Role: {roles}' + NCHAR(10) + N'Napomena administratora: {decisionNote}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.role.request.approved.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.role.request.approved.detail',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.role.request.approved.detail',N'Your request was approved.' + NCHAR(10) + N'Roles: {roles}' + NCHAR(10) + N'Administrator note: {decisionNote}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.role.request.approved.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.role.request.approved.detail',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.role.request.approved.detail',N'Ihre Anfrage wurde genehmigt.' + NCHAR(10) + N'Rollen: {roles}' + NCHAR(10) + N'Hinweis des Administrators: {decisionNote}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='BS' AND ResourceKey=N'notifications.role.request.rejected.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.role.request.rejected.detail',1,SYSUTCDATETIME(),'migration',Id,1,'BS',N'notifications.role.request.rejected.detail',N'Vaš zahtjev je odbijen.' + NCHAR(10) + N'Role: {roles}' + NCHAR(10) + N'Napomena administratora: {decisionNote}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='EN' AND ResourceKey=N'notifications.role.request.rejected.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.role.request.rejected.detail',1,SYSUTCDATETIME(),'migration',Id,1,'EN',N'notifications.role.request.rejected.detail',N'Your request was rejected.' + NCHAR(10) + N'Roles: {roles}' + NCHAR(10) + N'Administrator note: {decisionNote}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                IF NOT EXISTS (SELECT 1 FROM CodeLists WHERE Kategorija='LanguagePacks' AND LanguageCode='DE' AND ResourceKey=N'notifications.role.request.rejected.detail')
                    INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik,DefinitionId,IsSystemRequired,LanguageCode,ResourceKey,ResourceLabel)
                    SELECT 'LanguagePacks',REPLACE(CONVERT(varchar(36),NEWID()),'-',''),N'notifications.role.request.rejected.detail',1,SYSUTCDATETIME(),'migration',Id,1,'DE',N'notifications.role.request.rejected.detail',N'Ihre Anfrage wurde abgelehnt.' + NCHAR(10) + N'Rollen: {roles}' + NCHAR(10) + N'Hinweis des Administrators: {decisionNote}' FROM CodeListDefinitions WHERE Name='LanguagePacks';
                UPDATE CodeListDefinitions SET Version = Version + 1 WHERE Name = 'LanguagePacks';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "PeriodLocks");
        }
    }
}
