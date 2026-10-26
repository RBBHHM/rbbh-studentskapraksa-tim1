# Native Blazor migration parity tracker

Inventory baseline2026-10-22; local closure updated2026-10-23.
Status is evidence-based, not inferred from generated markup.
Allowed states: NOT_STARTED, IN_PROGRESS, PARITY, INTENTIONAL_CHANGE, BLOCKED.
React remains the active reference; no cutover is approved by this document.
See BLAZOR-MIGRATION-INVENTORY.md for exact API/validation/state contracts and
BLAZOR-MIGRATION-AUTHORIZATION.md for identity/application permission separation.

## Module matrix

| Route | Old React feature → new Blazor feature | API | Policy | Evidence | Status |
|---|---|---|---|---|---|
| `/app` | role-aware dashboard → native template shell/cards | auth/profile | authenticated, DB module view permissions | shell-profile-guide cases across all6 DB roles; correct read/Admin cards despite forged provider grants | PARITY |
| `/app/physical-persons` | specialized list/two-step form/verify/deactivate → native module | related-persons, verification | FL_* | physical closure report:58 frontend tests,14 FL+2 Guest browser cases | PARITY |
| same route | linked records and tree → unified form and native tree | detailed, relationship-tree, related-persons mutations | FL_* | actual backend create/edit/deactivate/tree reload browser flow | PARITY |
| same route | physical import/export → backend file workflows | related-persons/import, export | FL_CREATE / FL_VIEW | valid/invalid XLSX, metadata/bytes, Guest download, safe error feedback | PARITY |
| `/app/legal-persons` | resident/nonresident CRUD/verify/import/export → native module | legal-entities, verification | PL_* | legal closure:17 new frontend tests,12 PL+2 Guest browser cases; preserves Matbroj/status | PARITY |
| `/app/limits` | legal lookup/server-derived capital/CRUD/export → native module | limiti, legal-entities/*/limit-form-data | LIMITI_* | limits closure:13 added frontend tests,10 limiti+1 Guest browser cases, current-module regressions | PARITY |
| `/app/capital` | optional singleton read/upsert/export → native module | capital | KAPITAL_VIEW / EDIT | capital closure:12 added frontend tests,7 Kapital+7 Admin browser cases,1 Guest case | PARITY |
| `/app/reports` | archive/generate/download → native reporting module | reports | known active AppUser read; DB KAPITAL_CREATE generate/persist | original closure plus reconciled unknown/inactive/profile-outage membership denial; actual archive/download/persistence preserved | PARITY |
| `/app/notifications` | email log → native recorded-notification list | email-log | DB KAPITAL_VIEW | notifications closure:6 browser cases × 6 current DB roles, no SMTP delivery claim or HTML rendering | PARITY |
| `/app/admin/period` | lock/request/process/respond → native period workflow | period-lock | existing DB Admin UI route + unchanged CapitalRead/Write API | period closure:12 Admin browser cases +5-role denial,21 focused period tests; approved10..490 response limit | PARITY |
| `/app/admin/users` | functional role/user administration → native module | users, roles | existing DB Admin route + ADMINISTRATION_* | users closure:9 Admin browser cases +5-role forged denial, create/multi-role/state/delete and28 HTTP safety regressions | PARITY |
| `/app/admin/code-lists` | category/value CRUD/import → native module | code-lists | existing Admin route, ADMINISTRATION_MANAGE mutations | code-list closure:10 Admin +5-role denial cases,28 API/draft cases,5 HTTP status regressions, live business dropdown refresh | PARITY |
| `/app/admin/audit` | actual business audit → native table/filter/paging | audit-logs | exact admin from current DB profile | audit closure:5 Admin browser cases + denied/forged-claim coverage for5 other DB roles; real AuditService metadata/paging | PARITY |
| `/app/profile` | identity/effective permissions → template-based profile | auth/profile | authenticated | current claims + backend grants, outage/retry and375px/Axe per role; live SSO remains external | PARITY |
| `/app/guide` | role-aware business guide → app-owned localized content | effective profile only | visible modules only |7 unit/resource cases; keyboard/reporting distinction/Axe/mobile/BSEN per role | PARITY |
| all | BS/EN business resources → RBI.Localization catalogs | Accept-Language on API calls | no permission grant | every module's culture cases plus common cookie/reload/navigation checks across6 roles; no DE advertisement | PARITY |
| reports backend | old read rule → known active Guest baseline read/download, DB-controlled persisted generation | reports | approved reporting exception, reconciled application-membership boundary | current protected HTTP and six-role browser coverage; older12 integration/16 unit checkpoint remains historical | INTENTIONAL_CHANGE |
| users / period UX | own role action disabled; locked Capital/verification unavailable | users, capital, verification, period-lock | self-management forbidden; authoritative period middleware | reconciled HTTP/SQL tests and native disabled/recovery/stale-editor cases | INTENTIONAL_CHANGE |
| auth backend | provider permission leakage → exclusively DB-owned app grants | all protected endpoints | current DB matrix | forged grants red201→green403; repeated transform revocation tests | INTENTIONAL_CHANGE |

PARITY above means local module contract/browser equivalence, not integrated
production acceptance. The historical physical checkpoint is recorded in
BLAZOR-MIGRATION-PHYSICAL-CLOSURE.md (332 .NET/16 browser). The subsequent legal
checkpoint is BLAZOR-MIGRATION-LEGAL-CLOSURE.md (352 .NET/30 browser). The next
limits checkpoint is BLAZOR-MIGRATION-LIMITS-CLOSURE.md (365 .NET/41 browser).
The following capital checkpoint is BLAZOR-MIGRATION-CAPITAL-CLOSURE.md
(377 .NET/56 browser). Physical, legal, limits and capital modules are locally
closed. Reporting follows in BLAZOR-MIGRATION-REPORTS-CLOSURE.md (392 .NET/76
browser), including the approved read/download vs generate/persist split.
Notifications follows in BLAZOR-MIGRATION-NOTIFICATIONS-CLOSURE.md
(400 .NET/112 browser), including a source-backed registry whitespace-search fix.
Audit follows in BLAZOR-MIGRATION-AUDIT-CLOSURE.md (407 .NET/122 browser),
including DB-owned Admin access and encoded audit metadata.
At the historical audit checkpoint, remaining administration/guide modules were
incomplete; their subsequent closure is recorded below.
The user-administration safety blocker is RESOLVED in
BLAZOR-MIGRATION-USERS-SAFETY-BLOCKER.md:28 HTTP regressions, 305 backend tests
and 435 total .NET tests PASS at that checkpoint. Its original 2 FAIL/1 PASS
discovery remains historical evidence. Native Users follows in
BLAZOR-MIGRATION-USERS-CLOSURE.md (450 .NET/141 browser), including safe native
confirmations, multi-role workflows and current DB authorization.
The subsequent period clarification red discovery (1 PASS/2 FAIL) is historical
evidence in BLAZOR-MIGRATION-PERIOD-RESPONSE-BLOCKER.md, now RESOLVED under the
approved10..490 rule. BLAZOR-MIGRATION-PERIOD-CLOSURE.md records the native
period checkpoint (478 .NET/158 browser). The next code-list checkpoint has512
.NET passes and15 focused browser passes, including actual reference-data
consumption by a business form. Guide was still open at that checkpoint. The
subsequent shell/profile/guide checkpoint closes local modules with520 .NET and
209 browser passes. The red discoveries are no longer uncommitted failing tests.
Real SQL/SSO/OCP parity is not claimed.

## Required end-to-end closure ledger

Each item needs a named reproducible test/receipt, desktop and 375px coverage,
and safe error/accessibility checks before PARITY. No screenshots/assertions
may be weakened to manufacture completion.

| # | Required flow | Route | Native evidence / status |
|---|---|---|---|
| 1 | home, only authorized modules | /app | PARITY:actual DB baseline read cards and Admin-only cards/navigation across6 roles, including forged provider grants |
| 2 | create resident physical person | physical-persons | PARITY: resident browser case + draft checks |
| 3 | create nonresident physical person | physical-persons | PARITY: real backend save and reload |
| 4 | duplicate JMBG/passport/FBA validation | physical-persons | PARITY: field query contracts, invalid JMBG and duplicate passport browser case; shared duplicate path validates all three |
| 5 | add/manage unified family link and tree | physical-persons | PARITY: create/edit/remove/tree reload browser case |
| 6 | edit physical person | physical-persons | PARITY: self-exclusion, explicit flags and reload |
| 7 | verify physical person | physical-persons | PARITY: actual status transition and action refresh |
| 8 | deactivate physical person | physical-persons | PARITY: backend soft-delete no longer listed after reload |
| 9 | physical XLSX export | physical-persons | PARITY: backend filename/bytes, Guest read |
| 10 | valid/invalid physical workbook import | physical-persons | PARITY: real ClosedXML validation/import, safe result display |
| 11 | create/edit/verify legal entity | legal-persons | PARITY: actual backend CRUD/status, preserved Matbroj, soft delete, resident/nonresident and duplicate validation |
| 12 | legal export | legal-persons | PARITY: backend XLSX filename/ZIP bytes, real import/schema errors, Guest download |
| 13 | create/edit/delete limit | limits | PARITY: real integer-ID CRUD, search/form-data, zero/null/negative amounts, filtered XLSX and permanent-delete confirmation |
| 14 | capital view/upsert authorization | capital | PARITY: unset/null contract, real stable singleton upsert, Kapital/Admin write and Guest read/export |
| 15 | reports read/download vs authorized generation | reports | PARITY: known active Guest archive/files; unknown/inactive denied including forged provider claims; profile failure invents no membership; Kapital/Admin persisted daily/monthly, no automatic POST retry, failed-download/uncertain-save recovery |
| 16 | lock period | admin/period | PARITY:real current lock/unlock and audit, no fabricated open fallback |
| 17 | request unlock | admin/period | PARITY:real PENDING record and current backend period values |
| 18 | process/respond to unlock request | admin/period | PARITY:reject/info/respond/new PENDING/approve and audit; approved10..490 boundaries with unchanged prefix/schema |
| 19 | notifications/provider empty state | notifications | PARITY: recorded rows, safe body search and explicit SMTP empty-state semantics across all6 DB roles |
| 20 | create user | admin/users | PARITY: real local backend create/reload, corporate-domain/required validation and duplicate rejection; integrated Keycloak lookup gate remains |
| 21 | assign functional permissions via roles | admin/users | PARITY: actual catalog GUIDs, multiple roles, replacement and non-final empty-role/Guest removal |
| 22 | deactivate/reactivate user | admin/users | PARITY: native confirmations/cancel, actual backend state/audits, independent self/final-Admin HTTP protection |
| 23 | delete user with confirmation | admin/users | PARITY: native permanent-delete/cancel and real record/audit, self/final-Admin server rejection |
| 24 | code-list CRUD/preview/import/referenced-value rejection | admin/code-lists | PARITY:real CRUD/soft-delete/audit, guarded referenced value/category rejection, real XLSX preview/cancel/partial valid-row import, live business dropdown consumption |
| 25 | audit visible after mutation | admin/audit | PARITY: real AuditService entry, metadata/search/paging, DB Admin only and forged-provider denial |
| 26 | current identity/permissions profile | profile | PARITY:template AuthenticationState identity, actual DB grants, forged provider-role rejection, permission outage/retry; live identity integration external |
| 27 | BS/EN switching/persistence | all | PARITY:module BS/EN cases plus common cookie/reload/navigation/header cases across6 roles |
| 28 | backend unavailable, safe recovery | all | PARITY:each data module's outage/retry, no synthetic empty/open/zero data; unknown mutation outcomes explicitly warn before resubmission |
| 29 | auth unconfigured/unavailable, no fake grants | all | BLOCKED for real SSO acceptance; local signed-out startup/routes, missing/partial configuration, no anonymous proxy, profile failure and forged-grant cases PASS; requires approved realm/client/shared cache |
| 30 | period-locked mutation returns423 and visible explanation | physical/legal/limits/capital/verification | PARITY: backend route/method + real HTTP matrix; native Capital/verify disabled/recovery and stale-editor423 cases; report-generation exclusion preserved |

## Final local gate and remaining integrated release gates

The previous2f0a04f checkpoint was528/528 .NET (203 frontend +216 unit +109
integration), backend325, and215/215 browser (Admin70, gost25, Kapital33, PL29,
Limiti27, FL31). The current reconciled .NET gate is594/594 (210 frontend +248
unit +136 integration), backend384, no skips. Current browser gate:237/237
(Admin75, gost27, Kapital38, PL33, Limiti29, FL35); workers1,
retries0, final JSON stats show no failures/flaky/skips. Build with analyzers,
changed-new-C# format verification, Release publish, published signed-out startup
on14 routes, actual RBI CSS and absent unconfigured login handler PASS.
See REACT-TO-BLAZOR-FINAL-HANDOFF.md for receipts, the FL setup correction and
all environment limitations. Module Axe/375px/culture/error checks remain intact.
The final completion adds real platform localization discovery/key checks and a
six-role protected-controller matrix with forged-provider claims and no-write
assertions. The earlier520/209 checkpoint above remains historical evidence.

Real SSO/shared cache/DataProtection, SQL concurrency/constraints, SMTP, private
package publication, deployed runtime/security acceptance and
OCP/replacement deployment proof remain BLOCKED on approved infrastructure and
owner acceptance. No reusable component copies, frontend DB/Excel access or
logged credentials were introduced. Keep existing source/deployment reference
intact until cutover is accepted; never deploy two canonical frontends.
Pkcs NU1902/NU1903 are locally remediated with9.0.21; JWT/OIDC HTTP metadata is
restricted to Development loopback. See BLAZOR-MIGRATION-COMPLETION-SECURITY.md.
React has only the new MIGRATION_REFERENCE marker; no implementation was removed.
The tested native publish/manifest/ZIP and operator retirement gates are documented
in BLAZOR-MIGRATION-RETIREMENT-AND-DEPLOYMENT.md.

SQL qualification: five disposable SQL Server LocalDB last-Admin tests now pass
the real transaction-owned application-lock path; production multi-replica SQL
acceptance is still external. F3 remains PARTIAL: subject-first/verified unique
normalized-email lookup does not persist fallback sub. Real email_verified,
approved subject-link backfill and Keycloak export/data reconciliation remain
mandatory before production SSO cutover. PKCS9.0.21 is locally retained; fresh
production vulnerability-feed confirmation is external, without warning suppression.
