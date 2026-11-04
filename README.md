# RBBH Related Parties Register

Register of related parties (individuals, legal entities, limits, capital, regulatory reports) for Raiffeisen Bank Bosnia and Herzegovina.
A Blazor Server frontend and an ASP.NET Core API on .NET 10. Keycloak provides identity; application permissions live in the database.
The API is the authority for user safety, period locking, validation and report persistence.

New to the project? Start with [docs/ONBOARDING.md](docs/ONBOARDING.md).

## Contents
- [Architecture](#architecture)
- [Repository layout](#repository-layout)
- [Running locally](#running-locally)
- [Configuration](#configuration)
- [Authorization](#authorization)
- [Database](#database)
- [Testing](#testing)
- [Build, delivery and operations](#build-delivery-and-operations)
- [Release blockers](#release-blockers)
- [Requirements and coverage](#requirements-and-coverage)

## Architecture
```text
Browser -> Blazor Server (Frontend) -> HTTPS JSON -> ASP.NET Core API (Backend) -> EF Core -> SQL Server
                |                                          |
                +------------- Keycloak (OIDC) ------------+
```
- **Frontend** renders the screens and tracks UI state. Business rules are validated again by the API; hiding a menu entry is never a security control.
- **Backend** applies business rules, authorization, period locks and audit. It is the only component that reaches the database.
- **Keycloak** issues tokens. The frontend sends the bearer token to the API, which validates signature and claims.
- **Code lists** (`CodeLists`, `CodeListDefinitions`) supply every dropdown and multi-select. Business option lists must not be hardcoded: a guard test fails the build when a literal option or a new static business list appears.
- **Localization**: all visible text uses resource keys from `Frontend/Localization/bs.json` and `en.json`. A guard test fails the build on literal visible text, on unknown keys and on a BS/EN key mismatch.
- **Errors** are returned as safe `ProblemDetails` with a trace ID; technical detail goes only to the log. Logs carry correlation IDs and must never contain tokens, passwords or client secrets.

## Repository layout
```text
Backend/    RelatedPartiesRegister.csproj: API, services, persistence
Frontend/   RBBH.RelatedPartiesRegister.Web.csproj: Components, Api clients, Security, wwwroot
  Platform/ seven RBI runtime source projects (design system, forms, localization, authentication ...)
tests/      Backend.UnitTests, Backend.IntegrationTests, Web.Blazor.Tests,
            Web.Blazor.BrowserHost (test-only seeded host), Web.Blazor.Browser (Playwright + Axe)
docs/       ONBOARDING.md, POPRAVKE.txt (requirements), FRONTEND-COVERAGE.md, database/changes (SQL)
.github/    build, CodeQL and OCP deployment workflows
```
The RBI libraries are tracked source under `Frontend/Platform` and referenced through `ProjectReference`; no RBI package feed is needed. See `Frontend/Platform/SOURCE.md`.

## Running locally
Prerequisite: .NET SDK 10.

```powershell
dotnet restore RelatedPartiesRegister.slnx
dotnet build RelatedPartiesRegister.slnx
dotnet test RelatedPartiesRegister.slnx
```

**A. Seeded demo (no SQL, Keycloak or mail).** The test-only BrowserHost uses isolated data and a synthetic identity. It listens on loopback only and is never deployed.
```powershell
$env:BROWSER_FIXTURE_ROLE = "admin"   # admin | gost | kapital | PL | limiti | FL
dotnet run --project tests/Web.Blazor.BrowserHost/Web.Blazor.BrowserHost.csproj --no-launch-profile
```
Open http://127.0.0.1:5273/app.

**B. Real application.** Copy `.env.example` to `Backend/.env`, review it, then:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"; $env:Email__Provider = "demo"
dotnet run --project Backend/RelatedPartiesRegister.csproj --no-launch-profile --urls http://localhost:5000
```
Without SQL settings, Development uses an empty InMemory store. Startup never applies migrations or seeds a real database.
In a second terminal point the frontend at the API and set the identity client through user-secrets (never in tracked files):
```powershell
$p = "Frontend/RBBH.RelatedPartiesRegister.Web.csproj"
dotnet user-secrets set "Apis:BaseUrl" "http://localhost:5000" --project $p
dotnet user-secrets set "Authentication:Authority" "<realm url>" --project $p
dotnet user-secrets set "Authentication:ClientId" "<client id>" --project $p
dotnet user-secrets set "Authentication:ClientSecret" "<client secret>" --project $p
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project $p --no-launch-profile --urls http://localhost:5080
```
Opening any `/app` page signed out goes straight to the Keycloak login form. Blank identity settings give a signed-out host that is not the full demo.

## Configuration
Use `__` for nested environment keys. Secrets stay outside Git. The backend loads `Backend/.env`; the frontend does not (use user-secrets).

**Backend**

| Variable | Purpose |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` allows local fallbacks; Production fails closed. |
| `Database__ServerName`, `Database__Name` | SQL target. Both blank only in Development (InMemory). Partial configuration is rejected. |
| `Database__IntegratedSecurity`, `Database__User`, `Database__Password` | Integrated identity or SQL login. `Encrypt=true` is always on; `TrustServerCertificate` only in Development. |
| `KeycloakSettings__Enabled`, `__Issuer`, `__PublicIssuer`, `__ClientId`, `__ClientSecret` | Token validation. Disabled only in Development. Issuer must be HTTPS outside local loopback. |
| `KeycloakSettings__Audience` | Required API access-token audience outside Development (distinct from the OIDC client id). |
| `Localization__LanguagesCodebook`, `__LanguagePacksCodebook`, `__DefaultLanguage` | Codebook names for runtime localization (defaults `Languages`, `LanguagePacks`, `BS`). |
| `BankReference__Enabled`, `__ConnectionString`, `__CustomerTable`, `__CustomerTaxColumn`, `__CustomerMatbrojColumn`, `__CustomerNameColumn`, `__GccTable`, `__GccNumberColumn`, `__GccNameColumn` | Read-only bank source for customer and GCC lookup (`kod.dbo.komitent`, `glava.dbo.grupekom`). Disabled means lookups return 503; nothing is fabricated. |
| `FinanceMonthly__Enabled`, `__ConnectionString`, `__CustomerKeyColumn`, `__Currency`, `__BalanceColumn`, `__OffBalanceColumn`, `FinanceMonthly:MonthlyTables` | Approved monthly finance source for regulatory reports. Disabled means snapshots are labelled non-authoritative. |
| `KeycloakSettings__AdminClientId`, `__AdminClientSecret` | Only where the Keycloak administration lookup is used. |
| `Email__Provider` | `demo` locally; `httpapi` (`Email__SendEmailWithHtmlBodyUrl`) or `smtp` (`Email__Smtp__*`) for real delivery. |
| `Email__AdminEmail`, `Email__HrEmail`, `Email__From`, `Email__AppBaseUrl` | Approved recipients, sender and public link base. |
| `ReverseProxy__KnownProxies__N`, `__KnownNetworks__N`, `__ForwardLimit`, `AllowedHosts` | Production requires explicit trusted peers or bounded CIDRs, a positive hop limit and non-wildcard hosts. |
| `Cors__AllowedOrigins__N` | Approved browser origins only. |
| `IdentityBinding__AllowVerifiedEmailFirstLink` | Keep false until the production reconciliation is approved. |

**Frontend**

| Variable | Purpose |
| --- | --- |
| `Apis__BaseUrl` | Backend origin (HTTPS, ending in `/`). Blank gives unavailable clients. |
| `Authentication__Authority`, `__ClientId`, `__ClientSecret` | Keycloak realm and the confidential client. Configure together. |
| `AllowedHosts` | Approved hosts, consistent with the ingress and probes. |

Production authentication also needs a registered shared `IDistributedCache` and persisted Data Protection keys; environment variables alone do not provide them. Both hosts run with `TZ=Europe/Sarajevo`, listen on 8080 behind approved TLS termination and need the enterprise CA chain.

## Authorization
- Roles and permissions are database data, not code. Roles (`admin`, `FL`, `PL`, `kapital`, `limiti`, `gost`) can be combined; every signed-in person without an explicit role is a Guest with read-only access.
- Permissions follow the pattern `<AREA>_<ACTION>`: `FL_*`, `PL_*`, `LIMITI_*`, `KAPITAL_*` (VIEW, CREATE, EDIT, DELETE), `LISTS_VIEW`, `ADMINISTRATION_VIEW`, `ADMINISTRATION_MANAGE`. The frontend gates screens by permission; the API re-checks every endpoint (401 unauthenticated, 403 forbidden, 423 period locked).
- Four-eyes principle: the person who entered a record cannot verify it. The verify button is disabled for the author with an explanatory tooltip.
- Administrators cannot deactivate or delete themselves or remove the last active administrator.
- Identity binding uses the Keycloak subject; email and username changes are handled through the application user record.

## Database
SQL Server is the only persistent provider. The application never creates the schema, migrates or seeds data at startup.

1. Change the EF model and add a migration: `dotnet ef migrations add <Name> --project Backend/RelatedPartiesRegister.csproj --startup-project Backend/RelatedPartiesRegister.csproj`.
2. Run backend unit and integration tests; check the migration on a copy or an empty database.
3. Generate an idempotent script for exactly one change (`dotnet ef migrations script --idempotent`). DML runs inside a transaction with `TRY/CATCH`, `ROLLBACK` and `THROW`. Reviewed scripts are kept in `docs/database/changes/`.
4. Submit the script through the central `rbbh-coredb-microsvcdb-cac` repository: branch from `UAT`, put the file in `schema/tables/`, `schema/DML/` or the matching object folder, open a PR to `UAT`, then create the GitHub Issue from the template (script link, PR, `db_name`, `Depend`, reason, deploy order, rollback). Move it to `GO-UAT` once the PR is merged and checked, verify in `QA`, then `GO-LIVE` and `Close`. DBA contact: `it-dba@raiffeisengroup.ba`.
5. The OCP application never runs DDL. Update the database before or together with the API release; record row counts before and after data migrations.

Data sources for lookups: the bank database server `bosnadb\HEAD` provides `kod.dbo.komitent` (`porez_broj`, `matbroj`, `naziv`) and `glava.dbo.grupekom` (`brojg`, `naziv_grupe`).

Local SQL: `Database__ServerName=localhost\SQLEXPRESS`, `Database__Name=RPR_Local`, `Database__IntegratedSecurity=true`, then `dotnet ef database update`.

## Testing
```powershell
dotnet build RelatedPartiesRegister.slnx -c Release -p:RunAnalyzers=true
dotnet test RelatedPartiesRegister.slnx -c Release --no-build
cd tests/Web.Blazor.Browser; npm ci; npx playwright install chromium
$env:BROWSER_FIXTURE_ROLE = "admin"; npm test      # repeat for gost, kapital, PL, limiti, FL
```
- `Web.Blazor.Tests` (xUnit) covers API clients, models, validation, localization and the source guards.
- `Web.Blazor.Browser` (Playwright, workers=1, retries=0) covers role gates, mutations, validation, BS/EN, light and dark Axe, keyboard use and 375px layouts, once per role on the isolated BrowserHost.
- Backend SQL tests need `RBBH_TEST_SQLSERVER` pointing at a disposable server without an initial catalog; they create and drop their own databases. Never point tests at production. Skipped tests are not passes.
- Manual testing stays for visual design, real Keycloak login, e-mail content and generated report content.

## Build, delivery and operations
```powershell
dotnet publish Backend/RelatedPartiesRegister.csproj -c Release -o artifacts/publish/api
dotnet publish Frontend/RBBH.RelatedPartiesRegister.Web.csproj -c Release -o artifacts/publish/web
docker build -f Backend/Dockerfile -t rbbh-api:local .
docker build -f Frontend/Dockerfile -t rbbh-web:local .
```
- Workflows: `build-and-push-image.yml`, `codeql.yml`, `deploy-to-ocp.yml`. Images are tagged `sha-<full commit>` and deployed pinned to `@sha256`. Rollback re-dispatches the deploy with the previous accepted tag and digest; the workflow does not down-migrate SQL.
- GitHub environment variables: `ARTIFACTORY_REGISTRY`, `ARTIFACTORY_REPOSITORY`, `WEB_IMAGE_NAME`, `API_IMAGE_NAME`, `OCP_SERVER`, `OCP_PROJECT_NAME`, `OCP_PULL_SECRET`, `WEB_HEALTH_HOST`, `API_HEALTH_HOST`. Secrets: `ARTIFACTORY_USERNAME`, `ARTIFACTORY_TOKEN`, `TEST_SQL_SA_PASSWORD` (disposable CI SQL), `OCP_TOKEN`.
- Health: `/health/live` (process only) and `/health/ready` (SQL connectivity, 503 on failure). Probe hosts must be in `AllowedHosts`. After a rollout also verify TLS and redirects, sign-in and permissions, deep links, notifications and the database state; an HTTP 200 alone is not enough.
- Logs are structured with correlation IDs. `/metrics` must be reachable only through an approved network or auth policy. Alerting should cover error rate, database or Keycloak unavailability, failed migrations and report-generation failures.
- Routine maintenance: security updates, controlled migrations, database backup, audit-log review, secret rotation, periodic role and permission review.

## Release blockers
| Severity | Item |
| --- | --- |
| BLOCKER | A Keycloak client secret was committed in an earlier version of `Frontend/appsettings.json`. Tracked configuration is now blank, but the value remains in Git history. The authorized owner must rotate it in Keycloak and confirm that distributed copies are purged. History rewrite and rotation need explicit approval. |
| BLOCKER | The frontend host has no approved shared session provider (`IDistributedCache`, shared Data Protection keys); production authentication fails closed until one is registered. |
| HIGH | Proxy trust for the frontend must be bound from deployment configuration; replica, resource and rollout values need owner approval. Real SQL, Keycloak audience and redirects, mail and CA chains are untested against production. |
| MEDIUM | Exports are unbounded and held in memory (imports are limited to 10 MB / 5,000 rows); measure under load before sizing pods. |

## Requirements and coverage
`docs/POPRAVKE.txt` and the project specification list the requirements. The backend wire contract is `docs/BACKEND-FRONTEND-CONTRACT-FINAL.md` (handoff: `docs/INTEGRATION-HANDOFF-CODEX.md`, evidence: `docs/BACKEND-FINALIZATION.md`, audit: `docs/BACKEND-INDEPENDENT-AUDIT.md`). Publication and credential-rotation requirements are in `docs/BACKEND-RELEASE-PUBLICATION.md`. [docs/FRONTEND-COVERAGE.md](docs/FRONTEND-COVERAGE.md) records the status of each one (VERIFIED with test evidence, PARTIAL, BLOCKED with the needed contract, OPEN).
