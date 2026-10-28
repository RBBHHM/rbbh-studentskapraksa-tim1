# RBBH Related Parties Register

Native Blazor Server frontend and ASP.NET Core backend on .NET 10. Application
permissions are database-owned; Keycloak supplies identity. The backend remains
the authority for user safety, global period locking and report persistence.

## Repository

```text
Backend/       RelatedPartiesRegister.csproj, API, business services, persistence
Frontend/      RBBH.RelatedPartiesRegister.Web.csproj, Components, application clients
  Platform/    seven required RBI runtime source projects
tests/        backend unit/integration, Blazor .NET and native browser suites
 docs/         configuration, database, deployment and acceptance evidence
.github/       exactly three build/CodeQL/deployment workflows
RelatedPartiesRegister.slnx
NuGet.config   one public third-party restore source
.env.example   safe backend Development example
```

RBI runtime libraries are tracked source under `Frontend/Platform` and connected
through `ProjectReference`. A clone needs no separate RBI checkout or private RBI
package delivery. See [source provenance](Frontend/Platform/SOURCE.md).

## Fresh clone

Install .NET SDK 10. SQL integration tests require Windows LocalDB or a disposable
SQL Server instance configured with `RBBH_TEST_SQLSERVER` (no database catalog;
tests create and remove their own uniquely named databases). Never use production SQL.

```powershell
git clone <repository-url>
cd <cloned-repository>
dotnet restore RelatedPartiesRegister.slnx
dotnet build RelatedPartiesRegister.slnx
dotnet test RelatedPartiesRegister.slnx
```

`NuGet.config` uses nuget.org for third-party packages. Where corporate policy
requires a public-package proxy, replace that source with the approved mirror and
supply credentials outside the repository. No RBI package feed is needed.

## A. Seeded local demo / test application

The test-only BrowserHost provides realistic isolated data and an Admin identity
without SQL, Keycloak or mail. It is loopback-only and is never published or deployed.

```powershell
$env:BROWSER_FIXTURE_ROLE = "admin"
$env:BROWSER_FIXTURE_URL = "http://127.0.0.1:5273"
dotnet run --project tests/Web.Blazor.BrowserHost/Web.Blazor.BrowserHost.csproj --no-launch-profile
```

Open http://127.0.0.1:5273/app. All Admin routes are available, including physical
and legal persons, limits, capital, reports, notifications, users, period, code
lists, audit, profile and guide. BS/EN and light/dark controls belong to the app.
This test identity is not a production authentication mode.

## B. Real application development

In one terminal, copy `.env.example` to `Backend/.env`, review its values, then:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Email__Provider = "demo"
dotnet run --project Backend/RelatedPartiesRegister.csproj --no-launch-profile --urls http://localhost:5000
```

Absent SQL settings, Development uses an empty InMemory store; it does not provide
the seeded demo. Startup does not apply migrations or seed a real database.

In another terminal, configure the frontend's real backend and approved local OIDC
client through user-secrets (do not put credentials in tracked files), then:

```powershell
dotnet user-secrets set "Apis:BaseUrl" "http://localhost:5000" --project Frontend/RBBH.RelatedPartiesRegister.Web.csproj
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project Frontend/RBBH.RelatedPartiesRegister.Web.csproj --no-launch-profile --urls http://localhost:5080
```

Set `Authentication:Authority`, `Authentication:ClientId`, and
`Authentication:ClientSecret` for real sign-in. A signed-out host with blank
identity settings is not the full demo. See [configuration](docs/configuration/ENVIRONMENT-VARIABLES.md).

## Verification and delivery

```powershell
dotnet build RelatedPartiesRegister.slnx -c Release -p:RunAnalyzers=true
dotnet test RelatedPartiesRegister.slnx -c Release --no-build
# Node 22 and Chromium are required only for native browser testing:
cd tests/Web.Blazor.Browser
npm ci
npx playwright install chromium
# Run separately for admin, gost, kapital, PL, limiti and FL:
$env:BROWSER_FIXTURE_ROLE = "admin"
npm test -- --workers=1 --retries=0
```

Return to the repository root for the following commands. Both image builds use
the repository root as context:

```powershell
dotnet publish Backend/RelatedPartiesRegister.csproj -c Release -o artifacts/publish/api
dotnet publish Frontend/RBBH.RelatedPartiesRegister.Web.csproj -c Release -o artifacts/publish/web
docker build -f Backend/Dockerfile -t rbbh-api:local .
docker build -f Frontend/Dockerfile -t rbbh-web:local .
```

Production still requires approved shared session/cache and Data Protection storage,
proxy/host/CA settings, SQL/SSO/mail validation, resource/drain/scheduler policy and
subject-link migration approval. See [production readiness](docs/PRODUCTION-READINESS.md)
and [final handoff](docs/REACT-TO-BLAZOR-FINAL-HANDOFF.md). Source delivery does not
resolve those external prerequisites.
