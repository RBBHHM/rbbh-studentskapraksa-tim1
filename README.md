# RBBH Related Parties Register

Poslovna aplikacija za vođenje Registra povezanih lica: fizička i pravna lica, porodične veze, limiti, regulatorni izvještaji, zaključavanje perioda, obavijesti i revizijski trag.

## Šta aplikacija omogućava

- kreiranje, pregled, izmjenu, verifikaciju i deaktiviranje fizičkih i pravnih lica;
- jedinstven unos zaposlenika i povezanih članova porodice u tabeli fizičkih lica, bez dupliranja identiteta;
- pregled stabla povezanosti za svako fizičko lice, uključujući zaposlenike i sve njihove porodične veze;
- Excel pregled i uvoz koji prije obrade provjerava format, broj, redoslijed i nazive kolona, a zatim svaki red;
- RBI-stilizovan Excel izvoz fizičkih lica, pravnih lica, limita i regulatornih izvještaja;
- evidenciju i praćenje raspoloživih limita te zasebnu Kapital stranicu za regulatorni i osnovni kapital;
- dnevne i mjesečne regulatorne izvještaje koji se odmah mogu preuzeti kao RBI-stilizovan Excel;
- zaključavanje perioda, zahtjev za otključavanje i obradu odgovora;
- korisničke obavijesti, kreiranje definicija i vrijednosti šifrarnika te razumljiv revizijski trag;
- upravljanje korisnicima sa višestrukim funkcionalnim pristupima, jedinstvenim emailom, deaktiviranjem i brisanjem;
- potpuno usklađen bosanski i engleski interfejs, light/dark temu i pristupačan dizajn.

Početna stranica daje hero pregled i bento kartice samo za funkcionalnosti kojima trenutni korisnik smije pristupiti.

## Četiri funkcionalna pristupa

Pristup nije vezan za sektor ili organizacionu jedinicu. Jedan korisnik može istovremeno imati jedan ili više od tačno ova četiri pristupa:

| Keycloak/tehnički naziv | Poslovno značenje |
|---|---|
| `physical-persons` | Fizička lica i porodične veze |
| `legal-persons` | Pravna lica |
| `limits` | Limiti |
| `regulatory-reporting` | Regulatorno izvještavanje i upravljanje periodima |

Promjena pristupa zamjenjuje prethodni skup odabranim skupom. Duplikati i druge role se ne mogu dodijeliti kroz aplikaciju.

## Struktura

- `RelatedPartiesRegister/RelatedPartiesRegister.csproj` — glavni .NET/OCP servis.
- `RelatedPartiesRegister/` — sav backend kod, konfiguracija, Dockerfile i backend testovi.
- `src/Web` — React aplikacija, RBI dizajn sistem, lokalizacija i tipizirani API pozivi.
- `RelatedPartiesRegister/UnitTests` i `RelatedPartiesRegister/IntegrationTests` — automatske provjere backenda.
- `.github/workflows` — standardni OCP build/deploy tok za backend.

## Lokalno pokretanje

Preduvjeti su .NET SDK 10, Node.js i pnpm 9. Docker nije potreban.

```powershell
# Terminal 1
cd src/Web
pnpm install
pnpm dev:api

# Terminal 2, isti src/Web folder
pnpm dev
```

Otvorite `http://127.0.0.1:8080`. API je na `http://127.0.0.1:5000`. `dev:api` postavlja Development režim; bez lokalnih tajni automatski koristi seedovanu InMemory bazu i lokalnog korisnika sa sva četiri pristupa.

`pnpm dev:api` prvo koristi SQL Server kada su njegove vrijednosti definisane u
`.env`. Ako lokalni Development ne može uspostaviti SQL vezu, npr. zbog rada
izvan bankarske mreže ili bez VPN-a, ispisuje jasno upozorenje i pokreće
seedovanu InMemory bazu. Ako SQL uopšte nije konfigurisan, odmah se koristi
InMemory. UAT i produkcija nikada ne koriste ovaj fallback. Ako Keycloak nije
konfigurisan, prikazuje se upozorenje i koristi se lokalna autentifikacija.

Seed pokriva nacrt, verificiran i odbijen zapis, rezidenta i nerezidenta, porodične veze, prekoračen limit, zaključan period, zahtjeve za otključavanje, obavijesti, šifarnike, izvještaje i sva četiri pristupa. InMemory podaci nestaju nakon gašenja API-ja.

## Najvažnija pravila unosa

- Rezidentno fizičko lice mora imati validan JMBG od 13 cifara; nerezident mora imati i pasoš i numerički FBA ID do 10 cifara.
- Ime i prezime prihvataju slova, razmak, crticu i apostrof; JMBG provjerava datum rođenja i kontrolnu cifru, a pasoš 5–20 dozvoljenih znakova.
- JMBG, broj pasoša i FBA ID provjeravaju se već u prvom koraku forme i ponovo na servisu i jedinstvenom SQL indeksu, pa aktivni duplikat nije moguće sačuvati.
- Odabir člana uže porodice automatski postavlja poslovna DA/NE pravila; promjena na drugi osnov čisti te vrijednosti i zahtijeva novi svjestan odabir.
- Fizičko lice mora imati GCC, osnov i opis povezanosti, osnov posebnog odnosa te početni i završni datum.
- Rezidentno pravno lice mora imati porezni broj od 13 cifara; nerezident mora imati numerički FBA ID do 10 cifara.
- Pravno lice mora imati naziv, GCC, osnov i opis povezanosti te početni datum.
- Sistem prikazuje poslovne poruke uz konkretno polje; interne .NET/JSON poruke i tehnički detalji se ne prikazuju korisniku.
- Nakon uspješne izmjene, verifikacije, deaktiviranja ili zaključavanja lista i status se odmah osvježavaju; ručno dugme za osvježavanje nije potrebno.
- Padajući meniji za poslovne vrijednosti pune se iz šifrarnika u bazi; frontend zadržava samo siguran prikazni fallback ako referentni endpoint privremeno nije dostupan.

## SQL Server i Keycloak

```powershell
$env:Database__ServerName='localhost\SQLEXPRESS'
$env:Database__Name='RPR_Local'
$env:Database__IntegratedSecurity='true'
```

Baza se može pregledati kroz SSMS. Ovo je jedini konfiguracijski oblik; naziv
baze je varijabla i nije zakucan u C# kodu. OCP koristi isti skup, uz
`Database__IntegratedSecurity=false` i `Database__User`/`Database__Password`
iz Secreta. Development automatski primjenjuje commitovane EF migracije nad
lokalnim SQL Serverom. UAT i produkcija ih ne izvršavaju iz OCP aplikacije:
odobreni idempotentni SQL prolazi kroz centralni `rbbh-coredb-microsvcdb-cac`
workflow.

API validira tokene pomoću `KeycloakSettings__Issuer`, opcionog
`KeycloakSettings__PublicIssuer` i `KeycloakSettings__Audience`. Realm i admin
base URL izvode se iz Issuer vrijednosti. React prijava koristi javne runtime
vrijednosti `KEYCLOAK_URL`, `KEYCLOAK_REALM` i `KEYCLOAK_CLIENT_ID` uz PKCE.
To nisu tajne; client secret se nikada ne stavlja u IIS/React paket. Upravljanje
korisnicima opciono koristi zaseban service-account client ID/secret na backendu.

U Keycloaku unaprijed kreirajte četiri realm role iz gornje tabele. Aplikacija ignoriše ostale realm role pri prikazu i dodjeli funkcionalnih pristupa.

Za odvojeni IIS frontend postavite svaki dozvoljeni URL kroz `Cors__AllowedOrigins__N`. U produkciji nema automatski dozvoljenih origin-a niti wildcard CORS-a. Reverse-proxy zaglavlja se obrađuju prije HTTPS preusmjerenja, a `AllowedHosts` treba postaviti na stvarni API hostname.

## Provjera i isporuka

```powershell
dotnet build RelatedPartiesRegister/RelatedPartiesRegister.sln --configuration Release
dotnet test RelatedPartiesRegister/RelatedPartiesRegister.sln --configuration Release
cd src/Web
pnpm localization:validate
pnpm lint
pnpm build
pnpm publish:iis
```

Backend ide na OCP kroz workflowe, frontend kao statički IIS ZIP. U samoj aplikaciji je na dnu sidebara dostupan role-aware **Korisnički vodič**: prikazuje poslovna pravila, statuse i tokove samo za module kojima prijavljeni korisnik ima pristup. Više: [arhitektura](docs/architecture/README.md), [lokalni razvoj](docs/development/LOCAL-DEVELOPMENT.md), [SQL Server i migracije](docs/database/SQL-SERVER.md), [bankarski DB change workflow](docs/database/DB-CHANGE-WORKFLOW.md), [varijable](docs/configuration/ENVIRONMENT-VARIABLES.md), [konvencije](docs/CONVENTIONS.md), [production-readiness](docs/PRODUCTION-READINESS.md) i [deployment](docs/deployment/OCP-IIS.md).

GitHub Environment koristi `PROJECT_PATH=RelatedPartiesRegister`, `SOLUTION_PATH=RelatedPartiesRegister.csproj` i `NUGET_CONFIG_PATH=nuget.config`.
