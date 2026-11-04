# Physical persons / family module — local closure

2026-10-22. This is a module-local contract/browser checkpoint, not production
SSO/SQL/OCP acceptance or whole-application completion. React remains unchanged
and canonical deployment has not switched.

## Implemented

Native `/app/physical-persons`, reachable through the role-aware template sidebar
and dashboard. The module uses package tables, controls, typed choices, dialogs,
confirmation, feedback and responsive layout; no React/Tailwind/Radix code or RBI
component implementation is copied. Read/create/edit/delete/verify gates consume
the existing effective `FL_*` backend permissions. Backend checks remain final.

The specialized two-step form preserves residency-dependent identity fields,
JMBG/date/checksum UX, passport/FBA checks, explicit nullable Yes/No choices,
dynamic relationship codebooks with the existing intentional fallback vocabulary,
derived read-only description, immediate-family locked flags/parent linkage,
required dates/date order, duplicate checks with edit exclusion and safe field
feedback. It does not restore removed GCC fields. Record status is controlled
by the backend workflow, not a free-form status editor.

The existing unified family workflow creates/edits/deactivates linked physical
records and refreshes the recursive read-only relationship tree. There is no
invented separate legacy family CRUD screen. List search retains scalar response
values including identities/status and existing creator/audit metadata.

Imports are real multipart files (nonempty .xlsx, ≤10MiB preflight); schema and
row validation/persistence remain backend-owned. Known column-count/header-order,
row-count and field validation messages are mapped to owned localized copy.
Unknown exception/SQL/header text is never echoed. Backend-generated XLSX bytes,
media type and safe filename are delivered through a small app-owned stream/Blob
adapter. Nothing generates Excel or writes audit/EF records in the frontend.

RBI.Forms handles EditContext error storage and submission exclusion. Async
duplicate errors have a separate store so lookup completion cannot erase a
synchronous invalid-JMBG error. Focus moves to step/error context; modal cancel
returns focus to its trigger. The launching button remains focusable while
codebooks load, and background refresh preserves the existing DOM.

Unconfigured authentication does not register outbound business clients, even
when a backend URL is supplied: it must not proxy anonymously to a separate
legacy Development auth fallback. Server event handlers also check effective
permissions; visibility is never the security boundary. Test-only hostile DOM
opening of the hidden import dialog cannot upload or issue a mutation as Guest.

## Exact local evidence

- Backend: **210 unit +64 integration =274 PASS**, unchanged since the reporting
  authorization hardening. New frontend: **58 PASS**. Combined: **332/332**,
  zero failures/skips. Receipt: `artifacts/migration-template/physical-final-test.log`
  and `physical-final-trx/`.
- Browser: **14 FL +2 Guest =16/16 PASS**, workers1/retries0. Receipts:
  `artifacts/migration-browser/{FL,gost}/results.json`.
- Covers empty/data/search/no-results, resident/nonresident, invalid and duplicate
  identity, edit self-exclusion, verify/deactivate and reload persistence, family
  create/edit/remove/tree reload, valid and invalid real workbook import, backend
  attachment bytes/name, dynamic-codebook fallback, backend outage/recovery,
  actual period middleware423 with zero data/audit writes, Guest read/download
  without mutation controls, hostile hidden-import attempt, and BS/EN cookie
  persistence plus Accept-Language reaching the backend.
- Axe WCAG2A/AA/2.1AA scans pass for the empty route and business form; keyboard
  modal/step focus assertions pass. This is automated coverage, not a complete
  assistive-technology/conformance certification.
- At375px a real record is created through the form; the table scrolls inside
  its labeled keyboard-accessible region without page overflow. A screenshot
  was inspected. Desktop tests use1440×1000. No legacy image/reference is changed.
- Frontend Release publish passes. Changed C# format verification exits0 with
  a workspace-load warning; diff check passes. Existing backend/test-host
  Pkcs9.0.4 NU1902/NU1903 advisories remain unsuppressed release risks.

## Test isolation and reproduction

`tests/Web.Blazor.BrowserHost` is a separate **non-production** executable. It
runs production frontend components/typed package clients against actual backend
controllers, services, role transformation, audit service, period middleware and
EF InMemory repository. Identity and email are isolated fixtures; the production
app has no fixture switch/user. Each case gets a fresh uniquely named InMemory
store. It never executes backend Program/.env, SQL migrations, SMTP or Keycloak.
Its loopback-only control routes exist exclusively in the test host. The browser
suite does not prove real OIDC token exchange; the approved transport extension's
separate authenticated propagation/refresh tests cover that package boundary.

After the documented isolated package restore, from repository root:

```powershell
dotnet build tests/Web.Blazor.BrowserHost/Web.Blazor.BrowserHost.csproj --no-restore
dotnet test RelatedPartiesRegister.Migration.slnx --no-restore
Set-Location tests/Web.Blazor.Browser
npm ci --ignore-scripts
npm test
$env:BROWSER_FIXTURE_ROLE = 'gost'
npm test
```

The test runner owns only its fixture host5273. Set BROWSER_FIXTURE_URL to another
loopback HTTP port when needed. It never replaces5199,8183 or platform hosts.
Playwright waits for the real sidebar's interactive connection before clicks,
not a fixed sleep or retry. Existing legacy React/deployment checks remain intact.
See BLAZOR-MIGRATION-ISSUES.md for the separate boolean-select platform finding,
local pre-release transport packages and outstanding integrated release gates.
