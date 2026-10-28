# Legal-entity native migration checkpoint

2026-10-22. Local module closure only; React/deployment remain unchanged.

The native `/app/legal-persons` route uses packed RBI components, Forms,
Localization, the approved ApiCore multipart/binary transport and server-side
Authentication token propagation. Keycloak remains identity-only; the real
backend profile supplies PL_VIEW/CREATE/EDIT/DELETE. The route and every mutation
event check those effective permissions. Backend authorization is authoritative.

## Preserved contracts

- Raw `legal-entities` paged response and GUID detail/create/update/soft-delete;
  verification remains `verification/legal-person/{id}`. No generic DTO envelope,
  client persistence, EF, workbook parsing or copied platform components.
- Resident/nonresident conditional identifiers, backend tax checksum, optional
  numeric MaticniBroj, codebook basis/description, explicit nullable Yes/No,
  required start date, optional end date and date ordering. The existing source
  basis-description autofill behavior and seven fallback bases are retained.
- Edit fetches fresh detail and preserves **Matbroj**, which is distinct from
  editable **MaticniBroj**. The update request does not override Status. Browser
  evidence verifies a verified record remains verified after editing and the
  server-only Matbroj remains intact. Soft deletion is checked using the test
  store's IgnoreQueryFilters, not inferred from disappearing table markup.
- XLSX import/export stays backend-owned. Download checks filename/ZIP bytes;
  import exercises real ClosedXML schema checks, counts and persisted refresh.
  Unknown backend errors/headers are never rendered; safe existing import
  guidance is localized by the application.
- Search retains scalar/audit-metadata matching on the displayed API page.
  The source React showed only the default first 20 records. Native pagination
  exposes the existing paged backend, with an explicit current-page search label
  rather than silently replacing it with the narrower server-side search.
  Pagination remains a pure package UI component; page fetching is app-owned.
- Honest loading/empty/outage/retry/forbidden/423 states, confirmations,
  duplicate rejection without persistence, modal Escape/return focus, labelled
  fields, WCAG automated checks, mobile horizontal table region and BS/EN cookie
  persistence/API Accept-Language propagation.

## Reproducible local evidence

From `tests/Web.Blazor.Browser`, set `BROWSER_FIXTURE_ROLE` to PL, gost or FL
and run `npm test`. The committed configuration enforces workers=1/retries=0.
The separate test executable hosts real controllers/services/DB permission
transformation/audit/period middleware with isolated InMemory state and a
test-only identity. It never executes backend Program/.env, SQL, real Keycloak
or SMTP. There is no production fixture/identity switch.

- PL: 12/12 browser tests (`legal-entities.spec.ts`).
- gost: 4/4 (two legal + two existing physical read-only/hostile-import tests).
- FL: 14/14 existing physical/family browser regressions.
- Frontend: 75 tests (17 new legal draft/packed-client tests); backend 210 unit
  and 67 integration tests. Total 352. No skips.
- `dotnet test RelatedPartiesRegister.Migration.slnx --no-restore`.
- `git diff --check`; changed-C# format verification. Workspace-load warnings
  must not be described as a zero-warning formatter run.

Ignored receipts: `artifacts/migration-template/legal-browser-green.log`,
`legal-guest-browser.log`, `legal-physical-regression.log`,
`legal-final-regression.log` and per-role JSON/traces/screenshots under
`artifacts/migration-browser/`. The 375px legal screenshot was visually inspected.
An initial test lookup used the wrong accessible copy; it was corrected to the
actual localized labels. An initial soft-delete proof missed the query filter;
only that test-only inspection query changed. No retries, increased timeouts,
force-clicks, screenshots or business assertions were weakened.

The separate HTTP validation correction is documented in
BLAZOR-MIGRATION-ISSUES.md with red/green receipts. Existing NU1902/NU1903 Pkcs
advisories remain unresolved and unsuppressed. SQL/SSO/cache/OCP integration,
remaining modules and overall cutover remain open; this is not a production
acceptance claim.
