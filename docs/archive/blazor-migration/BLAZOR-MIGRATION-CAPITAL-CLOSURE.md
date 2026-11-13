# Capital native migration checkpoint

2026-10-22. Local module acceptance only; React and active deployment unchanged.

The native `/app/capital` route uses the existing optional singleton GET, PUT
upsert and backend XLSX export. KAPITAL_VIEW/EDIT come from the actual backend
profile/database grants. The browser never receives an access token, calculates
capital or writes business storage. No capital backend/controller/schema changed.

## Preserved behavior

- Honest unset state before the first upsert, distinguished from unavailable,
  forbidden, malformed or incomplete API responses. There is no fictional
  zero-valued singleton. Explicit zero amounts are valid and stay zero.
- Three required, nonnegative nullable decimal form fields and required date;
  backend remains authoritative. The response's stable singleton ID survives
  subsequent updates. No new delete/import/multi-record workflow was added.
- Existing amounts/date populate editing; save refreshes server data. Actual
  backend export works with either no record or a populated record. Currency,
  date, labels, validation and notices use app-owned BS/EN wording/culture.
- Both Kapital and Admin exercise real DB-transformed write permissions. Guest
  reads empty/existing state and downloads, without a rendered edit handler/form.
  Mutation callbacks also check effective write permission.
- The existing PeriodLockMiddleware excludes capital. A named browser case
  proves this preserved behavior; this migration does not expand its scope or
  introduce a new period-lock business rule.
- Keyboard modal dismissal/return focus, labelled fields, automated WCAG
  checks, honest loading/error/retry, culture persistence/API Accept-Language,
  and 375px layout. The source table's 520px minimum width is preserved inside
  the package's labelled horizontal region so money/date cells remain legible.

## Optional singleton transport boundary

The packed generic RBI JSON operation intentionally requires a response value.
Capital's app-owned client therefore reads **this specific endpoint** as a
JsonElement through the same packed transport/token pipeline. Only declared
JSON null or a normalized Unknown/HTTP204 result means unset. Non-null objects
deserialize to the typed capital contract with required constructor parameters;
missing fields, malformed JSON, arrays, 404 and 503 do not become empty state.
Unit tests cover each distinction. No shared transport relaxation, generic
envelope normalizer, raw HttpClient, token exposure or extra platform change was
needed. Other required-value operations retain their original contract.

## Evidence

`dotnet test RelatedPartiesRegister.Migration.slnx --no-restore`: frontend 100
(12 added at this checkpoint), backend 210 unit + 67 integration = 377, no skips.
Frontend Release publish, changed-C# format verification and git diff --check
pass. Formatting still reports the known workspace-load warning; existing
backend/test-host NU1902/NU1903 Pkcs advisories remain visible and unresolved.

Browser configuration uses workers=1/retries=0. Run `npm test` from
`tests/Web.Blazor.Browser` with BROWSER_FIXTURE_ROLE:

- kapital 7/7 and admin 7/7 capital cases.
- gost 6/6 read-only cases across four migrated modules.
- FL 14/14, PL 12/12 and limiti 10/10 regressions. Total 56/56.

Receipts: ignored `artifacts/migration-template/capital-*.log` and per-role
JSON/screenshots/traces under `artifacts/migration-browser/`. The mobile capital
screenshot was visually inspected; no baselines/assertions/timeouts were weakened.
Browser verification and solution formatting are serialized following the
documented earlier cold-start contention observation.

The test-only executable uses real controllers/services/audit/permission
transformation with isolated InMemory stores and synthetic fixture identity.
It never starts the backend Program/.env, real SQL/SMTP or Keycloak. Production
has no fixture switch. SQL/SSO/cache/OCP integration and remaining module closure
remain open; this report is not a production cutover authorization.
