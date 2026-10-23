# Related Parties Register: migration authorization matrix

Baseline: `68d3e7f5c580e8da307a87ef017cb8a0099f5ec8`, branch
`release-v1.0.6`. Business reporting decision approved by the owner on 2026-10-22.
The checked-out source, not the older four-Keycloak-role description, is authoritative.

## Identity versus application authority

| Concern | Authority | Migration rule |
|---|---|---|
| Authenticated identity, subject, email | Keycloak in integrated environments | Blazor uses RBI.Authentication and ClaimsPrincipal, not keycloak-js or browser protocol code |
| Application roles/permissions | Application DB: AppUsers, UserRoles, Roles, RolePermissions, Permissions | Backend checks protected operations; Blazor consumes the backend's effective permissions, never reads the DB |
| Effective identity/permissions wire contract | Authenticated `GET /api/auth/profile` | Keep existing `{ userId, fullName, username, email, roles, permissions }`; do not replace with realm roles |
| Unassigned authenticated user | Existing `gost` baseline | Read access only; authentication is not a mutation grant |
| Unconfigured local Blazor | Explicit signed-out/unconfigured state | No fabricated production identity; authenticated fixtures belong only in tests |

The backend already stores `FL`, `PL`, `kapital`, `limiti`, `admin`, `gost`.
These wire/database names remain unchanged; display names may use FL, PL,
Kapital, Limiti, Admin and Guest. No DB migration, rename, reseed or Keycloak
role reassignment is part of this bounded authorization change.

The native RBI permission interface requires semantic dot names. An app-owned
name adapter maps e.g. `fl.update`→`FL_EDIT`, `reports.generate`→`KAPITAL_CREATE`
and `administration.manage`→`ADMINISTRATION_MANAGE`. It only checks those codes
against the effective backend response; no frontend role→grant map is added.
The real RBI authorization pipeline is regression-tested for this boundary.

## Approved reporting exception

Generating a report persists an archived snapshot through ReportService, unlike
reading/downloading an existing report. It is an explicit exception to the
platform's general "reporting is read-only for all" rule.

| Screen/action | HTTP contract | Required backend policy | Allowed users |
|---|---|---|---|
| List daily reports | GET `/api/reports/daily?page&pageSize` | `reports.read`: authenticated | Every authenticated application user |
| List monthly reports | GET `/api/reports/monthly?page&pageSize` | `reports.read` | Every authenticated application user |
| Download existing snapshot | GET `/api/reports/export/generated/{reportId}` | `reports.read` | Every authenticated application user |
| Download client/all clients | GET `/api/reports/export/client/{identifier}`, `/export/all-clients` | `reports.read` | Every authenticated application user |
| GENERATE/PERSIST daily | POST `/api/reports/daily` | `reports.generate`: existing DB `KAPITAL_CREATE` permission | Kapital and Admin |
| GENERATE/PERSIST monthly | POST `/api/reports/monthly/{year}/{month}` | `reports.generate`: existing DB `KAPITAL_CREATE` permission | Kapital and Admin |

GET export audit entries remain backend-owned: an audit side effect does not
grant permission to create a new report snapshot. Generation, persistence and
audit workflow remain in ReportController/ReportService, not in Blazor pages.
Reporting read access never grants period/user/code-list administration.

## Remaining business areas (actual source policies)

| Area | READ | CREATE | UPDATE / VERIFY | DELETE | ADMINISTER |
|---|---|---|---|---|---|
| Physical persons / unified family records | `FL_VIEW` | `FL_CREATE` | `FL_EDIT` | `FL_DELETE` | None implied |
| Legal entities | `PL_VIEW` | `PL_CREATE` | `PL_EDIT` | `PL_DELETE` | None implied |
| Limits | `LIMITI_VIEW` | `LIMITI_CREATE` | `LIMITI_EDIT` | `LIMITI_DELETE` | None implied |
| Global Capital | `KAPITAL_VIEW` | Existing PUT upsert requires `KAPITAL_EDIT` | `KAPITAL_EDIT` | No capital delete endpoint exists | None implied |
| Users / roles | `ADMINISTRATION_VIEW` | `ADMINISTRATION_MANAGE` | `ADMINISTRATION_MANAGE` | `ADMINISTRATION_MANAGE` | Admin |
| Code lists | `CODE_LISTS_VIEW`; React admin route is narrower | `ADMINISTRATION_MANAGE` | `ADMINISTRATION_MANAGE` | `ADMINISTRATION_MANAGE` | Admin |
| Audit | `ADMINISTRATION_MANAGE` / `admin.only` | No frontend write | None | None | Admin |
| Notifications | Existing `capital.read`; guest DB baseline includes view | No frontend email sending | None | None | None implied |
| Period | Existing `capital.read` for status/requests | Existing `capital.write` for lock/unlock/request/process operations | Same | No general delete endpoint | React direct route additionally requires Admin |

The existing period API/React guard discrepancy is recorded, not silently
broadened by the reporting exception. Preserve the narrower existing route
guard; any future permission change needs its own reviewed change/tests.

Historical names `physical-persons`, `legal-persons`, `limits`,
`regulatory-reporting` appear in existing explicit development seed conversion.
In particular, its existing regulatory-reporting→kapital conversion and
all-modules→admin promotion are **not** automatically executed or generalized
by this migration. Production authorization data must not be silently reseeded.

## Regression evidence

Baseline backend tests: 208 unit + 52 integration = 260 passed, no skips.
Baseline React: 13/13 Node tests, localization/lint/build passed (webpack has
two existing asset-size warnings). Corporate NuGet restore fails with NU1301;
an isolated explicit public-source restore succeeds without changing NuGet config.
That restore reports existing NU1902/NU1903 for System.Security.Cryptography.Pkcs
9.0.4. No production security clearance is implied.

`ReportHttpAuthorizationTests` runs real HTTP middleware, ReportController,
ReportService, EF InMemory persistence and the production DB claims transform.
Only identity and store are isolated fixtures; it never runs Program/.env,
SQL Server, Keycloak, SMTP or hosted jobs. It verifies:

- Ordinary authenticated user: daily/monthly GET and real XLSX download 200;
  both generate routes 403 and no additional persisted report.
- Kapital/Admin: daily and monthly generation 201; ID/type/author/snapshot and
  increased report count verified through a separate DB context.
- FL/PL/Limiti cannot generate; unauthenticated reads/downloads return 401.
- Provider role/permission claims and a forged legacy transform marker cannot
  substitute for database authorization.

The last check initially proved a real escalation: expected 403, actual 201
(`artifacts/migration-baseline/report-authorization-red.log`, 11 passed / 1 failed).
The bounded fix discards provider application-role/permission/old-marker claims,
retains identity/email, and recomputes DB permissions even on repeated transforms.
It also removes stale grants without accumulating identities. No schema changes.

Focused green result: 12 HTTP integration + 16 authorization unit tests = 28 PASS,
zero skips. Receipts: `artifacts/migration-auth/green/` and
`artifacts/migration-baseline/report-authorization-green.log`.
This is local authorization evidence, not a real Keycloak or SQL Server test.

Full post-change backend regression: 210 unit + 64 integration = **274 PASS**,
zero failures/skips. Receipts: `artifacts/migration-auth/full-trx/` and
`artifacts/migration-baseline/backend-after-authorization.log`.

## Subsequent native reporting checkpoint

The native route/navigation/dashboard entry now implements the approved split,
without a kapital.read gate on authenticated reporting read access. Backend
profile availability controls generation only; unavailable permission lookup fails
closed for writes but preserves archive/download access. See
BLAZOR-MIGRATION-REPORTS-CLOSURE.md for the subsequent 392 .NET/76 browser
checkpoint, including ordinary-user download, Kapital/Admin actual persisted
generation, safe uncertain outcomes and no automatic generation retry. Existing
download/export audit entries remain; read/download does not create another
business report snapshot. Earlier totals above describe their historical checkpoint.

## Approved user-administration safety correction

The later users safety checkpoint preserves Keycloak identity and DB-owned
permissions unchanged. It rejects self-deactivation/deletion (400) after resolving
the actual local identity link and rejects reducing the last active database
Admin via role replacement/removal, deactivation or deletion (409). Other
authorized user operations retain their existing meaning. SQL API replicas share
a transaction-owned lock around validation, mutation and audit; local InMemory
fixtures use an async gate. See BLAZOR-MIGRATION-USERS-SAFETY-BLOCKER.md for the
resolved finding, HTTP/concurrency evidence and the outstanding live SQL gate.

## Final local six-role HTTP matrix — 2026-10-23

`authorization-matrix.spec.ts` runs once for each current DB role through actual
protected controllers/claims transformation, with forged provider Admin claims.
Forbidden writes must return403; authorized invalid probes reach400/404 business
validation without a write/audit. Positive module specs independently verify
successful mutations and persistence. This supplements, not replaces, the
reporting/user safety HTTP tests and real SSO/SQL acceptance.

| DB role | Read/download | Physical writes | Legal writes | Limits writes | Capital / report generate / period API writes | Users / code-list writes; users / audit reads |
|---|---|---|---|---|---|---|
| gost | yes, existing DB baseline | no | no | no | no | no |
| FL | yes | yes | no | no | no | no |
| PL | yes | no | yes | no | no | no |
| limiti | yes | no | no | yes | no | no |
| kapital | yes | no | no | no | yes | no |
| admin | yes | yes | yes | yes | yes | yes |

All four native Admin routes retain the narrower existing Admin UI guard, including
Period. A Kapital period API permission does not silently grant that Admin page.
Reporting read/download itself only requires authentication; the table's other
read access is the current DB baseline, not an unconditional policy rewrite.
Receipts: completion-matrix-{admin,gost,kapital,PL,limiti,FL}.log, 6/6 PASS.
Earlier test totals and dependency warnings above are historical; current security
remediation is in BLAZOR-MIGRATION-COMPLETION-SECURITY.md.
