# Promjene baze kroz RBBH SQL workflow

Ovaj dokument prevodi centralni RBBH Operation Manual na potrebe aplikacije **RBBH Registar povezanih lica** i baze **RelatedPartiesRegDB**.

## Ko je odgovoran za šta

- Aplikacijski repozitorij sadrži EF Core model, migraciju i provjeren idempotentni SQL artefakt.
- `rbbh-coredb-microsvcdb-cac` je centralni repozitorij iz kojeg se SQL odobrava i izvršava na UAT/produkciji.
- OCP aplikacija u UAT/produkciji **ne izvršava DDL automatski**. Baza mora biti ažurirana kroz odobreni SQL tok prije ili zajedno s novom verzijom API-ja.
- Lokalni Development može automatski primijeniti EF migracije radi brzog rada developera.

## Kratki operativni postupak

1. U aplikacijskom repozitoriju izmijeniti model i napraviti EF migraciju.
2. Pokrenuti backend unit i integration testove te provjeriti migraciju na kopiji baze ili praznoj testnoj bazi.
3. Generisati idempotentni SQL za tačno jednu promjenu i pregledati ga. DML mora biti u transakciji s `TRY/CATCH`, `ROLLBACK` i `THROW` zaštitom.
4. Klonirati centralni repozitorij `rbbh-coredb-microsvcdb-cac`, preći na `UAT`, uraditi `fetch/pull` i napraviti novu feature granu iz ažurnog `UAT`.
5. SQL smjestiti u stvarni folder baze prema postojećoj strukturi repozitorija:
   - promjena tabele/indeksa/FK: `<db_folder>/schema/tables/`
   - čisti INSERT/UPDATE/DELETE: `<db_folder>/schema/DML/`
   - view, procedura, funkcija ili trigger u odgovarajući folder.
6. Pregledati diff, napraviti jasan commit, push i PR prema `UAT` grani. Sačekati odobrenje i merge.
7. Kreirati GitHub Issue iz propisanog templatea i popuniti:
   - link/commit SQL fajla koji se izvršava;
   - povezani PR;
   - `db_name`: ciljna baza `RelatedPartiesRegDB` (ako DBA projekt koristi trodijelni format, preuzeti tačnu vrijednost iz postojećih microsvcdb issuea);
   - `Depend`: prethodni issuei, ako postoje;
   - kratak razlog promjene, očekivani rezultat, redoslijed deploya i rollback napomena.
8. Issue automatski ulazi u `Active`. Ručno ga prebaciti u `GO-UAT` tek kada su PR i provjere završeni.
9. Nakon automatskog izvršenja pregledati komentar: uspjeh vodi u `QA`, neuspjeh vraća u `Active`.
10. U QA provjeriti shemu, broj prenesenih redova, API health i funkcionalni scenario. Zatim pratiti `GO-LIVE` → `GO-LIVE-executing` → `Close`.

Za nejasnu vrijednost `db_name`, prava nad bazom ili problem automatizacije kontakt je `it-dba@raiffeisengroup.ba`.

## Trenutna promjena: jedinstvena evidencija fizičkih lica

Pripremljeni artefakti:

- EF migracija `20260828164555_UnifyPhysicalPersonsAndFamilyLinks`;
- SQL `docs/database/changes/20260828-unify-physical-persons-and-family-links.sql`.

SQL radi sljedeće u jednoj zaštićenoj transakciji:

1. dodaje samoreferentnu vezu `RelatedToPersonId` i šifru `FamilyRelationshipType` u `RelatedPersons`;
2. postojeće aktivne `FamilyMembers` redove prenosi u `RelatedPersons` uz očuvanje njihovih ID-eva;
3. postojeću hijerarhiju prevodi u vezu fizičko-lice → fizičko-lice;
4. stare redove samo deaktivira radi kontrolisanog rollbacka;
5. dodaje indeks, strani ključ i EF migration-history zapis.

## Kontrole za UAT

Prije izvršenja zabilježiti broj aktivnih redova u `RelatedPersons` i `FamilyMembers`. Poslije izvršenja provjeriti:

- broj novih fizičkih lica jednak je prethodnom broju aktivnih članova porodice;
- svaki preneseni red ima `SpecialRelationBasis = 'UZA_PORODICA'`;
- svaki preneseni red ima popunjen `RelatedToPersonId` i `FamilyRelationshipType`;
- nema samoveza niti veza prema nepostojećem fizičkom licu;
- stari `FamilyMembers` redovi su neaktivni;
- API vraća sva fizička lica u jednoj listi i forma pravilno zaključava propisane DA/NE vrijednosti.
