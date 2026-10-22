# Native Blazor migration parity tracker

2026-10-22. Status is evidence-based, not inferred from generated markup.
Allowed states: NOT_STARTED, IN_PROGRESS, PARITY, INTENTIONAL_CHANGE, BLOCKED.
React remains the active reference; no cutover is approved by this document.
See BLAZOR-MIGRATION-INVENTORY.md for exact API/validation/state contracts and
BLAZOR-MIGRATION-AUTHORIZATION.md for identity/application permission separation.

## Module matrix

| Route | Old React feature → new Blazor feature | API | Policy | Evidence | Status |
|---|---|---|---|---|---|
| `/app` | role-aware dashboard → native template shell/cards | auth/profile | authenticated, DB module view permissions | physical card/route integrated; remaining module cards pending | IN_PROGRESS |
| `/app/physical-persons` | specialized list/two-step form/verify/deactivate → native module | related-persons, verification | FL_* | physical closure report:58 frontend tests,14 FL+2 Guest browser cases | PARITY |
| same route | linked records and tree → unified form and native tree | detailed, relationship-tree, related-persons mutations | FL_* | actual backend create/edit/deactivate/tree reload browser flow | PARITY |
| same route | physical import/export → backend file workflows | related-persons/import, export | FL_CREATE / FL_VIEW | valid/invalid XLSX, metadata/bytes, Guest download, safe error feedback | PARITY |
| `/app/legal-persons` | resident/nonresident CRUD/verify/import/export → native module | legal-entities, verification | PL_* | legal closure:17 new frontend tests,12 PL+2 Guest browser cases; preserves Matbroj/status | PARITY |
| `/app/limits` | legal lookup/server-derived capital/CRUD/export → native module | limiti, legal-entities/*/limit-form-data | LIMITI_* | limits closure:13 added frontend tests,10 limiti+1 Guest browser cases, current-module regressions | PARITY |
| `/app/capital` | optional singleton read/upsert/export → native module | capital | KAPITAL_VIEW / EDIT | capital closure:12 added frontend tests,7 Kapital+7 Admin browser cases,1 Guest case | PARITY |
| `/app/reports` | archive/generate/download → native reporting module | reports | authenticated read; DB KAPITAL_CREATE generate/persist | reports closure:15 added frontend tests,9 Kapital+9 Admin+2 Guest browser cases; backend12 HTTP preserved | PARITY |
| `/app/notifications` | email log → native recorded-notification list | email-log | DB KAPITAL_VIEW | notifications closure:6 browser cases × 6 current DB roles, no SMTP delivery claim or HTML rendering | PARITY |
| `/app/admin/period` | lock/request/process/respond → native period workflow | period-lock | existing Admin route + CapitalWrite API | pending tests | NOT_STARTED |
| `/app/admin/users` | functional role/user administration → native module | users, roles | ADMINISTRATION_* | pending tests | NOT_STARTED |
| `/app/admin/code-lists` | category/value CRUD/import → native module | code-lists | existing Admin route, ADMINISTRATION_MANAGE mutations | pending tests | NOT_STARTED |
| `/app/admin/audit` | actual business audit → native table/filter/paging | audit-logs | admin.only | pending tests | NOT_STARTED |
| `/app/profile` | identity/effective permissions → template-based profile | auth/profile | authenticated | signed-out HTTP smoke + effective permission tests; integrated identity/browser pending | IN_PROGRESS |
| `/app/guide` | role-aware business guide → app-owned localized content | effective profile only | visible modules only | pending tests | NOT_STARTED |
| all | BS/EN business resources → RBI.Localization catalogs | Accept-Language on API calls | no permission grant | pending culture tests | NOT_STARTED |
| reports backend | old read rule → authenticated read/download, DB-controlled persisted generation | reports | approved reporting exception | 12 integration + 16 focused unit; full backend 274 PASS | INTENTIONAL_CHANGE |
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
Remaining administration/guide modules are incomplete.
Real SQL/SSO/OCP parity is not claimed.

## Required end-to-end closure ledger

Each item needs a named reproducible test/receipt, desktop and 375px coverage,
and safe error/accessibility checks before PARITY. No screenshots/assertions
may be weakened to manufacture completion.

| # | Required flow | Route | Native evidence / status |
|---|---|---|---|
| 1 | home, only authorized modules | /app | NOT_STARTED |
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
| 15 | reports read/download vs authorized generation | reports | PARITY: Guest existing archive/files with no generation; Kapital/Admin persisted daily/monthly, no automatic POST retry, failed-download and uncertain-save recovery |
| 16 | lock period | admin/period | NOT_STARTED |
| 17 | request unlock | admin/period | NOT_STARTED |
| 18 | process/respond to unlock request | admin/period | NOT_STARTED |
| 19 | notifications/provider empty state | notifications | NOT_STARTED |
| 20 | create user | admin/users | NOT_STARTED |
| 21 | assign functional permissions via roles | admin/users | NOT_STARTED |
| 22 | deactivate/reactivate user | admin/users | NOT_STARTED |
| 23 | delete user with confirmation | admin/users | NOT_STARTED |
| 24 | code-list CRUD/preview/import/referenced-value rejection | admin/code-lists | NOT_STARTED |
| 25 | audit visible after mutation | admin/audit | NOT_STARTED |
| 26 | current identity/permissions profile | profile | NOT_STARTED |
| 27 | BS/EN switching/persistence | all | IN_PROGRESS: physical route/cookie/backend header proven; other modules pending |
| 28 | backend unavailable, safe recovery | all | IN_PROGRESS: physical module proven; other modules pending |
| 29 | auth unconfigured/unavailable, no fake grants | all | NOT_STARTED |
| 30 | period-locked mutation returns423 and visible explanation | physical/legal/limits | IN_PROGRESS: physical middleware423/no writes proven; legal/limits pending |

## Release gates still pending

Native restore/build/publish/startup; module-specific tests; browser E2E;
accessibility/responsiveness; approved integrated identity/SQL/OCP environment;
no secrets/raw exception disclosure; no reusable component copies; no frontend
DB access; deployment replacement proof. Keep both existing source and deployment
reference intact until parity is accepted; never deploy two canonical frontends.
