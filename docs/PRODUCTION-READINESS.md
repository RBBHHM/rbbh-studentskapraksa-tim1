# Production-readiness pravila

Ovaj dokument bilježi obavezna tehnička pravila koja su ugrađena u aplikaciju i koja deployment mora sačuvati.

## Sigurnost i konfiguracija

- Keycloak prijava koristi Authorization Code + PKCE u browseru. Client secret nikada ne ide u frontend.
- Keycloak administracija koristi zaseban povjerljivi service-account klijent i OAuth `client_credentials`; administratorska korisnička lozinka se ne koristi.
- Service account dobija samo potrebne `realm-management` ovlasti za korisnike i role.
- Audit zapise kreira isključivo server u okviru poslovnih operacija. Ne postoji javni endpoint za ručno kreiranje audit zapisa.
- Produkcijski CORS nema wildcard ni automatski dozvoljene origin-e. Svaki IIS URL se unosi kroz `Cors__AllowedOrigins__N`.
- `AllowedHosts` se na serveru ograničava na stvarni API hostname.
- OCP/IIS proxy mora ukloniti ili prepisati dolazna `X-Forwarded-*` zaglavlja i aplikaciji proslijediti najviše jedan pouzdan proxy hop.
- Tajne se isporučuju kroz OCP Secret/zaštićene varijable, nikada kroz `appsettings.json`, `.env.example` ili frontend ZIP.

## Podaci i integracije

- SQL Server je produkcijska baza. InMemory baza i seed postoje samo u Development režimu.
- Soft-delete zapisi se ne vraćaju u aktivne poslovne liste.
- Liste ograničavaju veličinu stranice na najviše 200 zapisa.
- SQL Server pretrage ne pozivaju `ToLower()` nad indeksiranim kolonama; oslanjaju se na case-insensitive kolaciju i `LIKE`.
- EF migracije koriste repo-lokalni `dotnet-ef` iste verzije kao EF Core runtime: prvo `dotnet tool restore`, zatim `dotnet tool run dotnet-ef ...`.

## Pouzdanost i performanse

- Neočekivane greške vraćaju siguran `ProblemDetails` s trace ID-em; interni exception i cijeli `HttpContext` se ne šalju korisniku niti se serijalizuju u strukturirani log.
- Email pozivi se čekaju; greške se ne gube kroz fire-and-forget taskove. Za veći produkcijski obim sljedeći korak je transactional outbox.
- API i download pozivi imaju timeout i pravilno uklanjaju abort listenere.
- Teže poslovne stranice učitavaju se tek pri otvaranju, a vendor/runtime chunkovi imaju content hash radi dugotrajnog browser cachea.
- Backend log nivo je `Warning`; isti događaj se ne zapisuje kroz dva konzolna sinka.

## Obavezna provjera prije isporuke

```powershell
dotnet build RelatedPartiesRegister/RelatedPartiesRegister.sln --configuration Release
dotnet test RelatedPartiesRegister/RelatedPartiesRegister.sln --configuration Release
dotnet list RelatedPartiesRegister/RelatedPartiesRegister.sln package --vulnerable --include-transitive

cd src/Web
pnpm localization:validate
pnpm lint
pnpm test
pnpm audit --prod
pnpm build
```
