# Testni podaci i scenariji — Registar povezanih lica

Dokument pokriva lokalni InMemory/SQL Server seed, UI, validacije i glavne API tokove. ID-jeve za izmjene uzeti iz odgovarajućeg GET odgovora; nisu hardkodirani.

## Pokretanje i identitet

- API: `src/Web/pnpm dev:api` ili korijenski `scripts/dev-api.cmd`; port 5000.
- Web: u `src/Web` pokrenuti `pnpm dev`; port 8080.
- Bez podešenog Keycloaka aplikacija radi u lokalnom fallback režimu i ispisuje warning.
- Seed korisnici: `admin1` (sva četiri poslovna pristupa), `user1` (Pravna lica + Limiti), `inactive1` (neaktivan).
- Keycloak lozinke postoje samo u lokalnoj Keycloak konfiguraciji; ne čuvati produkcijske tajne u ovom dokumentu.

## Seed podaci za provjeru

- Fizička lica: Amina Hadžić / zaposlenik / rezident / sintetički JMBG `0101990170003` / Verified; Marko Kovač / zaposlenik / nerezident / pasoš `P-DEMO-2026` / Draft; Lejla Testić / zaposlenik / `P-REJECT-01` / Rejected.
- Uža porodica u istoj tabeli fizičkih lica: Emir Hadžić (bračni partner Amine) i Ana Kovač (dijete Marka). Oba zapisa imaju `UZA_PORODICA`, popunjenu vezu prema postojećem licu i zaključane propisane DA/NE vrijednosti.
- Pravna lica: `RBI Poslovni partner d.o.o.`, porezni broj `4200000000001`, Verified; `International Partner GmbH`, FBA `2002`, Draft.
- Limiti: Ukupna izloženost 1.000.000/425.000; Interni operativni limit 500.000/125.000.
- Izvještaji: DAILY i MONTHLY te jedan probijeni EUR limit.
- Periodi: tekući otključan; prethodni zaključan; PENDING i REJECTED zahtjev za otključavanje.
- Šifrarnici: TipLica, OsnovPovezanosti, VrstaLimita, Status, Srodstvo i OsnovPosebnogOdnosa.

## Validni unosi

### Fizičko lice

`Test Korisnik`, rezident, sintetički JMBG `1506990123459`, osnov povezanosti `ZOB-2-V-5`, datum od danas, datum do za godinu. Za nerezidenta izostaviti JMBG i koristiti jedinstven pasoš `P-TEST-2026-01`.

### Pravno lice

`Test Partner d.o.o.`, rezident, jedinstveni i kontrolno ispravan 13-cifreni porezni broj, osnov `VL`, datum od danas. Za nerezidenta koristiti FBA ID `FBA-TEST-001`.

### Limit

Naziv `Test limit`, tip `INT`, iznos 100000 i utilizacija 25000. Raspoloživi limit mora biti izveden. Zatim na zasebnoj stranici Kapital postaviti regulatorni kapital 500000 i osnovni kapital 400000; vrijednosti ostaju u istom zapisu limita u bazi.

### Korisnik

Username `test.korisnik`, ime `Test`, prezime `Korisnik`, e-mail `test.korisnik@raiffeisengroup.ba`; dodijeliti jednu ili više od tačno četiri poslovne oblasti.

## Negativni scenariji

- Rezident bez JMBG-a, pogrešna dužina/kontrolna cifra JMBG-a ili duplikat.
- Nerezident bez pasoša; istovremeni JMBG i pasoš koji krše tip rezidentnosti.
- Prazno ime/prezime/naziv, samo razmaci, pretjerana dužina i nedozvoljeni znakovi.
- Datum do prije datuma od; negativni novčani iznosi; utilizacija iznad limita gdje pravilo to zabranjuje.
- Duplikat poreznog broja/FBA ID-a; e-mail bez `@raiffeisengroup.ba`; duplikat e-maila ili usernamea.
- Brisanje šifrarnika/vrijednosti u upotrebi mora vratiti 409 i jasnu poruku.
- Random, prazni, prevelik ili pogrešno strukturiran Excel mora navesti grešku po redu/koloni; ne smije prijaviti samo “0 uvezeno”.

## API i poslovni tokovi

1. GET liste/detalja za `legal-entities`, `related-persons`, `limiti`, `code-lists`, `users`, `reports`, `period-lock`, `audit-logs`.
2. POST validnog zapisa -> 201/200; lista se automatski osvježava bez Refresh dugmeta.
3. PUT izmijenjenog komentara i ostalih polja -> vrijednost ostaje nakon ponovnog GET-a.
4. VERIFY Draft zapisa -> status postaje Verified i verify akcija nestaje.
5. DELETE -> potvrda kroz RBI dialog, zapis nestaje; ponovljeni DELETE daje kontrolisan 404.
6. Član uže porodice: kreirati kroz glavnu formu fizičkih lica; provjeriti automatske DA/NE vrijednosti, obaveznu povezanu osobu, obavezno srodstvo i zabranu samoveze.
7. Period: lock, request-unlock, request-info, respond, reject/unlock; svaka tranzicija mijenja status i audit.
8. Izvještaji: generiši dnevni/mjesečni, zatim preuzmi generisani fajl i export jednog/svih klijenata.
9. Users: dodijeli više pristupa, ukloni pristup, deaktiviraj/reaktiviraj i trajno izbriši dozvoljeni testni nalog.
10. Import: preview/validacija prije upisa, parcijalne greške po redovima, potvrda samo validnih redova prema poslovnom pravilu.
11. Excel export: preuzeti fizička lica, pravna lica, limite i regulatorne izvještaje; otvoriti `.xlsx` i provjeriti RBI zaglavlje, filtere, datume i broj redova.
12. Stablo povezanosti: odabrati zaposlenika Amina Hadžić i provjeriti da je Emir Hadžić prikazan kao bračni partner; odabrati Marka i provjeriti dijete Anu.
13. Promijeniti osnov posebnog odnosa s `UZA_PORODICA` na drugi osnov: sva ranije automatski postavljena DA/NE polja moraju ostati bez odabira dok korisnik ne unese nove vrijednosti.
14. Unijeti postojeći JMBG, pasoš ili FBA ID u prvom koraku: inline greška mora se pojaviti prije završnog čuvanja; servis i SQL jedinstveni indeks ostaju završna zaštita.

## Automatizovani testovi

- `dotnet test RelatedPartiesRegister/RelatedPartiesRegister.sln --configuration Release`: 191 unit + 52 integration testa.
- `cd src/Web; pnpm test; pnpm build`: CSS/logo/surface ugovor i production bundle.
- Posebno su pokriveni JMBG/porezni broj, korisnički e-mail, CRUD, izvještaji, Excel export, servisni rezultati i HTTP integracija.
