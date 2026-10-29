# Related Parties Register: React → native Blazor source inventory

This preserves the inspected historical source contract. Current2026-10-26
hardening removes department semantics from active period APIs/DTOs and makes
Guest virtual; it adds explicit durable-subject confirmation metadata. See the
[current authorization matrix](BLAZOR-MIGRATION-AUTHORIZATION.md) and
[identity/period runbook](IDENTITY-AND-PERIOD-HARDENING-2026-10-26.md).

Inspected baseline: `68d3e7f5c580e8da307a87ef017cb8a0099f5ec8`
(`release-v1.0.6`), 2026-10-22. React remains the reference under `the retired React reference tree`;
the existing API/BL/DL backend remains authoritative. This inventory is not a
parity certificate. Track implementation/evidence in BLAZOR-MIGRATION-PARITY.md.

## Baseline evidence

| Gate | Result before migration |
|---|---|
| Backend Release build | PASS, no errors; existing Pkcs 9.0.4 NU1902/NU1903 warnings |
| Backend tests | 208 unit + 52 integration = 260 PASS; no skips |
| React localization validation | PASS (three inherited catalogs validated, not a German product commitment) |
| React lint / TypeScript | PASS |
| React production build | PASS, two existing retired bundler size warnings |
| React Node tests | 13 PASS; no failures/skips |

Receipts live in ignored `artifacts/migration-baseline/`. Corporate NuGet DNS
was unavailable (NU1301); an isolated explicit nuget.org restore succeeded.
No machine/repository feed configuration was changed. Dependency advisories are
not suppressed and remain a release risk. No SQL/Keycloak/SMTP/OCP integration
or actual production data was exercised by the local baseline.

## Source-of-truth corrections

- Identity is Keycloak; application permissions are already database-owned.
  See [authorization matrix](BLAZOR-MIGRATION-AUTHORIZATION.md). The old brief's
  four-direct-Keycloak-role model is obsolete. The current React auth provider
  already uses `/api/auth/profile`; do not introduce keycloak-js.
- Current physical/legal DTOs and forms no longer contain GCC fields. Earlier
  removal is intentional source history, not a migration omission.
- FamilyManager is a read-only relationship tree. Unified physical-person
  create/edit/delete forms manage the linked records. Separate older family API
  endpoints still exist; do not invent additional UI workflows from their names.
- Capital is one global record (PUT upsert); there is no capital delete endpoint.
- Reports generation persists snapshots. Owner-approved exception: authenticated
  read/download, DB Kapital/Admin permission for generate/persist.
- Startup currently does not print DB passwords and does not automatically
  execute migrations/seeding. Do not reintroduce either from older instructions.
- Notifications return recorded demonstration email entries when the provider is
  not SMTP; SMTP mode deliberately returns an empty recorded list. This is not
  proof of SMTP delivery or a persistent mailbox.

## Routes, components and state coverage to preserve

`the retired React reference tree/src/routes` contains root redirect, root error/not-found, `/app` shell
and the following leaf routes. `app.index` is the dashboard; there is no separate
React family-detail route. All routes must also be directly navigable.

| Route | React implementation / workflow | Existing states and constraints |
|---|---|---|
| `/` → `/app` | index redirect; RegistryShell | identity loading/error/sign-in, mobile menu, theme, language, logout |
| `/app` | app.index, resources cards | role-aware cards, application identity; no invented dashboard metrics |
| `/app/physical-persons` | ResourcePage, FamilyManager, ImportPanel, ExportButton | list/search, two-step create/edit, duplicates, verify, delete confirmation, family tree, import row errors, export, refresh |
| `/app/legal-persons` | ResourcePage, ImportPanel, ExportButton | list/search, resident/nonresident form, verify, deactivate confirmation, import/export/refresh |
| `/app/limits` | ResourcePage | entity search/filter, form-data lookup, create/edit/delete, amounts/date/comment, export/refresh |
| `/app/capital` | CapitalPage | nullable empty record, loading/error, formatted amounts/date, privileged upsert, export |
| `/app/reports` | ReportsPage | daily/monthly combined list, dates, generate then download, client/all downloads, errors/empty/busy |
| `/app/notifications` | ResourcePage | recorded purpose/recipient/subject/status/time; loading/error/empty/search |
| `/app/admin/users` | UsersPage | list/roles, create, replace functional roles, deactivate/reactivate/permanent-delete confirmations |
| `/app/admin/period` | PeriodPage | current period/lock, requests/statuses, lock/unlock, reject/request-info/respond, confirm and refresh |
| `/app/admin/code-lists` | CodeListsPage | category/value select, definition CRUD, active state, import dry-run/confirmation/results, referenced-value rejection |
| `/app/admin/audit` | ResourcePage | real timestamp/action/area/change/user/IP columns, search, loading/error/empty |
| `/app/profile` | ProfilePage | identity names/email, application roles/permissions, signed-out/unavailable |
| `/app/guide` | UserGuidePage | role-aware business rules/statuses/workflows, not platform demo content |

For every collection: preserve loading, normal, empty/no-search-results, safe
backend-error/retry, authorization-denied and post-mutation success/refresh.
For every mutation: validation errors, busy/double-submit prevention, cancel,
server rejection, and period-lock 423 where applicable. Read-only pages have no
create/edit/delete state to invent. Preserve confirmations; use accessible RBI
dialogs rather than copying the old raw modal implementation. Verify mobile
375px and desktop, keyboard/focus/labels/status announcements per module.

The four administration routes use an explicit Admin route guard. The period
API additionally has its existing CapitalWrite policy, not reports.read. Keep
the narrower existing route guard pending a separately reviewed policy change.

## Screen/action → actual HTTP contracts

Paths below are backend-relative. JSON uses the backend's camelCase serializer.
DTO names refer to the existing `Backend/DL/DTO` source, not shared
backend assembly references for the new frontend. Implement narrow wire records.
`BaseResuItController.HandleResult` returns raw `result.Value` (not an envelope).
Some infrastructure errors use an envelope; other failures use ProblemDetails,
field-error arrays or 423. Do not normalize arbitrary object properties into rows.

| Screen/action | Method / endpoint | Request | Successful wire response |
|---|---|---|---|
| Current user | GET `/api/auth/profile` | authenticated identity | userId, fullName, username, email, roles[], permissions[] |
| Existing React sign-in/out | GET `/authentication/login`, `/authentication/logout` | return flow | OIDC/cookie redirects, not JSON; new frontend uses platform endpoints |
| Module descriptors | GET `/api/auth/modules` | none | modules; older four descriptors are not the app's authoritative grant source |
| Physical selector / list | GET `/api/related-persons`, `/detailed` | none | RelatedPersonSummaryDTO[] / RelatedPersonResponseDTO[] |
| Physical detail/tree | GET `/api/related-persons/{id}`, `/{id}/relationship-tree` | GUID | response DTO / recursive RelatedPersonTreeNodeDTO[] |
| Physical duplicate check | GET `/api/related-persons/check-duplicate` | jmbg, passportNumber, fbaId, excludeId query | DuplicateIdentityResponseDTO |
| Physical create/update | POST `/api/related-persons`; PUT `/{id}` | Create/UpdateRelatedPersonDTO | response DTO, create 201 |
| Physical deactivate | DELETE `/api/related-persons/{id}` | GUID | message; backend soft deletion |
| Physical verify | POST `/api/verification/physical-person/{id}` | no body required | updated related-person DTO |
| Physical import | POST `/api/related-persons/import` | multipart `file` | imported, failed, errors[] |
| Physical export | GET `/api/related-persons/export` | none | backend XLSX attachment |
| Existing family API (not separate current React UI) | GET `/api/related-persons/all-family-members`, `/{id}/family-members`, `/{id}/family-tree` | GUID where present | FamilyMemberResponseDTO[] |
| Existing family API mutation | POST `/{id}/family-members`, PUT/DELETE `/{id}/family-members/{familyMemberId}` under `/api/related-persons` | Create/UpdateFamilyMemberDTO | family response / message |
| Legal list/detail | GET `/api/legal-entities`, `/{id}` | list page,pageSize,search; GUID | paged service result / LegalEntityDTO |
| Legal create/update | POST `/api/legal-entities`; PUT `/{id}` | Create/UpdateLegalEntityDTO | LegalEntityDTO |
| Legal deactivate/verify | DELETE `/api/legal-entities/{id}`; POST `/api/verification/legal-person/{id}` | GUID | message / updated LegalEntityDTO |
| Legal import/export | POST `/api/legal-entities/import`; GET `/export` | multipart `file` / none | imported,failed,errors[] / XLSX |
| Limit entity search | GET `/api/legal-entities/limits/search` | search query | narrow entity lookup rows |
| Limit form data | GET `/api/legal-entities/{id}/limit-form-data` | entity GUID | LegalEntityLimitFormDataDTO (server-derived capital/limit values) |
| Limits list/detail | GET `/api/limiti`, `/{id}` | optional legalEntityId,matbroj; integer ID | LimitResponseDTO[] / DTO |
| Limits create/update/delete | POST `/api/limiti`; PUT/DELETE `/{id}` | Create/UpdateLimitDTO | LimitResponseDTO / delete message |
| Limits export | GET `/api/limiti/export` | same entity filters | XLSX |
| Capital read/upsert | GET/PUT `/api/capital` | PUT UpdateCapitalDTO: regulatoryCapital,coreCapital,supplementaryCapital,capitalDate | CapitalDTO or null on initial read |
| Capital export | GET `/api/capital/export` | none | XLSX |
| Daily/monthly archive list | GET `/api/reports/daily`, `/monthly` | page,pageSize (old UI 1,100) | items,total,page,pageSize |
| Daily generation | POST `/api/reports/daily` | no payload required (old UI {}) | created ReportResponseDTO, 201; snapshot persisted |
| Monthly generation | POST `/api/reports/monthly/{year}/{month}` | route year/month | created DTO, 201; snapshot persisted |
| Report downloads | GET `/api/reports/export/generated/{id}`, `/export/client/{identifier}`, `/export/all-clients` | report GUID / client identifier | XLSX attachments, backend filenames/content-types |
| Period state | GET `/api/period-lock/current`, `/status` | optional department; status supports period query | actual current/status object, not report DTO |
| Period requests | GET `/api/period-lock/unlock-requests` | status,page,pageSize | requests,total,page,pageSize |
| Period lock/unlock | POST `/api/period-lock/lock`, `/unlock` | optional year,month,department | message, audit/email side effects remain backend-owned |
| Unlock request | POST `/api/period-lock/request-unlock` | reason,year,month (department if supported) | message |
| Reject / request information | POST `/api/period-lock/unlock-requests/{id}/reject`, `/request-info` | note | message |
| Respond | POST `/api/period-lock/unlock-requests/{id}/respond` | message | message |
| Notifications | GET `/api/email-log` | optional audience; Accept-Language | provider,emails[{id,to,subject,htmlBody,sentAt,audience,purpose,deliveryStatus,requestId}] |
| Users list | GET `/api/users` | page,pageSize,search,role | GetUsersResponseDTO |
| Assignable roles | GET `/api/roles` | none | GetRolesResponseDTO; application DB roles, not arbitrary realm roles |
| User create | POST `/api/users` | username,firstName,lastName,email,roleIds,isActive | UserDTO 201 |
| Replace roles | PUT `/api/users/{id}/role` (POST `/{id}/roles` alias) | roleIds[] | message,roles[]; empty selection preserves existing backend removal/Guest semantics |
| User state/delete | DELETE `/api/users/{id}`; POST `/{id}/reactivate`; DELETE `/{id}/permanent` | identifier | message; backend Keycloak administration remains backend-only |
| Code-list categories | GET `/api/code-lists` | none | categories from current service |
| Category definition create/delete | POST `/api/code-lists/categories`; DELETE `/categories/{name}` | name,description / encoded name | created id,name,description / message |
| Category values/detail | GET `/api/code-lists/{category}`; GET `/id/{id}` | encoded category / integer ID | category,items[{id,category,code,value,description,displayOrder,isActive,modifiedBy,modifiedAt}] / CodeListResponseDTO |
| Create full value | POST `/api/code-lists` | kategorija,kod,naziv,opis,redoslijedPrikaza | CodeListResponseDTO 201 |
| Simple value alias | POST `/api/code-lists/{category}` | value | id,value,isActive,modifiedBy,modifiedAt 201 |
| Update/delete value | PUT/DELETE `/api/code-lists/{id}` | naziv,opis,redoslijedPrikaza,aktivan for PUT | updated value / message; referenced values cannot be removed |
| Code-list import preview/commit | POST `/api/code-lists/{category}/import?dryRun=true/false` | multipart `file` | totalRows,toImport[],duplicates[],errors[],imported |
| Business dropdowns | GET `/api/code-lists/dropdown/{category}` | category OsnovPovezanosti / OsnovPosebnogOdnosa / VrstaLimita | CodeListDropdownDTO[] (kod,naziv,opis etc.); distinct from localization resources |
| Audit | GET `/api/audit-logs` | page,pageSize≤200,tableName,action,username,search,dateFrom,dateTo | logs,total,page,pageSize; real actionDisplay/areaDisplay/changeSummary/username/IP |

Preserve Accept-Language, cancellation, authentication/token propagation,
correlation, bounded timeouts and backend attachment metadata. Do not retry
mutating imports/generation automatically. Credentials/cookies from old browser
fetch are not a substitute for the new server-side RBI user-token integration.

## Form validation and conditional behavior

| Rule / source | Classification | Native migration behavior |
|---|---|---|
| Physical names 2–100, letters/spaces/apostrophe/hyphen; passport 5–20 alphanumeric/hyphen | BACKEND_ALREADY_ENFORCES: RelatedPersonValidator; ResourcePage mirrors | immediate field feedback, server remains authority |
| Resident JMBG required, 13 digits/date/check digit | BACKEND_ALREADY_ENFORCES: JMBGValidator | mirror UX, do not accept arbitrary numeric string |
| Main nonresident passport AND 1–10 digit FBA ID; supplied JMBG valid | BACKEND_ALREADY_ENFORCES: ValidateMainNonResident | residency change clears incompatible identity inputs |
| Duplicate active JMBG/passport/FBA ID; exclude edited ID | BACKEND_ALREADY_ENFORCES: service + indexes + check-duplicate | blur feedback, block progression/save, cancel stale lookup |
| Physical relation basis/description/special basis, dateFrom/dateTo required; to≥from | BACKEND_ALREADY_ENFORCES: RelatedPersonValidator | preserve two-step form/error placement |
| Non-family must be identified staff; immediate family must select linked person | BACKEND_ALREADY_ENFORCES | preserve UZA_PORODICA conditional lookup |
| Family/declaration and relationship consistency | BACKEND_ALREADY_ENFORCES: service/validator | preserve backend rejection and refresh tree |
| Auto-description from basis; family-specific boolean defaults/locks; reset on basis switch | FRONTEND_ONLY form orchestration | preserve user-facing dependency behavior, not backend business persistence |
| Explicit yes/no selection vs default false; two-step progression | FRONTEND_ONLY form UX | nullable input until selected; do not silently default missing choices |
| Legal resident tax ID, checksum; nonresident numeric FBA; required business fields/date order | BACKEND_ALREADY_ENFORCES: LegalEntityService/TaxNumberValidator | preserve resident/nonresident UX and lookup-driven description |
| Legal field max lengths and numeric-only registration input | FRONTEND_ONLY where not enforced by the current service | preserve metadata feedback; DB/API still reject invalid persisted data |
| Limit entity/type existence, nonnegative amounts | BACKEND_ALREADY_ENFORCES: LimitService | async entity picker, dynamic VrstaLimita, server-derived calculations |
| Capital three amounts/date and numeric UX | frontend checks; backend UpdateCapitalDTO/service must still authorize and validate | explicit numeric/date form, no client capital/limit recalculation |
| Monthly report year/month and client identifier | BACKEND_ALREADY_ENFORCES: ReportController | old UI year range differs (2020..next year); use actual accepted API bounds, document if visible change |
| User uniqueness, allowed email domain, functional role IDs | BACKEND_ALREADY_ENFORCES: user service/controller | mirror @raiffeisengroup.ba and initial role selection UX; no realm-role editor |
| Unlock reason/note lengths and action/status transitions | backend period controller/service + frontend 10–500 reason UX | preserve confirmation/validation; do not infer permission from reporting read |
| File nonempty, ≤10MB, .xlsx; schema/header/order/row checks | BACKEND_ALREADY_ENFORCES by import controllers/services; frontend preflight | no browser Excel parser; present safe structured row errors |
| Code-list names/duplicates/references/import preview | BACKEND_ALREADY_ENFORCES | dry-run then explicit commit, cancel and partial-result feedback |

Fallback code-list options/descriptions in `resources.ts` are intentional
resilience for the existing OsnovPovezanosti/OsnovPosebnogOdnosa UX, not a license
to replace live dropdowns with hardcoded data. Limit types remain dynamic.
Raw exceptions/SQL/stack traces in ProblemDetails must not be displayed.

PeriodLockMiddleware currently gates POST/PUT/DELETE for related-persons,
legal-entities and limiti, not capital/reports. Preserve its 423 rejection; do
not silently extend its scope or treat read/download as a mutation grant.

## Business localization inventory

Business keys are in `the retired React reference tree/src/localization/source/{bs,en}/registry.json`
plus registry component inline BS/EN strings (forms, confirmations, failures,
help, guide, statuses, imports, currency/date display). Supporting validation
catalogs are also referenced. Product minimum is BS/EN with BS default.
The third inherited DE catalog passing validation is not product-language parity.

Move only app-owned business messages into RBI.Localization JSON with semantic
lowercase keys. Keep dynamic backend dropdown data and backend audit/email
localization via Accept-Language. No i18next provider/framework or demo strings
are needed in Blazor. Do not put business reference data into
RBI.Localization.Sifrarnik merely because both are called šifrarnici.

## Deployment / data safety boundary

Keep current React/IIS and backend/OCP workflows until the replacement publish,
startup, accessibility, responsive and full E2E parity gates are proven. The new
frontend is a separate server-hosted Blazor app under `Frontend`; API URL
is centrally configured. It must not reference EF/backend implementation or
contain DB/SMTP/Keycloak-admin credentials. No production migration, deletion,
reseed, realm-role reassignment or frontend cutover is authorized by inventory.

Current .env is ignored and was not read for this inventory. Integrated
Keycloak/OCP/SQL verification requires separately supplied approved environment
configuration; use isolated test identities/stores only inside tests, never an
application fake production user.
