# Regulatory reporting native migration checkpoint

2026-10-22. Local module closure, not production/environment acceptance.

## Approved authorization, implemented end to end

| Operation | Native and backend policy |
|---|---|
| Existing archive/read/download; all/client export | Every authenticated user |
| Generate/persist daily or monthly report | Current DB/application KAPITAL_CREATE grant: Kapital or Admin |

Keycloak supplies identity, not a direct four-role application authorization
model. The route, navigation and dashboard reporting entry are authenticated,
not gated by kapital.read. Current backend profile permissions control generation
only. A profile failure disables generation but does not remove authenticated
archive/download access. Backend enforcement remains authoritative. Existing
read/download **audit entries** are preserved; they are not new persisted business
report snapshots and are not a reason to restrict approved downloads.

## Workflow/data preservation

- Raw typed daily/monthly archives retain source pageSize=100, combined
  created-at sorting and all summary/author fields. Independent labelled daily
  and monthly pagination exposes the backend's existing subsequent pages.
  Pagination components contain no API logic; fetching stays app-owned.
- Daily/monthly POSTs retain the actual ReportController/ReportService persistence,
  audit, snapshot and generated-XLSX workflow. No service, schema, calculation,
  report ID generation or snapshot building moved into Blazor.
- The current backend builds generated snapshots from **ClientLimits**. Current
  client/all exports use the existing **Limiti** contract. Both sources are
  represented separately in the isolated fixture. This migration does not merge
  datasets or silently redefine the financial/data lineage. Real SQL data lineage
  must be verified in the integrated environment.
- Monthly inputs follow the actual API's 2000..current UTC year and 1..12 bounds,
  rather than the old UI's incompatible 2020..next-year range. No additional
  current-month restriction is invented. Numeric client identifiers retain the
  source 1..13-digit contract. Attachments keep backend filenames/bytes.
- Successful creation refreshes the archive and initiates snapshot download.
  A failed subsequent download explicitly says the report **was saved** and can
  be downloaded again. It does not repeat generation.
- An injected audit failure after the real snapshot save but before HTTP201
  proves the uncertain-outcome path: warn that persistence may have occurred,
  refresh the archive, then download the existing record. Neither the app nor
  packed transport automatically retries a generation POST. This does not add
  idempotency or change the backend's existing transaction/audit behavior.
- Existing period middleware excludes reports, as it excludes capital; no new
  period-lock business rule is introduced. A generation test preserves that scope.
- Honest empty/loading/error/retry, safe errors, strongly typed selects, labelled
  controls, automated WCAG, visually inspected 375px layout, BS/EN cookies and API
  culture propagation. Source 720px report-table width remains inside native scroll.

## Local evidence

- Frontend 115 tests (15 added here), backend 210 unit + 67 integration = 392/392,
  no skips: `dotnet test RelatedPartiesRegister.Migration.slnx --no-restore`.
- Existing backend reporting HTTP authorization suite remains 12/12: ordinary
  authenticated read/download and denied generation; Kapital/Admin persisted
  daily/monthly generation; unauthenticated/forged-grant denial. Historical
  authorization checkpoint totals remain in BLAZOR-MIGRATION-AUTHORIZATION.md.
- Browser: kapital 16/16 and admin 16/16 (capital 7 + reporting 9 each); gost 8/8
  across migrated modules; PL 12/12, limiti 10/10, FL 14/14. Total **76/76**,
  workers=1/retries=0. Reporting cases explicitly verify persisted IDs, author,
  type/date/summary/snapshot, failed-download recovery, uncertain save without
  retry, all/client files, pagination and profile-independent read access.
- Frontend Release publish, changed-C# format verification, git diff --check.
  The existing formatter workspace-load warning and backend Pkcs NU1902/NU1903
  advisories are not suppressed or described as zero warnings.

Reproduce browser coverage with `npm test` from `tests/Web.Blazor.Browser` and
BROWSER_FIXTURE_ROLE set to kapital/admin/gost/PL/limiti/FL. Receipts are ignored
under `artifacts/migration-template/reports-*.log` and per-role JSON/traces/images
under `artifacts/migration-browser/`. The initial option-bound assertion incorrectly
used an input-only matcher; it now asserts the same option value attribute.
No assertions, timeouts, retries, screenshots or production geometry were weakened.

The dedicated test executable uses real controllers/services/DB role transformation
and real audit, with InMemory storage and test-only fault injection/identity.
It never executes backend Program/.env, real SQL, SMTP or Keycloak. Production
has no fixture switch, fake identity or browser token. Remaining modules and
SSO/cache/SQL/OCP integration remain open; no React/deployment cutover is authorized.
