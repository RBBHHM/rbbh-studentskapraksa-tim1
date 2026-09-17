/* Idempotent SQL Server seed for RelatedPartiesRegDB.
   Safe to execute repeatedly in SSMS after the EF migrations have been applied. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Values TABLE
(
    Kategorija varchar(100), Kod varchar(50), Naziv nvarchar(200),
    Opis nvarchar(500), RedoslijedPrikaza int
);

INSERT INTO @Values (Kategorija, Kod, Naziv, Opis, RedoslijedPrikaza) VALUES
('VrstaLimita','MM',N'MM',N'Izvor: SATA',1),
('VrstaLimita','FXS',N'FXs',N'Izvor: SATA. Iskorištenost se po FBA ponderiše sa 1%.',2),
('VrstaLimita','FXD',N'FXD',N'Izvor: SATA',3),
('VrstaLimita','OVL',N'OVL',N'Izvor: manuelna evidencija',4),
('VrstaLimita','OVL_CUSTODY',N'OVL CUSTODY',N'Izvor: manuelna evidencija',5),
('VrstaLimita','SFL_NET',N'SFL Net',N'Izvor: manuelna evidencija',6),
('VrstaLimita','LG',N'LG',N'Izvor: manuelna evidencija',7),
('VrstaLimita','LT',N'LT',N'Izvor: manuelna evidencija',8),
('VrstaLimita','ST',N'ST',N'Izvor: manuelna evidencija',9),
('VrstaLimita','SPRL',N'SPRL',N'Izvor: manuelna evidencija',10),
('VrstaLimita','LEASING',N'LEASING',N'Izvor: manuelna evidencija',11),
('OsnovPovezanosti','ZOB-2-U-2',N'Zakon o bankama, član 2, paragraf u, odjeljak 2',N'Lice sa najmanje 5% učešća u banci ili članu bankarske grupe i članovi njegove uže porodice.',1),
('OsnovPovezanosti','ZOB-2-V-1',N'Zakon o bankama, član 2, paragraf v, odjeljak 1',N'Član bankarske grupe u kojoj je banka.',2),
('OsnovPovezanosti','ZOB-2-V-3',N'Zakon o bankama, član 2, paragraf v, odjeljak 3',N'Pravno lice u kojem banka ima kvalifikovano učešće.',3),
('OsnovPovezanosti','ZOB-2-V-4',N'Zakon o bankama, član 2, paragraf v, odjeljak 4',N'Pravno lice u kojem član uprave, nadzornog odbora ili prokurista banke, odnosno član njegove uže porodice, ima kvalifikovano učešće.',4),
('OsnovPovezanosti','ZOB-2-V-5',N'Zakon o bankama, član 2, paragraf v, odjeljak 5',N'Član nadzornog odbora, uprave banke, nosilac ključne funkcije, prokurista ili član njegove uže porodice.',5),
('OsnovPovezanosti','ZOB-2-V-7',N'Zakon o bankama, član 2, paragraf v, odjeljak 7',N'Član organa upravljanja i rukovođenja ili prokurista člana bankarske grupe i član njegove uže porodice.',6),
('OsnovPovezanosti','ZOB-2-V-8',N'Zakon o bankama, član 2, paragraf v, odjeljak 8',N'Lice sa značajnim uticajem na poslovanje banke ili lice u sukobu interesa.',7);

MERGE dbo.CodeLists WITH (HOLDLOCK) AS target
USING @Values AS source
ON target.Kategorija = source.Kategorija AND target.Kod = source.Kod
WHEN MATCHED THEN UPDATE SET
    Naziv = source.Naziv, Opis = source.Opis,
    RedoslijedPrikaza = source.RedoslijedPrikaza, Aktivan = 1,
    IzmijenjenDatum = @Now, IzmijenioKorisnik = 'business-seed'
WHEN NOT MATCHED THEN INSERT
    (Kategorija, Kod, Naziv, Opis, RedoslijedPrikaza, Aktivan, KreiranDatum, KreiraoKorisnik)
VALUES
    (source.Kategorija, source.Kod, source.Naziv, source.Opis, source.RedoslijedPrikaza, 1, @Now, 'business-seed');

UPDATE dbo.CodeLists
SET Aktivan = 0, IzmijenjenDatum = @Now, IzmijenioKorisnik = 'business-seed'
WHERE Kategorija = 'VrstaLimita'
  AND Kod NOT IN (SELECT Kod FROM @Values WHERE Kategorija = 'VrstaLimita');

COMMIT TRANSACTION;
