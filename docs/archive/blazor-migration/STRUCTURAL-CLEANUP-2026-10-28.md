# Structural cleanup and source delivery — 2026-10-28

Starting HEAD: `f78404a9e7976d7c2d8bcec32e72b4b318b8bae7`, branch `release-v1.0.6`.
Implementation commit: `c1209db2d664839209697665874fec3647b61d2e`.
The following documentation/evidence commit completes this batch; its SHA is in
the Git log and final delivery response. No push or production deployment was performed.

## Scope and layout

This replaces the superseded private RBI package delivery assumption with tracked
source. It does not change business behavior, permissions, identity, period locks,
API/schema, visual design, production auth or deployment prerequisites.

```text
Backend/
  RelatedPartiesRegister.csproj
  API/ BL/ DL/ Helpers/ IoC/ Middlewares/ Migrations/ ...
  Dockerfile
Frontend/
  RBBH.RelatedPartiesRegister.Web.csproj
  Components/ Api/ Security/ Localization/ wwwroot/
  Platform/
    RBI.DesignSystem/ RBI.Hosting/ RBI.ApiCore/ RBI.Authentication/
    RBI.Forms/ RBI.Authorization/ RBI.Localization/
    Directory.Build.props SOURCE.md SOURCE-MANIFEST.json
  Dockerfile
tests/
  Backend.UnitTests/UnitTests.csproj
  Backend.IntegrationTests/IntegrationTests.csproj
  Web.Blazor.Tests/ Web.Blazor.Browser/ Web.Blazor.BrowserHost/
docs/
.github/workflows/  # existing three workflows only
.config/           # existing repository-local EF tool manifest
RelatedPartiesRegister.slnx
NuGet.config
.env.example
README.md
.gitignore
.dockerignore
```

Root `env-variables.md` points to the canonical environment guide;
`schema-overview.md` remains reference documentation. Build/browser evidence and
retired ignored local outputs are under ignored `artifacts/`, outside production
areas. No old production directory tree remains. Existing ignored local files
were preserved rather than cleaned away.

Path mapping: backend root -> `Backend`; frontend root -> `Frontend`; formerly
nested backend unit/integration projects -> `tests/Backend.UnitTests` and
`tests/Backend.IntegrationTests`. Project filenames, assembly names, namespaces,
EF migration history and public contracts are retained. BrowserHost and frontend
source-reading tests discover the canonical solution, then use `Frontend`.

## Dependency delivery and integrity

Removed all seven RBI PackageReferences from the application:
DesignSystem, Hosting, ApiCore, Authentication, Forms, Authorization and Localization.
Each is now a ProjectReference to its corresponding project in `Frontend/Platform`.
Authentication references adjacent ApiCore; the other six have no RBI dependency.
Backend has no RBI dependency. Tests reference their application projects and
BrowserHost references both applications. The canonical solution contains 13 projects.

Only the required seven runtime projects were imported (265 upstream files).
Source revisions, per-file hashes and explicit adaptations are in
`Frontend/Platform/SOURCE.md` and `SOURCE-MANIFEST.json`. The approved transport
extension is retained. Runtime source is unchanged except eleven EOF-only blank-line
removals for whitespace validation. Other adaptations are project paths/explicit
third-party versions and source-install README wording. No platform product patch
was added. Public API analyzers and their existing severity rules are retained.
Release-only packaging properties/metadata were not imported.

All **254 existing production C#/Razor/JS/CSS/JSON Git blobs** are unchanged across
the application moves. All **23** prior DesignSystem static assets match imported
source with text line endings normalized. The prior package is used only as an
asset reference, never as a build input. No showcase, platform test project,
template, React application, Sifrarnik provider or release tooling was imported.

One root `NuGet.config` contains nuget.org for normal third-party dependencies.
An approved corporate public-package mirror can replace it under local policy;
credentials remain external. There is no private RBI source, sibling checkout,
RBI package cache dependency or release feed. OIDC 10.0.12, resilience 10.10.0 and
PublicApiAnalyzers 5.6.0 retain the source's existing third-party versions.
The backend's explicit Pkcs 9.0.21 patch remains intact; vulnerability warnings
were not suppressed.

Removed version-only frontend build/central-package props, duplicate application
NuGet configurations, the empty backend appsettings template and redundant
frontend Docker ignore file. A shared root Docker context serves both Dockerfiles;
frontend source includes its Platform closure, while tests/build outputs/secrets
are excluded. Runtime images receive publish outputs only.

## Onboarding and configuration

README now documents the actual source layout, clone/restore/build/test commands,
normal third-party restore source, root Docker contexts and two separate modes:
seeded loopback-only Admin BrowserHost versus real configured API/Blazor development.
The environment guide separates backend Database/Keycloak/Email/ReverseProxy/
AllowedHosts/Cors settings from frontend API/OIDC settings and explains Development
versus Production. `.env.example` contains only empty SQL/identity placeholders,
Development, integrated-security selection, disabled local backend auth and demo
mail. It recommends frontend user-secrets and is not a production configuration.

Durable development, SQL, pipeline, production and handoff guidance is aligned.
Historical checkpoint counts remain historical; old file paths are normalized for
the current tree and the superseded package prerequisite is explicitly retired.
Historical references to removed JS tooling are descriptive, not development steps.
The EF workflow now consistently describes explicit migrations and no startup seed.

## Fresh clean checkout proof

A real separate clone of the structural commit was created under ignored
`artifacts/structural-source/fresh-clone`; it contained no untracked user file,
application outputs or sibling source dependency. It remains clean after validation.

Initial completely empty-cache restore failed with **NU1301 DNS resolution** from
the .NET client to nuget.org. The OS HTTP client could retrieve the same official
URLs. Therefore 229 normal third-party package coordinates were downloaded afresh
from official nuget.org into the new isolated cache; none were copied from a prior
cache and none are RBI packages. Receipts are retained. This is an environment
network workaround, not a repository dependency/feed configuration. No temporary
NuGet config or source override was used. No advisory warning was disabled.

Using that isolated public-only cache, the fresh clone ran:

```powershell
dotnet restore RelatedPartiesRegister.slnx -m:1
dotnet build RelatedPartiesRegister.slnx -c Release --no-restore -p:RunAnalyzers=true -m:1 -nr:false
dotnet test RelatedPartiesRegister.slnx -c Release --no-build --no-restore -m:1 --logger trx
```

`NUGET_PACKAGES` and `NUGET_HTTP_CACHE_PATH` were explicitly isolated for these
runs. All 13 fresh assets files were inspected: zero RBI package libraries;
RBI dependencies resolve as projects inside the clone. The isolated cache has
zero RBI package directories. No global RBI cache/local package feed participates.
A completely cold direct .NET network restore remains subject to the observed
workstation DNS limitation; it is not falsely reported as a successful network run.

## Fresh verification results

| Gate | Actual result |
| --- | --- |
| Original-tree source restore/build with analyzers | PASS; 0 warnings, 0 errors |
| Clean-clone restore/build with analyzers | PASS with isolated public-only cache; 0 warnings, 0 errors |
| Backend unit | 278/278 PASS |
| Backend integration | 162/162 PASS |
| SQL/LocalDB subset | 8/8 PASS, included in integration; no skips |
| Frontend .NET | 210/210 PASS |
| Full .NET, original tree and fresh clone | 650/650 PASS in each run; no skips |
| Browser Admin | 78/78 PASS |
| Browser FL | 38/38 PASS |
| Browser PL | 36/36 PASS |
| Browser Kapital | 41/41 PASS |
| Browser Limiti | 32/32 PASS |
| Browser Guest | 28/28 PASS |
| Six-role native browser total | 253/253 PASS |
| Additional theme/viewport route matrix | 52/52 PASS: 13 Admin routes × light/dark × 1440/375px |
| Total browser scenarios | 305/305 PASS; workers=1, retries=0; zero skips/flaky/unexpected |
| Axe | Existing 43 executed suite cases with Axe assertions, plus all 52 matrix cases: zero violations |
| 375px | 29 explicit six-role cases plus 26 matrix cases PASS, no horizontal overflow in matrix |
| Release publish | Both Backend and Frontend PASS |
| Published startup | Both Development hosts PASS; live/ready 200; web deep links and published RBI CSS 200 |
| Fail-closed startup | API missing auth, web missing auth, web configured auth without shared cache all correctly rejected |
| Workflow/static image contracts | Exactly 3 root workflows; 19 shell syntax checks PASS; image paths/security/probe contracts PASS |
| Docker image execution / real OCP | NOT_RUN_INFRASTRUCTURE: Docker CLI exists but daemon pipe is unavailable; no live cluster deployment |
| Whitespace | git diff --check PASS |

SQL proof includes last-Admin transaction/concurrency safety, confirmed subject
linking and actual SQL readiness against disposable test-owned LocalDB databases.
No existing SQL data was migrated or changed.

The browser runs use the rebuilt source-based BrowserHost on loopback port 5388;
no user's preview host was replaced. The matrix covers `/app`, physical/legal
persons, limits, capital, reports, notifications, users, period, code lists, audit,
profile and guide, with theme persistence, semantic headings, Axe and overflow checks.

Raw receipts are in ignored `artifacts/structural-source`: restore/build/test logs,
TRX files, official package receipts, clean clone, six-role JSON results,
`theme/results.json`, asset equivalence hashes, publish trees, startup logs/results
and workflow validation. Historical acceptance evidence was not reused as a fresh PASS.

## Repository checks and remaining prerequisites

Whole tracked-tree searches return **zero matches** for the requested stale directory
and version/feed/tooling patterns. Searches also include Windows path forms and
split content-root strings. Namespaces, canonical solution/project filenames and
assembly identities deliberately retain RelatedPartiesRegister; they are not old
directory references. `src` in a brand component is an HTML image attribute.

Protected untracked file remains untouched and untracked:
`tests/Web.Blazor.BrowserHost/Properties/launchSettings.json`.
SHA256: `07daa11ff2d5d953b78d8fe4c7209af625c3d76f2fbec747986eb6f8df39ec93`.
No clean operation was performed. No user file, secret, output or dependency cache
was staged. Only bounded structural and documentation/evidence commits were made.

External production prerequisites remain: owner-approved shared session provider
and protected/persisted Data Protection keys; real ingress/proxy/hosts/CA values;
resource/drain/circuit/scheduler policies; real SQL/SSO/mail validation; actual realm
claims and approved one-time subject-link backfill/data reconciliation; corporate
vulnerability-feed/servicing acceptance; registry/image/OCP deployment validation.
Source delivery resolves the unpublished RBI dependency prerequisite, not these
infrastructure or production-data decisions. No push was performed.
