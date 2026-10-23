# Related Parties Register — native Blazor migration

Generated from the finalized local `RBI.Templates.0.1.0.nupkg` using
`dotnet new rbbh-webapp -n RBBH.RelatedPartiesRegister.Web --auth --authorization --localization`.
Template SHA256:
`BEED77E4366D9EE82D7083C56BC297C8685CA6B4D4AA592679A42F638D736F1E`.
No RBI component implementations are copied into the application. Runtime
dependencies are NuGet packages, not project references to the platform checkout.

The owner-approved file-transport extension is consumed as local pre-release
`RBI.ApiCore` and `RBI.Authentication` **0.1.1-migration.1**, built from platform
commit `a38b94c` on `codex/api-file-transport`. Other RBI packages remain0.1.0.
This is not a published corporate release. Restore both new packages from the
approved isolated local feed; do not overwrite/reuse older0.1.0 binaries.

This is a side-by-side migration, **not the canonical production frontend yet**.
React and existing backend deployment remain unchanged. Consult
`docs/BLAZOR-MIGRATION-{INVENTORY,AUTHORIZATION,PARITY}.md` at repository root.

## Restore, build, test

The generated NuGet.config retains the corporate proxy. When using final local
pre-release RBI packages, use an explicit local feed plus an approved public or
corporate upstream in a separate NuGet.config. Pass it with `--configfile`; do
not modify global sources or commit workstation paths. Use an isolated cache:
different development builds currently share package version 0.1.0 and must not
be silently satisfied from an older global package cache.

From repository root, after configuring the feed:

```powershell
dotnet restore tests/Web.Blazor.Tests/Web.Blazor.Tests.csproj --configfile <approved-config> --packages artifacts/blazor-packages
dotnet build src/Web.Blazor/RBBH.RelatedPartiesRegister.Web.csproj --no-restore
dotnet test tests/Web.Blazor.Tests/Web.Blazor.Tests.csproj --no-restore
dotnet publish src/Web.Blazor/RBBH.RelatedPartiesRegister.Web.csproj --no-restore -o artifacts/blazor-publish
dotnet run --project src/Web.Blazor/RBBH.RelatedPartiesRegister.Web.csproj --no-launch-profile -- --environment Development
```

`RelatedPartiesRegister.Migration.slnx` groups the existing backend/tests and the
new frontend/tests without replacing the backend solution. Tests have separate
isolated identities/stores; production application code has no test user mode.

## Configuration / identity

`Apis:BaseUrl` is the single backend URL, ending in `/`. Use environment variables
or user-secrets; HTTPS is required outside Development. Leave it empty only for
an honest unconfigured state, never fake business data. RBI.ApiCore provides
typed transport, timeouts, safe failures and request context; RBI.Authentication
propagates the authenticated user's server-side token when configured.
An API URL alone does not enable anonymous business requests: unconfigured auth
keeps outbound domain clients unavailable, including against a legacy backend
that independently enables Development authentication.

Set `Authentication:Authority`, `ClientId`, `ClientSecret` through approved
configuration, not source control. All empty in Development means signed out:
no fake identity, no login handler, no mutation privileges. Partial settings
remain subject to strict platform validation. Outside Development, missing auth
fails startup. Integrated auth uses confidential OIDC and HTTPS; production also
requires the approved shared IDistributedCache registration before authentication
and shared DataProtection/session deployment configuration. Local in-memory cache
is Development-only. Real environment provisioning is not proven by local tests.

Keycloak authenticates identity/email. `GET /api/auth/profile` supplies the
application's current DB-owned role/permission codes. The frontend never accesses
the DB, assigns realm roles or infers application grants from provider claims.
Reporting read is authenticated; persisted generation requires the backend's
`KAPITAL_CREATE` permission (Kapital/Admin). API enforcement remains authoritative.
The app adapts RBI semantic permission names (for example `reports.generate`)
to existing backend codes; it does not map identity-provider roles to grants.

RBI.Localization discovers only app-owned BS/EN catalogs, with BS default. The
removed inherited DE starter catalog is not an advertised business language.
Theme/sidebar/controls remain package-owned; only app layout composition is local.

The nested generated workflow examples are not active root deployment workflows.
Do not activate/cut over deployment until publish/startup, full module parity,
responsive/accessibility and integrated environment gates are accepted.

The 2026-10-23 local checkpoint passes 520 .NET and 209 browser tests, plus Release
publish and actual published signed-out startup. See
`docs/REACT-TO-BLAZOR-FINAL-HANDOFF.md` at repository root for module evidence and
the still-blocked SSO/shared cache/SQL/SMTP/OCP/security acceptance gates.
Local green tests do not authorize React retirement or production cutover.
