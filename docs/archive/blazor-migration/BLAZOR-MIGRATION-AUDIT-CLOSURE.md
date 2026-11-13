# Native audit checkpoint

2026-10-22. Local module closure only; no production/deployment cutover.

The native `/app/admin/audit` route consumes the actual paged `api/audit-logs`
contract, including backend action/area display labels, change summary, actor,
IP, timestamps and record metadata. It preserves the source page size of 200,
scalar search on the displayed page and server paging. There are no write or
export controls. Values remain encoded text, never interpreted HTML.

Access is based on the current database-backed profile's exact `admin` role,
not provider role claims. Navigation, dashboard and the route share this rule.
Each other current database role is denied before the page requests audit data;
the real API also returns 403. A forged provider admin/permission claim remains
visible in the test frontend principal but grants neither UI nor backend access.

## Evidence

- 130 frontend + 210 backend unit + 67 integration = **407/407 .NET**, no skips.
  Seven frontend cases were added for access, raw paging/search/error contracts
  and the signed-out route. Existing reporting authorization tests remain green.
- Five Admin browser cases cover empty/accessibility/no write controls, actual
  AuditService output and metadata search, encoded hostile text, 201-record
  paging, safe outage/retry, 375px and BS/EN backend display labels.
- One denied-access case runs for each of the five non-Admin database roles,
  including forged provider claims and direct real-API probes.
- Full browser gate: Admin 27, gost 15, kapital 23, PL 19, limiti 17, FL 21 =
  **122/122 PASS**, workers=1/retries=0. All previous module suites are retained.
- Frontend Release publish, changed-C# format verification and git diff --check
  passed. The existing formatter workspace-load warning and backend/test-host
  Pkcs NU1902/NU1903 advisories remain unresolved and unsuppressed.

An initial resource boot failure exposed duplicate unused starter audit keys;
the obsolete keys were removed, without relaxing strict resource validation.
The mobile test initially selected language inside a closed navigation drawer;
it now opens the drawer through its normal toggle before selecting. No product
geometry, assertions, retries or timeouts were changed to bypass that failure.
The final mobile screenshot was inspected.

Reproduce with `dotnet test RelatedPartiesRegister.Migration.slnx --no-restore`
and `npm test` in tests/Web.Blazor.Browser with BROWSER_FIXTURE_ROLE set to each
role. Ignored receipts are under `artifacts/migration-template/audit-*.log` and
`artifacts/migration-browser/`. The isolated host uses real controllers,
AuditService and permission transformation with synthetic identity and unique
in-memory stores. It never invokes backend Program/.env, SQL, Keycloak or SMTP.
There is no production fixture identity switch. Other administration/guide
modules and integrated environment acceptance remain open.
