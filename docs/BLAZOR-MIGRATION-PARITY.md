# Native Blazor migration parity tracker

2026-10-22. Status is evidence-based, not inferred from generated markup.
Allowed states: NOT_STARTED, IN_PROGRESS, PARITY, INTENTIONAL_CHANGE, BLOCKED.
React remains the active reference; no cutover is approved by this document.
See BLAZOR-MIGRATION-INVENTORY.md for exact API/validation/state contracts and
BLAZOR-MIGRATION-AUTHORIZATION.md for identity/application permission separation.

## Module matrix

| Route | Old React feature → new Blazor feature | API | Policy | Evidence | Status |
|---|---|---|---|---|---|
| `/app` | role-aware dashboard → native template shell/cards | auth/profile | authenticated, DB module view permissions | 17 foundation tests; remaining module cards pending | IN_PROGRESS |
| `/app/physical-persons` | specialized list/two-step form/verify/deactivate → native module | related-persons, verification | FL_* | pending UI/API/E2E | NOT_STARTED |
| same route | linked records and tree → unified form and native tree | detailed, relationship-tree, related-persons mutations | FL_* | pending focused family tests | NOT_STARTED |
| same route | physical import/export → backend file workflows | related-persons/import, export | FL_CREATE / FL_VIEW | pending workbook/download tests | NOT_STARTED |
| `/app/legal-persons` | resident/nonresident CRUD/verify/import/export → native module | legal-entities, verification | PL_* | pending UI/API/E2E | NOT_STARTED |
| `/app/limits` | legal lookup/calculated limits/CRUD/export → native module | limiti, legal-entities/*/limit-form-data | LIMITI_* | pending tests | NOT_STARTED |
| `/app/capital` | singleton read/upsert/export → native module | capital | KAPITAL_VIEW / EDIT | pending tests | NOT_STARTED |
| `/app/reports` | archive/generate/download → native reporting module | reports | reports.read / reports.generate | backend HTTP authorization 12 PASS; native pending | IN_PROGRESS |
| `/app/notifications` | email log → native recorded-notification list | email-log | capital.read | pending tests | NOT_STARTED |
| `/app/admin/period` | lock/request/process/respond → native period workflow | period-lock | existing Admin route + CapitalWrite API | pending tests | NOT_STARTED |
| `/app/admin/users` | functional role/user administration → native module | users, roles | ADMINISTRATION_* | pending tests | NOT_STARTED |
| `/app/admin/code-lists` | category/value CRUD/import → native module | code-lists | existing Admin route, ADMINISTRATION_MANAGE mutations | pending tests | NOT_STARTED |
| `/app/admin/audit` | actual business audit → native table/filter/paging | audit-logs | admin.only | pending tests | NOT_STARTED |
| `/app/profile` | identity/effective permissions → template-based profile | auth/profile | authenticated | signed-out HTTP smoke + effective permission tests; integrated identity/browser pending | IN_PROGRESS |
| `/app/guide` | role-aware business guide → app-owned localized content | effective profile only | visible modules only | pending tests | NOT_STARTED |
| all | BS/EN business resources → RBI.Localization catalogs | Accept-Language on API calls | no permission grant | pending culture tests | NOT_STARTED |
| reports backend | old read rule → authenticated read/download, DB-controlled persisted generation | reports | approved reporting exception | 12 integration + 16 focused unit; full backend 274 PASS | INTENTIONAL_CHANGE |
| auth backend | provider permission leakage → exclusively DB-owned app grants | all protected endpoints | current DB matrix | forged grants red201→green403; repeated transform revocation tests | INTENTIONAL_CHANGE |

Backend test totals do not prove native UI parity. Native template foundation,
signed-out local mode and effective-profile integration are implemented (17 tests
PASS). Full module shell/navigation is not complete. SQL/SSO/browser parity are
not claimed.

## Required end-to-end closure ledger

Each item needs a named reproducible test/receipt, desktop and 375px coverage,
and safe error/accessibility checks before PARITY. No screenshots/assertions
may be weakened to manufacture completion.

| # | Required flow | Route | Native evidence / status |
|---|---|---|---|
| 1 | home, only authorized modules | /app | NOT_STARTED |
| 2 | create resident physical person | physical-persons | NOT_STARTED |
| 3 | create nonresident physical person | physical-persons | NOT_STARTED |
| 4 | duplicate JMBG/passport/FBA validation | physical-persons | NOT_STARTED |
| 5 | add/manage unified family link and tree | physical-persons | NOT_STARTED |
| 6 | edit physical person | physical-persons | NOT_STARTED |
| 7 | verify physical person | physical-persons | NOT_STARTED |
| 8 | deactivate physical person | physical-persons | NOT_STARTED |
| 9 | physical XLSX export | physical-persons | NOT_STARTED |
| 10 | valid/invalid physical workbook import | physical-persons | NOT_STARTED |
| 11 | create/edit/verify legal entity | legal-persons | NOT_STARTED |
| 12 | legal export | legal-persons | NOT_STARTED |
| 13 | create/edit/delete limit | limits | NOT_STARTED |
| 14 | capital view/upsert authorization | capital | NOT_STARTED |
| 15 | reports read/download vs authorized generation | reports | backend proven; native NOT_STARTED |
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
| 27 | BS/EN switching/persistence | all | NOT_STARTED |
| 28 | backend unavailable, safe recovery | all | NOT_STARTED |
| 29 | auth unconfigured/unavailable, no fake grants | all | NOT_STARTED |
| 30 | period-locked mutation returns423 and visible explanation | physical/legal/limits | NOT_STARTED |

## Release gates still pending

Native restore/build/publish/startup; module-specific tests; browser E2E;
accessibility/responsiveness; approved integrated identity/SQL/OCP environment;
no secrets/raw exception disclosure; no reusable component copies; no frontend
DB access; deployment replacement proof. Keep both existing source and deployment
reference intact until parity is accepted; never deploy two canonical frontends.
