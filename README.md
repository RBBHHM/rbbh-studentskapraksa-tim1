# RBBH Related Parties Register

Registar povezanih lica: fizička i pravna lica, porodične veze, import/export,
limiti, kapital, izvještaji, periodi, obavijesti, korisnici, šifrarnici i audit.

Native frontend je `src/Web.Blazor`, .NET10/RBI NuGet aplikacija s postojećim
API/BL/DL backendom. Claudeov vizuelni batch je integrisan i verifikovan;
verzionirani React izvor i njegovi wrapperi su uklonjeni. Prethodna referenca
ostaje dostupna u Git historiji. Lokalni testovi nisu odobrenje produkcijske isporuke.

## Identitet, dozvole i period

Keycloak autentificira identitet; aplikacijska baza dodjeljuje pet eksplicitnih
rola: FL, PL, limiti, kapital, admin. Provider role/permission claimovi ne daju
aplikacijske dozvole. Svaki autentificirani identitet bez eksplicitnog mapiranja
ima **virtualni Guest** read/download baseline, bez dodjeljivanja `gost` role.
Deaktivirani aplikacijski korisnik ostaje bez pristupa. Pražnjenje rola zadržava
zapis identiteta i status; aktivni korisnik postaje Guest. Self role-management,
self deaktivacija/brisanje i gubitak posljednjeg aktivnog Admina su zabranjeni.

Reporting READ/DOWNLOAD je dostupan autentificiranim identitetima koji nisu
deaktivirani; GENERATE/PERSIST zahtijeva DB KAPITAL_CREATE (Kapital/Admin), jer
kreira zapis. Backend ostaje autoritet. Sve period-osjetljive mutacije koriste
**jedan globalni period**, bez username/department zaključavanja. Native kontrole
su onemogućene kada je period zaključan ili nedostupan; backend423 štiti race.
Korisnička dopuna odgovora ima10–490 znakova, bez trunciranja ili promjene polja500.

Ljudske mutacije koriste isključivo `ClaimTypes.Email`; nedostajući email daje
siguran401 prije izmjene. Potvrđeni Keycloak subject je trajan identitetski ključ.
Postojeći placeholder nije dokaz veze. Nova EF migracija dodaje metapodatke
potvrde bez automatskog povezivanja produkcijskih korisnika. Prvo povezivanje je
isključeno dok vlasnik ne odobri provjereni email/subject postupak.
[Subject-link i global-period postupak](docs/IDENTITY-AND-PERIOD-HARDENING-2026-10-26.md).

## Struktura i lokalni razvoj

- `RelatedPartiesRegister` — backend i backend testovi.
- `src/Web.Blazor` — native interaktivni frontend, app-owned BS/EN.
- `tests/Web.Blazor.Tests` — frontend .NET testovi.
- `tests/Web.Blazor.Browser*` — Playwright i izolovani testni host.
- `RelatedPartiesRegister.slnx` — jedino održavano zajedničko rješenje za backend,
  native frontend i .NET testove. Stari backend-only .sln je uklonjen.

Potreban je .NET SDK10 i odobren feed s RBI paketima. ApiCore/Authentication
koriste `0.1.1-migration.1`; ostali RBI runtime paketi su0.1.0. Privatna objava
paketa je preduvjet, ne rezultat lokalnog cache restorea. Ne unositi tajne u Git.

Iz root direktorija, direktnim standardnim komandama:

```powershell
dotnet restore RelatedPartiesRegister.slnx --configfile <approved-config> --artifacts-path artifacts/local
dotnet build RelatedPartiesRegister.slnx --artifacts-path artifacts/local --no-restore -p:RunAnalyzers=true
dotnet test RelatedPartiesRegister.slnx --artifacts-path artifacts/local --no-restore
dotnet publish src/Web.Blazor/RBBH.RelatedPartiesRegister.Web.csproj -c Release --artifacts-path artifacts/local --no-restore -o artifacts/publish/web
dotnet publish RelatedPartiesRegister/RelatedPartiesRegister.csproj -c Release --artifacts-path artifacts/local --no-restore -o artifacts/publish/api
```

Za SQL testove postaviti `RBBH_TEST_SQLSERVER` na odobren **disposable** SQL Server
instance bez Initial Catalog; fixture kreira/briše samo svoju GUID bazu. Bez
varijable SQL testovi su SKIPPED, nikada PASS. Ne koristiti produkcijsku bazu.

U dva terminala, nakon sigurne konfiguracije SQL/SSO/backend URL-a:

```powershell
dotnet run --project RelatedPartiesRegister/RelatedPartiesRegister.csproj --artifacts-path artifacts/local --no-build --no-launch-profile -- --environment Development --urls http://127.0.0.1:5000
dotnet run --project src/Web.Blazor/RBBH.RelatedPartiesRegister.Web.csproj --artifacts-path artifacts/local --no-build --no-launch-profile -- --environment Development --urls http://127.0.0.1:5080
```

Backend učitava postojeću lokalnu .env konfiguraciju; nikada je ne kopirati u Git.
Startup ne izvršava migracije/seed. Native Development bez auth/API konfiguracije
prikazuje istinit signed-out ekran, bez lažnog korisnika ili anonimnog API proxyja.
[Native konfiguracija](src/Web.Blazor/README.md).

Browser provjera (izolovani HTTP backend/controller pipeline, ne produkcijski SSO):

```powershell
dotnet build tests/Web.Blazor.BrowserHost/Web.Blazor.BrowserHost.csproj --artifacts-path artifacts/local --no-restore
cd tests/Web.Blazor.Browser
npm ci
npx playwright install chromium
$env:BROWSER_ARTIFACTS_PATH = '../../artifacts/local'
$env:BROWSER_FIXTURE_ROLE = 'admin'
npm test -- --workers=1 --retries=0
```

Ponoviti za gost, kapital, PL, limiti i FL. Browser testovi uključuju Axe i375px;
ne zamjenjuju pregled Claudeovih vizuelnih izmjena ili stvarni SSO/UAT prihvat.

## Containeri i isporuka

```powershell
docker build -f src/Web.Blazor/Dockerfile -t rbbh-register-web:local src/Web.Blazor
docker build -f RelatedPartiesRegister/Dockerfile -t rbbh-register-api:local .
```

Tačno tri root workflowa: `build-and-push-image.yml`, `codeql.yml`,
`deploy-to-ocp.yml`. Nema nested workflowa ili IIS deploya. Build testira i gradi
native web/API odvojeno; deploy je ručan, s punim commit tagom i digestom.
[Pipeline konfiguracija i preduvjeti](docs/NATIVE-PIPELINE-2026-10-26.md).

System.Security.Cryptography.Pkcs ostaje9.0.21. NU1900/NU1902/NU1903 se ne skrivaju.
Stvarni SSO, odobren subject backfill, SQL/SMTP, shared cache/DataProtection,
privatni feed i OCP/circuit/certifikati ostaju deployment gateovi.
[Aktuelni handoff](docs/REACT-TO-BLAZOR-FINAL-HANDOFF.md).
