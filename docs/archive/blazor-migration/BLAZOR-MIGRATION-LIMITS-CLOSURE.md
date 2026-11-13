# Limits native migration checkpoint

2026-10-22. Local module evidence; no frontend/deployment cutover.

`/app/limits` consumes the existing `api/limiti` array/integer-ID contracts and
DB-owned LIMITI_VIEW/CREATE/EDIT/DELETE grants. Production APIs use the approved
packed RBI file transport and server-side token session. No backend service,
financial calculation, SQL schema or period-lock scope changed in this module.

## Scope

- Typed list/detail/create/update/delete and XLSX export. Read/export use the
  same optional legalEntityId filter. Client scalar search retains returned
  audit/identity metadata matching, separately from backend entity filtering.
- Native legal-entity search uses the existing name/registration search endpoint
  with its real 20-result bound, disclosed in BS/EN. Search can reach entities
  beyond the initial 20. The existing form-data endpoint supplies identity and
  global regulatory/core capital. The source's Matbroj-before-MaticniBroj display
  fallback is retained. No browser-owned capital derivation or DB access.
- Live VrstaLimita only: an outage is explicit, zero active types disables save,
  inactive/missing selections receive localized validation, and no fake fallback
  type is generated. Entity selection cancels superseded detail requests; route
  disposal cancels its outstanding work.
- Required entity/type, nullable nonnegative decimal amounts, zero preservation,
  optional date/comment and backend validation. InputNumber uses the source's
  `step=any`, not a new frontend financial rounding rule.
- Edit reads current backend detail. Delete retains the existing hard-delete
  workflow, behind a confirmation that explicitly describes permanence; cancel
  is exercised before confirmed deletion. This does not authorize any direct
  database cleanup or alter the backend's deletion policy.
- Honest empty/loading/error/retry/403/423 behavior, server-side event guards,
  safe owned error copy, real download bytes, BS/EN cookie/API culture, automated
  WCAG checks, Escape/return focus and a visually inspected 375px table screenshot.

## Evidence and boundaries

From `tests/Web.Blazor.Browser`, run `npm test` with BROWSER_FIXTURE_ROLE set to
limiti, gost, PL or FL. Configuration enforces workers=1/retries=0. All stores,
identities, fault controls and reference seeding exist only in the separate
test executable; actual controllers/services/audit/period middleware run against
isolated InMemory state. Backend Program/.env, SQL, SMTP and real SSO never run.

- limiti 10/10 browser cases; gost 5/5 across all three migrated modules.
- PL 12/12 and FL 14/14 existing browser regressions. Total 41/41.
- Frontend 88 tests: 13 added at this checkpoint (draft/client/composition plus
  two signed-out route cases). Backend 210 unit + 67 integration. Total 365,
  no skips. Run `dotnet test RelatedPartiesRegister.Migration.slnx --no-restore`.
- Frontend Release publish and `git diff --check` pass. Changed-C# formatting is
  verified; the formatter reports an existing workspace-load warning, not a
  zero-warning run.

Receipts are ignored under `artifacts/migration-template/limits-*.log`; per-role
JSON/screenshots/traces are under `artifacts/migration-browser/`. Initial browser
checks caught missing ValueExpression forwarding and a literal rather than bound
select ID in the new app-owned picker. The fixes have both static HtmlRenderer
and real browser coverage. A test's incorrect cancellation-button label was
corrected to the actual resource. Assertions, timeouts, retries and screenshots
were not weakened; no force-click bypass was introduced.

One first-page Guest regression run missed the unchanged five-second interactive
readiness predicate while the solution formatter was running concurrently. Its
trace showed a successful WebSocket connection and no browser exception, but no
package connection within that predicate window. The failure log/trace are kept
as `limits-guest-cold-start-failure.log/.zip`. Subsequent verification is serialized
without the formatter running; no product code, readiness assertion or timeout
was changed to obtain that result. This observation does not establish that all
slow/deployed environments have been validated.

Existing Pkcs NU1902/NU1903 warnings remain visible and unresolved. Real SQL,
SSO/cache/OCP acceptance, remaining modules and production cutover remain open.
