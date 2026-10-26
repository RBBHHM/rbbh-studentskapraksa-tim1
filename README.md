# RBBH Related Parties Register

Registar povezanih lica: fizička i pravna lica, porodične veze, import/export,
limiti, kapital, izvještaji, periodi, obavijesti, korisnici, šifrarnici i audit.

Native kandidat je `src/Web.Blazor`, generisan iz `rbbh-webapp` sa auth,
authorization i localization opcijama. Koristi RBI NuGet pakete i postojeći
backend; backend nije prepisan. Lokalna parity provjera nije produkcijski cutover.
`src/Web` ostaje **MIGRATION_REFERENCE** dok se ne potvrde stvarni deployment
potrošači, prihvat novog hosta i rollback.

## Identitet i dozvole

Keycloak je izvor identiteta. Aplikacijska baza je autoritet za role/dozvole;
proizvoljne provider role ili permission claims ne daju aplikacijski pristup.
Stari opis sa četiri direktne Keycloak role je zastario.

| Trenutna DB rola | Namjena |
|---|---|
| `gost` | postojeći osnovni pregled/preuzimanje |
| `FL` | mutacije fizičkih lica i povezanih porodičnih podataka |
| `PL` | mutacije pravnih lica |
| `limiti` | mutacije limita |
| `kapital` | kapital, generisanje izvještaja i postojeće period API dozvole |
| `admin` | privilegovana administracija i poslovne dozvole |

Izvještaje smiju pregledati/preuzeti poznati aktivni aplikacijski korisnici s
Guest baseline pristupom ili jačom DB rolom. Nepoznat/inaktivan autentificirani
identitet nije Guest. Generisanje
trajno kreira zapis: zahtijeva DB `KAPITAL_CREATE` (Kapital/Admin), nije read-only.
Admin stranice imaju postojeći Admin guard; period API zadržava zaseban
CapitalRead/Write ugovor. Detalji: [authorization matrix](docs/BLAZOR-MIGRATION-AUTHORIZATION.md).

Backend odbija vlastitu deaktivaciju/brisanje i gubitak posljednjeg aktivnog
Admin pristupa. Korisnička dopuna odgovora za otključavanje ima 10–490 znakova;
nepromijenjeni prefiks i tekst staju u postojeće SQL polje od500, bez trunciranja.
GCC nije dio trenutnog proizvoda. Porodično stablo je pregled; izmjena veza ide
kroz postojeću formu fizičkog lica, ne kroz izmišljeni zasebni CRUD.

## Struktura

- `src/Web.Blazor` — native interaktivni .NET10 frontend, BS/EN RBI.Localization.
- `RelatedPartiesRegister` — postojeći API/BL/DL i backend testovi.
- `tests/Web.Blazor.Tests` — frontend/transport/configuration regresije.
- `tests/Web.Blazor.Browser*` — Playwright i izolovani testni host; nisu deployment.
- `src/Web` — zadržana React referenca, ne native quick start.
- `RelatedPartiesRegister.Migration.slnx` — zajednička lokalna provjera migracije.

## Lokalni native razvoj

Potreban je .NET SDK10 i odobren NuGet feed/cache sa RBI paketima. Ne unositi
tajne ili lične putanje u source. ApiCore/Authentication koriste odobrenu
transport verziju `0.1.1-migration.1`; ostali runtime RBI paketi su `0.1.0`.

```powershell
dotnet restore RelatedPartiesRegister.Migration.slnx --configfile <approved-config> --packages artifacts/packages --artifacts-path artifacts/migration
dotnet build RelatedPartiesRegister.Migration.slnx --artifacts-path artifacts/migration --no-restore
dotnet test RelatedPartiesRegister.Migration.slnx --artifacts-path artifacts/migration --no-restore
dotnet run --project src/Web.Blazor/RBBH.RelatedPartiesRegister.Web.csproj --artifacts-path artifacts/migration --no-launch-profile -- --environment Development
```

Bez auth konfiguracije native Development prikazuje istinit signed-out ekran:
nema izmišljenog korisnika, login handlera ni anonimnog API proxyja. Za stvarno
povezivanje slijediti [native README](src/Web.Blazor/README.md). Testni host ima
sintetički identitet i jedinstvenu InMemory bazu, bez SQL/SSO/SMTP kontakta.

Backend startup ne pokreće migracije ni seed. Konfigurisan SQL ili Keycloak ne
smije se zamijeniti lažnim uspješnim odgovorom. Development fallback nije UAT ili
produkcijska strategija. Povjerljivi OIDC client, HTTPS, stvarno mapiranje
identiteta, odobren zajednički cache i DataProtection zahtijevaju prihvat okruženja.

## Isporuka i dokazi

Native frontend je server-hosted Blazor, **nije** stari statički IIS ZIP.
Lokalni candidate/manifest/ZIP priprema `scripts/Publish-BlazorCandidate.ps1`;
objavljeni DLL provjerava `scripts/testing/Test-BlazorPublished.ps1`.
Ne aktivirati postojeće legacy IIS/pnpm skripte kao novi native deploy.
Postojeći root OCP workflowi nisu automatski promijenjeni u frontend workflow.

- [Aktuelni handoff i release blokade](docs/REACT-TO-BLAZOR-FINAL-HANDOFF.md)
- [Module/flow parity](docs/BLAZOR-MIGRATION-PARITY.md)
- [React retirement i deployment postupak](docs/BLAZOR-MIGRATION-RETIREMENT-AND-DEPLOYMENT.md)
- [Završna sigurnosna provjera](docs/BLAZOR-MIGRATION-COMPLETION-SECURITY.md)

Stvarna SSO/SQL/OCP provjera, shared-state/multi-pod ponašanje, certifikati,
injekcija tajni i konačni cutover ostaju van lokalnog odobrenja. Ne mijenjati
kanonski produkcijski frontend niti uklanjati rollback referencu prije prihvata.
