# Recorded notifications native checkpoint

2026-10-22. Local module closure; no production/deployment cutover.

The native `/app/notifications` route consumes the real `api/email-log` response
as `{ provider, emails[] }`, with KAPITAL_VIEW from the current database-backed
profile. No generic legacy envelope normalization, new sender, compose UI,
delivery dashboard, SMTP connection or backend change was introduced.

The page preserves purpose, recipient, subject, recorded status/time and scalar
search over the response's metadata/body. HTML body is never rendered as markup
or given to a rich-text component. A real malicious-image fixture confirms no
DOM image or executable handler appears. Searching a body term still finds the
record, preserving the old scalar-search behavior without exposing HTML markup.

The existing SMTP response intentionally contains no email rows. Native copy
explicitly says this is not proof of sending or delivery. Switching only the
test configuration back to the recorded provider reveals the existing stored
record again; the UI does not fabricate, delete or mark it delivered. Backend
localized purpose/status strings are retained and follow API Accept-Language.

## Adjacent source-backed search correction

Reviewing the original ResourcePage confirmed it trims search input before scalar
matching. The migrated physical/legal/limit matchers initially did not. They now
trim the term as the source does. Three focused typed-model cases and additional
browser assertions cover padded metadata queries; previous unpadded/no-result
assertions remain intact. React source, API queries, financial values and backend
search semantics are unchanged. This is a bounded parity fix, not a new search
framework or permission change.

## Evidence

- 123 frontend + 210 backend unit + 67 integration = **400/400 .NET**, no skips.
  Eight tests added here: four notification client/search cases, one signed-out
  route case and three registry-search cases. Existing backend reporting HTTP
  authorization remains covered in the integration total.
- Six notification browser cases run for **each** current application DB role:
  gost, kapital, admin, PL, limiti, FL. They cover empty/accessibility/no mutation,
  records/search/HTML safety, SMTP semantics, outage/retry, 375px and BS/EN.
- Full browser totals: gost 14/14, kapital 22/22, admin 22/22, PL 18/18,
  limiti 16/16, FL 20/20 = **112/112**, workers=1/retries=0. All earlier business
  module cases remain, including the stronger whitespace assertions.
- Frontend Release publish, changed-C# format verification and git diff --check.
  Existing workspace-load formatting warning and backend/test-host Pkcs
  NU1902/NU1903 advisories remain unsuppressed and unresolved.

Reproduce .NET checks with `dotnet test RelatedPartiesRegister.Migration.slnx
--no-restore`; browser checks with `npm test` from tests/Web.Blazor.Browser,
setting BROWSER_FIXTURE_ROLE per role. Receipts are ignored under
`artifacts/migration-template/notifications-*.log`; per-role JSON/traces/images
are under `artifacts/migration-browser/`. The mobile screenshot was inspected.

The isolated test host runs the real EmailLogController/EmailLogStore and actual
DB permission transformation with synthetic identity, in-memory state and
test-only provider configuration. It never invokes backend Program/.env, SQL,
Keycloak or SMTP. No production fixture switch exists. Real environment gates,
remaining administration modules and overall deployment acceptance remain open.
