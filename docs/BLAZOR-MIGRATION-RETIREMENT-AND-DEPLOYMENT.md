# React retirement and native deployment candidate â€” 2026-10-23

**Historical2026-10-23 checkpoint below.** On2026-10-26 the root workflows were
consolidated and the wrapper/candidate/IIS scripts described below were removed.
Do not run the historical script commands. Use the direct dotnet/docker commands
in [README](../README.md) and the current [native pipeline](NATIVE-PIPELINE-2026-10-26.md).
Claude's final visual batch has now been integrated and the tracked React source,
remaining wrappers and old solution names retired after the integration gates.
See NATIVE-CUTOVER-RECONCILIATION-2026-10-26.md for the final source state and
post-retirement validation. The remaining text preserves the original checkpoint.

**Local module parity; production cutover NOT APPROVED.** `the retired React reference tree` is explicitly
MIGRATION_REFERENCE. No React implementation or existing deployment was removed.

## Exact retirement boundary / active dependencies

The legacy boundary is the274 previously tracked files under `the retired React reference tree/**`
(plus the new reference marker), including its package.json/retired JS package tooling lock, retired bundler
config, React source/assets and React-specific tests/scripts. Remove/archive as
one reviewed frontend boundary only after actual consumers and rollback are known.
Do not delete backend/test infrastructure simply because it once served React.

Legacy package groups that native Frontend does not consume:

| Group | Current legacy package/tool boundary |
|---|---|
| React | react/react-dom19.2; React-specific component/helper packages |
| TanStack | react-router, react-query, react-table |
| UI | Radix react-*; lucide-react, recharts, cmdk, vaul, sonner, input-otp, embla-carousel-react, react-day-picker, react-resizable-panels |
| Styling | tailwindcss3.4.17, tailwind-merge, PostCSS/autoprefixer |
| Forms | react-hook-form, @hookform/resolvers |
| Localization | i18next, react-i18next, http-backend/icu; legacy resource compilation |
| Build/test | retired bundler.config.cjs and retired bundler/loaders/plugins/devserver; React Node tests, ESLint/TypeScript build tooling |

`keycloak-js` is NOT a current package/source dependency in this checkout; do not
claim a removal that did not occur. Native auth is confidential server-side OIDC.

Known repository consumers preventing premature deletion:

- `the retired React reference tree/package.json` and `scripts/config/generate-runtime-config.mjs`.
- `the retired React reference tree/scripts/deployment/create-iis-zip.mjs` and localization release/validation scripts.
- `scripts/{build,restore,test,dev-web}.cmd` invoke the old retired JS package tooling frontend workflow.
- `scripts/Deploy-FrontendIis.ps1` expects static `_shell.html`, IIS copy/app-pool behavior.
- Root build-and-push-artifact workflow has configurable PROJECT_PATH/context;
  current documented defaults are backend. GitHub Environment overrides and
  deployed IIS/runtime-config consumers were not inspected or proven retired.
- Existing manual OCP deployment and production route/rollback ownership still
  require operator acceptance. Local grep cannot prove there are no off-repo consumers.

## Deployment contract

| Legacy | Native candidate |
|---|---|
| retired bundler React static/IIS ZIP, runtime-config generation | published .NET10 server app, RBI static assets and interactive Blazor circuits |
| IIS static rewrite / `_shell.html` | ASP.NET entry DLL and approved ingress/circuit settings |
| old frontend packaging scripts | explicit native publish/manifest/ZIP script below |
| existing accepted rollback | replacement not canonical until real UAT/cutover approval |

No root workflow or production route was changed. Nested template workflows are
examples, not active deployment. Native Dockerfile requires nested frontend
context, approved corporate base images and third-party dependency sources; no
corporate Docker/OCP success is claimed. .dockerignore now excludes local secret
and certificate files from that build context.

From repository root after an approved isolated restore:

```powershell
./scripts/Publish-BlazorCandidate.ps1 -Name unique-candidate -BuildArtifactsPath artifacts/users-safety -NoRestore
./scripts/testing/Test-BlazorPublished.ps1 -PublishPath artifacts/blazor-candidates/unique-candidate/app
```

Without prior restore, supply `-NuGetConfig <approved-config>` instead of NoRestore.
Use the same BuildArtifactsPath for restore/build. Output stays under ignored
artifacts; an existing candidate is never deleted/overwritten. app.zip contains
only published native files, with entry DLL and no legacy shell. manifest.json
records source commit, tracked-dirty flag and every published file's bytes/SHA256;
the command prints ZIP SHA256. Do not package secrets in appsettings files:
name-based exclusion is not a full content secret scanner. Secrets must be injected
by approved hosting configuration, not copied into the candidate.

The startup probe explicitly clears auth/API config in Development, starts the
actual published DLL on an ephemeral loopback port, checks14 BS/signed-out routes,
RBI CSS200 and login404, and stops only its owned process. It never starts backend
Program, SQL, SSO, SMTP or jobs and never stops a user host. It is not an
authenticated production startup/cutover test.

Local receipt: completion-local-1 candidate; completion-publish.log and
completion-startup.log under artifacts/migration-template. Manifest SHA checks
and ZIP inventory agree on89 files; existing-name and invalid-name/output guards
were exercised without overwriting/creating a rejected candidate. A final
completion-committed candidate is generated after commits; its manifest binds
the final clean tracked source HEAD and the command reports its archive checksum.

## Required operator cutover / rollback sequence (not executed)

1. Review tracked RBI source; approve runtime/base-image servicing and target
   native build/registry workflow separately from existing backend deployment.
2. Provision HTTPS API/issuer/audience, confidential OIDC redirect/logout settings,
   injected secrets, correct non-Development environment and trusted CA/proxies.
3. Supply approved shared IDistributedCache registration/provider, shared
   DataProtection and ingress/circuit/multi-pod configuration; prove refresh/logout
   and identity-to-DB-user mapping in the real realm.
4. Validate actual SQL constraints/app-lock/concurrent final-Admin safety, existing
   migration policy and SMTP with controlled UAT data. No automatic migration/seed.
5. Run integrated business/Axe/mobile flows and approve logs, security and rollback.
6. Inventory IIS/ZIP/runtime-config consumers with operators; approve one canonical
   native frontend switch. Keep accepted backend/data unchanged and a tested
   rollback artifact/route; do not run two canonical frontends.
7. Only then archive/remove the retired React reference tree and its exclusive package/build/deploy consumers
   in a separate bounded commit. No current local test authorizes this deletion.
