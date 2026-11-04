# Final backend/frontend contract

Source baseline: `feat/codex-backend-finalization` at `82760bb513f6fa2efe2f9b970bb612b79d509e77` (2026-11-03). This document describes the existing backend, not proposed routes. Later handoff commits are documentation-only. Read this instead of speculative shapes in earlier frontend requests; the v1 handoff remains historical implementation evidence.

The endpoint and schema catalog below was cross-checked against the actual controllers, minimal authentication endpoints, DTOs, policies and services, and the running published backend's OpenAPI. OpenAPI alone is insufficient: anonymous response shapes, service validation, authorization and real status overrides are specified here. No frontend code or production data was changed.

## Integration status

| Area | Status | Boundary |
| --- | --- | --- |
| DB permissions, users, role catalog/requests, inbox/outbox, four-eyes | IMPLEMENTED | Backend HTTP/SQL tests; frontend adoption still required. |
| Eight-hour backend cookie/session ceiling and CSRF POST logout | PARTIAL | Backend implemented; actual realm/token/refresh and frontend acceptance external. Legacy GET logout remains deprecated. |
| Legal/physical registry, large-exposure metadata, workbook v2, limits, capital history | IMPLEMENTED | Existing business rules preserved. GCC becomes required when a real source is configured. |
| Bank/GCC and monthly finance adapters | EXTERNAL DEPENDENCY | Adapters and failure paths implemented; real access/mappings/sample reconciliation not verified. Daily bank finance mapping is not implemented. |
| Runtime BS/EN/DE resources, localized validation and business failures | IMPLEMENTED | Two application codebooks and scoped language resolution. Presentation-only legacy labels noted below remain PARTIAL. |
| Audit/module/person-type/email-preview presentation labels | PARTIAL | Audit/email-preview currently select EN versus BS; modules/person-type labels contain fixed BS display values. Do not assume these scalar display labels have DE coverage. No new localization refactor is authorized by this handoff. |
| Release-notes GitHub API | MISSING | No backend release-notes controller, DTO, GitHub reader, credential/configuration contract or environment-tag mapping exists. Coordinate a separate approved contract; do not call an invented route. |
| Full solution/native browser integration | PARTIAL | Backend gates pass; this checkout retains three BrowserHost compile failures. Claude owns the already-existing frontend assembly-name fix. |

## Shared wire and security rules

- JSON uses ASP.NET web camel-case property names. GUIDs are UUID strings; int32/int64 are JSON numbers; decimal is a JSON number (preserve financial precision); DateTime is an ISO date/time string. Use `yyyy-MM-dd` for date query values. Nullable fields may be JSON null. Nullable DTO fields can still be conditionally required by service rules below.
- Enums are numeric in JSON, not display labels: ResidencyType Resident=1, NonResident=2; RelatedPersonStatus Draft=1, Submitted=2, Verified=3, Rejected=4, Archived=5, Approved=6. Never submit a translated status, identity, audit field or resolved GCC name.
- Tables specify required/non-null at model-binding level, not every service precondition. Omitted value types bind their CLR default unless explicitly JSON-required; a nullable field is not a promise that business validation accepts null. Unknown JSON members are not a grant mechanism. Do not infer strict unknown-member rejection from OpenAPI additionalProperties metadata.
- Keycloak authenticates; normalized application database roles/permissions authorize. Provider roles/permission/appUserId markers are stripped and recomputed. Unknown authenticated identities receive the approved virtual `gost` baseline without creating an AppUser. Known inactive mappings receive no baseline. Explicit privileges require an active confirmed subject mapping and active DB assignments/permissions. This supersedes the older known-user-only Guest proposal.
- `sub` is opaque, ordinal/case-sensitive and server-derived; never trim/case-fold it or use email/local row ID as proof of ownership. Mutations require a valid subject (at most100 characters, no padded identity). Controllers with RequireActorEmail also require the authenticated email even on their read routes: LegalEntity, RelatedPerson, Limit, Capital, Report, Users, CodeList and PeriodLock. Missing required actor identity returns401. Email/name changes cannot transfer a confirmed account's grants.
- Policies below are cumulative: controller read policy plus action policy. `admin.only` is the existing ADMINISTRATION_MANAGE permission at HTTP level, not a new Keycloak role. Role-request decisions and role-catalog mutations additionally recheck a current named active application Admin inside the serialized transaction. Do not infer equivalent extra role checks for every administration endpoint.
- Physical/legal/limit/capital/verification POST, PUT and DELETE are blocked with423 while the current period is locked. Reads remain available. Reports generation, user administration, role requests/catalog, codebooks/localization, inbox read-state and period administration are not blocked by that middleware. Capital processing permissions remain unchanged.
- Unauthenticated/expired API access401; insufficient application authority403; missing resource/cross-owner resource404; validation400; stale/conflicting operation409; period lock423; unavailable external source 503; unexpected failure500 with safe detail. Responses can be empty on authorization/not-found paths. Check HTTP status before deserializing success.
- There is no general optimistic-concurrency token or idempotency header on all CRUD routes. Only the specific request/version/fingerprint mechanisms below promise it. Retrying a report or capital generation can create another persisted record.

### Policy map

| Policy prefix | Read | Create / edit / delete or other authority |
| --- | --- | --- |
| physical-persons | FL_VIEW | FL_CREATE / FL_EDIT / FL_DELETE; verification uses FL_EDIT |
| legal-persons | PL_VIEW | PL_CREATE / PL_EDIT / PL_DELETE; verification uses PL_EDIT |
| limits | LIMITI_VIEW | LIMITI_CREATE / LIMITI_EDIT / LIMITI_DELETE |
| capital | KAPITAL_VIEW | KAPITAL_CREATE / KAPITAL_EDIT / KAPITAL_DELETE; capital.write means any of those three |
| reports | authenticated effective gost | reports.generate = KAPITAL_CREATE (default Capital/Admin); generation persists |
| code-lists | CODE_LISTS_VIEW | administrative mutations ADMINISTRATION_MANAGE |
| administration | ADMINISTRATION_VIEW | ADMINISTRATION_MANAGE; role service transaction restrictions also apply |
| application.access / bare Authorize | authenticated effective gost | No extra mutation privilege implied |

Sources: [policies](../Backend/Helpers/Constants/ApplicationPolicies.cs), [permission registration](../Backend/IoC/Extensions/Authentication/AuthenticationServiceExtensions.cs), [claim transformation](../Backend/IoC/Extensions/Authentication/RoleClaimsTransformation.cs), [period middleware](../Backend/Middlewares/PeriodLockMiddleware.cs), [actor filter](../Backend/Helpers/RequireActorEmailAttribute.cs).

### Errors and localization

Do not collapse the existing formats into a single assumed DTO:

```json
{"status":409,"title":"Request cannot be completed.","detail":"Safe localized message","instance":"POST /api/...","traceId":"opaque","code":"request.concurrent_change","resourceKey":"errors.request.concurrent.change"}
```

```json
{"status":400,"title":"Validation failed","errors":{"isResident":["Select residency."]},"traceId":"opaque"}
```

```json
{"errors":[{"field":"message","message":"Safe localized validation message"}],"code":"request.concurrent_change"}
```

Examples illustrate shapes, not a promise of those exact localized strings or all extensions on all paths. Model binding uses ValidationProblemDetails with a dictionary; period423/command and some registry/user paths use an errors array. Result-based controllers use ProblemDetails with X-Correlation-ID/timestamp. Upload paths may return `{error:string}` or `{message:string}`. User self-role/self-delete denials use400 ProblemDetails; self-deactivate uses400 errors-array; last-Admin protection409. Global business exceptions add code and, when catalog resolution runs, resourceKey=`errors.` + code with underscores replaced by dots. UI must support all formats, prefer safe field/code keys and never display raw internal exception/storage payloads.

Send `Accept-Language: BS`, `EN` or `DE` on each API request. Resolution uses the first bounded language token (regional suffix stripped), configured default (BS), then stable key; scoped fallback messages remain safe if catalog access fails. Runtime labels are authoritative; invalid format overrides do not break requests. No process-global user culture is changed. Correlation header is `X-Correlation-ID`; do not send personal data as its value.

Sources: [model errors](../Backend/IoC/Extensions/Controllers/ControllersExtensions.cs), [global errors](../Backend/Exceptions/GlobalExceptionHandler.cs), [result errors](../Backend/API/Controllers/BaseController/BaseResuItController.cs), [runtime catalog](../Backend/BL/Services/RuntimeLocalizationCatalog.cs).

## Behavioral contracts and frontend actions

### Authentication, profile and session

GET `/authentication/login?returnUrl=/app` challenges Keycloak OIDC (code+PKCE), with redirect flow, not JSON200; unsafe external return URLs fall back to `/app`. GET `/api/auth/profile` returns `{userId:string|null,fullName:string|null,username:string|null,email:string|null,applicationUserId:string|null,roles:string[],permissions:string[]}`. userId is the opaque provider subject; applicationUserId is the optional application GUID. There is no isAdministrator property: use effective permission and current application roles, not incoming Keycloak role strings.

The backend cookie has an absolute8h cap and no sliding renewal. Saved OIDC access-token expiry can require earlier login; the backend does not promise8h of refresh regardless of realm policy. An expired bearer can include `Token-Expired: true`. Outside Development, enabled auth, approved issuer/client/secret and nonempty `KeycloakSettings__Audience` are mandatory; configured Audience is always validated. Do not reuse OIDC client audience blindly as API audience. Real realm mapping/redirect/logout/refresh remain external.

GET `/authentication/antiforgery` (authenticated) returns `{requestToken:string,headerName:string}` and sets the HttpOnly antiforgery cookie; no-store. Send the returned header/token and cookie to POST `/authentication/logout`; valid calls sign out cookie and OIDC and redirect `/`; invalid CSRF400; unauthorized401. Deprecated GET logout returns sign-out redirect with `Deprecation:true` and remains a compatibility risk until coordinated retirement. Frontend has its own RBI server-side auth/logout endpoint: do not mix token/cookie origins or assume its token can satisfy backend CSRF. Frontend must coordinate its own8h cap, server token cache and real realm behavior.

Sources: [auth endpoints](../Backend/IoC/Extensions/Authentication/AuthenticationEndpoints.cs), [auth options](../Backend/IoC/Extensions/Authentication/AuthenticationServiceExtensions.cs), [frontend auth endpoint](../Frontend/Platform/RBI.Authentication/IoC/AuthEndpoints.cs).

### Users, role catalog and role requests

Users list envelope is `{users,total,page,pageSize}`, not items; filter search and role; max pageSize200, lastName/firstName/id order. UserDTO.id is the application GUID, not provider sub (the legacy C# KeycloakId() method is not a JSON property). `identityConfirmed` distinguishes stored role choices from effective access. Create/edit profiles allow valid emails outside the bank domain; username/first/last max 100, email max 200, normalized duplicate protection. PUT user profile is administrative editing of `{username,firstName,lastName,email}`, not a new self-profile endpoint. No client-supplied Keycloak subject binding.

Roles replace through POST `/{userId}/roles` or compatibility PUT `/{userId}/role`, body `{roleIds:[guid]}` (legacy singular roleId accepted); empty roles remove explicit roles subject to final-Admin protection. DELETE `/{userId}` deactivates; POST reactivate restores; DELETE permanent removes. No self-role replacement/deactivation/deletion; no loss of last active confirmed Admin. Successful mutations return `{message}` or `{message,roles}`; no repeated-success/idempotency promise for these endpoints. Allowed state/audit changes are transactional under SQL application-lock serialization. Other ordinary profiles preserve existing rules.

Role definitions remain normalized, not generic CodeList copies: GET `/api/roles` returns `{roles:[{id,name,userCount}]}`. Administrative `/api/role-catalog` returns definitions with displayName/description/isActive/version/permissionIds/isProtected and includes inactive definitions. Technical names immutable; ASCII letters/digits/underscore/hyphen/dot max 100; displayName required100; description500; max 200 distinct active permission IDs. Protected Admin/Guest409; own assigned-role edit403; duplicate409; stale expectedVersion409. Existing technical roles are invariant identifiers, not translated values. Use `/api/role-requests/options` for requestable active choices (not administrative `/api/roles`); `{items:[{id,name,displayName}]}`.

Role-request submission example (example IDs are placeholders, not seeded production values):

```json
{"clientRequestId":"11111111-1111-1111-1111-111111111111","purpose":"new","roleIds":["22222222-2222-2222-2222-222222222222"],"note":"Application access requested."}
```

`purpose` is `new` or `change`, not Request/Change or type. new requires1–20 distinct active non-Guest role IDs and unions roles on approval; change replaces and can remove all explicit roles, preserving Guest/final-Admin. Note max 1000. Trusted valid subject and verified email required; no requester fields accepted as ownership. One pending request per subject. Matching clientRequestId/content replay200; new 201; incompatible replay409. Mine/review list `{items,total,page,pageSize}`, max 200, status filter pending/approved/rejected, createdAt desc/id order. Mine excludes requester display values (nullable); administrative review includes requesterUsername/requesterEmail. No separate detail route/currentRoles property. Missing active confirmed Admin approver503; missing verified email403.

Approve/reject body `{decisionId:guid,expectedVersion:int>=1,note?:string<=1000}`; response RoleRequestDTO,200. Admin cannot decide own request403. Same decisionId/actor/outcome/note replay returns prior decision without duplicate grants/audit/inbox/outbox, even if old expectedVersion is replayed. Changed/stale/incompatible decisions409. Approval identity conflict409 requires reconciliation, not a guessed email-to-sub takeover. State, provisioning, grants, audit and notices share one transaction. Refresh profile and invalidate cached permissions after successful changes; never treat email delivery as proof of a grant.

Sources: [users](../Backend/API/Controllers/UsersController.cs), [user service](../Backend/BL/Services/AppUserService.cs), [mutations](../Backend/BL/Services/AppUserService.Mutations.cs), [requests](../Backend/BL/Services/RoleRequestService.cs), [catalog](../Backend/BL/Services/RoleCatalogService.cs), [SQL boundary](../Backend/BL/Services/UsersMutationBoundary.cs).

### Inbox and delivery

GET `/api/notifications?page=1&pageSize=20&unreadOnly=false` returns `{items:[{id,titleKey,bodyKey,parameters:object,route:string,createdAt,readAt:null}],total,page,pageSize,unreadCount}`. Owner is current exact sub; pageSize1–200, newest first thenid. POST `/{id}/read` returns204; repeated read idempotent; cross-owner/missing 404. There are **no** `/unread-count` or `/read-all` endpoints, no type or link fields; use unreadCount and route from list. Empty list example: `{"items":[],"total":0,"page":1,"pageSize":20,"unreadCount":0}`. Render localized keys with parameters as text, authenticated relative app route; never raw mail HTML.

`/api/email-log` is not the production inbox. It returns `{provider,emails}`; raw previews exist only in Development/demo and match the caller email. Its existing scalar entry shape is `{id,to,subject,htmlBody,sentAt,audience,purpose,deliveryStatus,requestId}`. Do not rely on previews to discover all users' notices.

Business operations persist inbox/outbox atomically. Worker polls30s; dispatcher leases2min, max 50/batch, max 5 attempts with backoff. Delivery failures do not replay grants; provider/network crashes can duplicate mail (not exactly-once). Parameters bounded32000 and HTML-encoded for email. Exhausted delivery requires operational attention; no public retry/control endpoint is implemented.

Sources: [inbox](../Backend/API/Controllers/NotificationsController.cs), [mail preview](../Backend/API/Controllers/EmailLogController.cs), [delivery](../Backend/BL/Services/NotificationDispatcher.cs), [worker](../Backend/Services/NotificationDeliveryWorker.cs).

### Registry, verification and lookups

Legal CRUD uses Create/UpdateLegalEntityDTO; explicitly send isResident true/false. For residents taxNumber exact13/checksum remains; nonresidents require numeric fbaId1–10. Required name<=100, basisOfConnection<=100, connectionDescription<=500, dateFrom; dateTo optional >=dateFrom. Optional matbroj/maticniBroj numeric<=50; connectedWithBank must be nonnull. The controller summary says identifiers cannot change, but the current Update implementation validates and assigns replacement residency/tax/FBA values; do not promise server-side identifier immutability from that comment. This handoff does not change that established behavior. Active configured basis choices must be used. Missing legacy catalogs retain rollout compatibility; empty/inactive configured catalog does not authorize arbitrary values. Tax maximum13 vs exact13 remains a business clarification, not a new rule.

Legal lists `{items,total,page,pageSize}`, search name/tax/FBA, case-insensitive, page1–1000000,size1–200, createdAt desc/id. Limits lookup `/limits/search` remains a20-item array; `/limits/lookup` is paged max 100 name/id. `/{id}/limit-form-data` returns registry fields and nullable today's capital.

`gccNumber` nullable numeric string<=50; gccName is server-resolved read-only<=200. New/unlinked records require GCC when the bank adapter is configured. Omitted/null GCC on old updates preserves existing value; explicit blank attempts clear and is rejected in configured required mode. isLargeExposure nullable boolean: omitted preserves on update, new creates default false, historical null stays unknown. No invented threshold/calculation. `/api/bank-reference/customers` items `{taxNumber,matbroj,name}` and `/gcc` items `{number,name}`, each paged search,max 100. No `/api/lookups/gcc/{number}` route. Unconfigured/unavailable source 503, unknown GCC400, ambiguous source409; no fake fallback.

Physical create/update uses numeric residency1/2, names required100, JMBG<=13/checksum for resident; main nonresident requires both passport (service5–20 alphanumeric/hyphen, despite DTO max 50) and numeric FBA ID (1–10). Legacy family-member identification accepts passport or FBA; any supplied JMBG must validate. Names must be 2–100 Unicode-letter-led characters (letters/spaces/apostrophe/hyphen). RelationBasis/SpecialRelationBasis required max 500, description required1000, both dateFrom/dateTo required by service and end>=start; immediate-family basis requires relatedToPersonId, otherwise isIdentifiedStaff=true; booleans default false; optional relatedToPersonId; immutable server creator/status. Duplicate check accepts jmbg,passportNumber,fbaId,excludeId and returns `{exists,field,message}`. Preserve family parent/cycle/declaration restrictions; family endpoints return separate FamilyMemberResponseDTO (children populated on tree). New related-person linkage is not interchangeable with the legacy family-member row ID. Full array routes remain; page route returns summary DTO with search first/last/JMBG,max 100. Server response labels may be display-only; enums are canonical.

POST `/api/verification/physical-person/{id}` and `/legal-person/{id}` have no body, return full DTO200; edit permission plus unlocked period, different authenticated creator. Self403 code `verification.self_denied` (not verification.same-user); historical creator unknown 409 `verification.provenance_required`; concurrency409 `request.concurrent_change`; missing 404. Creator is immutable server subject; changed email and forged DTO cannot bypass. Ordinary legal Update.status may repeat state, cannot transition; physical updates do not bind status. Successful state/audit commits atomically; failures produce no success audit.

Sources: [legal](../Backend/BL/Services/LegalEntityService.cs), [physical](../Backend/BL/Services/RelatedPersonService.cs), [physical validation](../Backend/Helpers/Validators/RelatedPersonValidator.cs), [verification](../Backend/Helpers/RegistryVerification.cs), [bank source](../Backend/BL/Services/BankReferenceSource.cs). See actual source catalog below if a validation helper is reached through a partial service file.

### Workbook transport and limits

Use multipart/form-data with field **file**; server-side RBI transport must propagate session token without exposing it to browser scripts. Export response is raw XLSX (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`), not JSON/base64. Read Content-Disposition where supplied. Never update snapshots to accommodate format differences.

Legal v2 export/import19 ordered columns; legacy10 accepted. Physical v2 25; legacy15 accepted. Limits v2 18; legacy14 accepted. Exact ordered headers and aliases are in controller exports and `ExcelImportSchema`/service column definitions; do not rename headers based on UI language. Date/boolean/header protocol stays canonical. v2 metadata/status/creator/name is read-only on import; server identity/audit remain authoritative. Physical parent references use identity keys and safe ordering; row cycles/missing/ambiguous links rejected.

Registry/limit import response `{"imported":2,"failed":1,"errors":["Safe row feedback"],"details":[{"row":4,"field":"name","code":"validation...","message":"Safe row feedback"}]}`. `errors` remains string[], not Claude's proposed object[]. Structured diagnostics are additive **details**. Valid rows commit together with audit; row validation failures can coexist with HTTP200; storage/audit failure commits none. Preview is not supported on these three routes. Upload<=10MiB,5000data rows, ZIP<=2000entries/32MiBentry/64MiBexpanded; reject formulas/macros/external links. Legal/physical export>5000 rejected, not silently truncated.

Limits body `{legalEntityId:guid,tipLimita:string,odobreniLimit:decimal|null,maksimalnaOcekivanaUtilizacija:decimal|null,rokOcekivaneUtilizacije:datetime|null,komentar:string|null}`. Party must exist/active; tipLimita active VrstaLimita code; nullable nonnegative amounts<=9999999999999999.99 with<=2decimal places (no rounding), tipLimita<=100 and komentar<=1000. IDs are int32. Get array optionallegalEntityId/matbroj/asOf; page optionallegalEntityId/search/asOf, max 100 and stable sort. Export shares filters. Capital values are nullable as-of observations, not 0. Import fingerprint prevents duplicate batch records; generated-ID audit and commit-ack recovery are tested. Ordinary POST is not a general idempotent upsert.

Codebook workbook is intentionally one label column (Vrijednost/value/naziv/name headers), not a full database backup or full localization-pack roundtrip. `dryRun=true` default returns `{totalRows,toImport:string[],duplicates:string[],errors:string[],imported:0}`. false commits valid new labels atomically/audited; duplicate codes skipped with errors. Never interpret200 as all values imported. Header/data formulas rejected before cell value evaluation. Errors currently can include legacy BS duplicate-row text even for another requested language; preserve compatibility and report localized UI fallback rather than assume all legacy scalar text is a resource key.

Sources: [export columns](../Backend/API/Controllers/LegalEntityController.cs), [physical columns](../Backend/API/Controllers/RelatedPersonController.cs), [limits](../Backend/BL/Services/LimitService.cs), [workbook schema](../Backend/Helpers/Excel/ExcelImportSchema.cs), [envelope](../Backend/Helpers/Excel/WorkbookEnvelope.cs), [codebook import](../Backend/API/Controllers/CodeListController.cs).

### Capital history and regulatory reporting

PUT `/api/capital` accepts four required business values `{regulatoryCapital:decimal,coreCapital:decimal,supplementaryCapital:decimal,capitalDate:datetime}` (wire DTO nullable, service requires all). Reject negative/>16integral-digit/>2decimal values without rounding. It appends an immutable revision and audit, returns CapitalDTO200; **no POST capital route**, no validTo/isActive fields on versions. GET current/as-of returns CapitalDTO200 or204 no body when unknown. As-of query date required400 if missing/invalid; latest effective date<=requested, highest revision wins within same date. History max 100, stable effective/revision order, source revision/legacy, revision0 for known legacy observations. Unknown history never inferred/backfilled from current. SQL/EF prevent updating/deleting new revisions.

Example current capital: `{"id":"11111111-1111-1111-1111-111111111111","regulatoryCapital":1000000.00,"coreCapital":800000.00,"supplementaryCapital":200000.00,"capitalDate":"2026-11-01T00:00:00"}` (illustrative values, not bank data).

Reports list daily/monthly items ReportDTO, max 200, reportDate/createdAt/id desc; daily POST no body, monthly POST year/month path (month1–12,year2000..current UTC year). Generate persists201 plus audit and requires KAPITAL_CREATE; read/download effective Guest. Period middleware deliberately permits generation while locked. No general idempotency key: do not blindly retry generation. Generated download uses saved snapshot; client/all-clients exports read current data. Snapshot metadata includes GCC/large-exposure for new rows; old JSON retained, XLSX layout not extended without bank mapping.

`dataSource`: local.clientlimits/legacy.unspecified → isAuthoritative=false; finance.monthlyhistory →true indicates origin, **not regulatory acceptance**. Optional SELECT-only monthly adapter uses exact approved YYYY-MM/table dictionary, customer key/BAM and documented components. Missing/null/duplicate/precision/unmapped period409 or outage503; no local fallback on a configured authoritative-source failure. Daily finance mapping, real spreadsheets and reconciled totals remain external. Do not label local synthetic snapshots as bank finance truth.

Sources: [capital](../Backend/BL/Services/CapitalHistory.cs), [capital service](../Backend/BL/Services/CapitalService.cs), [reports](../Backend/BL/Services/ReportService.cs), [finance adapter](../Backend/BL/Services/FinanceMonthlySource.cs).

### Period workflow

Current/status response `{year,month,monthDisplay,isLocked,lockedBy:null,lockedAt:null}`; status query year2020–9999/month1–12. Lock/unlock optional `{year?:int,month?:int}` defaultUTCcurrent, returns `{message}`. POST request-unlock requires reason trimmed>=10/raw<=500, optionalyear/month. All processing remains CapitalRead+CapitalWrite (any create/edit/delete grant), not Admin-only.

GET `/api/period-lock/unlock-requests` is **own requests only**; processor view must call `/unlock-requests/review`. Envelope `{requests,total,page,pageSize}`, max 200; optional status (emptyall); response fields id,requestedBy,year,month,monthDisplay,reason,status,adminNote,processedBy,processedAt,createdAt. Statuses uppercase PENDING/NEEDS_INFO/REJECTED; do not substitute role-request lowercase statuses. There is no approve endpoint; unlock is the existing separate command. Review list does not reveal immutable owner subject as an editable field.

Reject/request-info POST body `{note:string}` trimmed>=10/raw<=1000; respond body `{message:string}` trimmed>=10/raw<=490, only own NEEDS_INFO request. Owner404, wrong/repeated transition409. Original response prefix retained and final storage<=500; no truncation. Response preserves original+new PENDING child workflow. SQL rowversion protects concurrent transitions; state/audit/inbox/outbox oneSave; no-op lock does not duplicate success audit/broadcast. Historical ownerless requests require reconciliation, not inferred access. Scheduler's host-local08:00/weekdays and UTC current-period logic are not an approved production calendar/HA-overlap policy.

Sources: [controller and inline DTOs](../Backend/API/Controllers/PeriodLockController.cs), [workflow](../Backend/BL/Services/PeriodWorkflow.cs), [scheduler](../Backend/Services/AutoLockHostedService.cs), [repository](../Backend/BL/Services/UnlockRequestRepository.cs).

### Runtime resources and codebook metadata

GET languages `{items:[{code:"BS",name:"Bosanski"},...]}`; **no isDefault property** (configured defaultBS). GET `/api/localization/resources/EN` returns `{language:"EN",fallbackLanguage:"BS",version:"opaque",items:[{resourceKey,label,helper:null,placeholder:null}]}`, no-cache. It is not `/api/localization/{culture}` nor a flat map; frontend adapter must map labels/helpers/placeholders. Unsupported direct language404; request-header fallback is separate. PUT resource body `{label:string<=4000,helper?:string<=4000,placeholder?:string<=500,isActive:bool=true}` returns204, admin manage. New key lowercase semantic dots<=200; legacy casing consumed case-insensitively. System-required resource cannot deactivate409; bad setup503. Version invalidates cache; concurrent request languages isolated. No optimistic expectedVersion on resource PUT.

Languages/LanguagePacks are two definitions alongside ordinary application codebooks; names configurable and no DP_ prefix required. Logical model active/language/key/label/helper/placeholder. DataProducts ID1/12/13/18/19/20 is a reference only. Maintain through normal application administration; missing real business codebooks are not replaced by fabricated dropdown entries. PeriodRequestStatus/RecordState categories are **not seeded as a new backend promise**; wire states are invariants. Coordinate their UI choices separately.

GET `/api/code-lists` returns `{categories:string[]}`; `/categories/definitions` returns `{items:[{id,name,description,isActive,isSystemRequired,version,createdAt,createdBy,modifiedAt,modifiedBy}]}`. Use **modified** names, not lastModified. POST categories name2–100/description500→201; PUT categories/{name} `{description?:string,expectedVersion:int}`→200 definition metadata (no rename); stale 409. DELETE category/value soft-deletes under system/reference protection, not destructive schema changes.

GET category response `{category,items:[{id,category,code,value,description,displayOrder,isActive,modifiedBy,modifiedAt}]}` differs from GET id's CodeListResponseDTO (kategorija/kod/naziv/opis/aktivan and audit fields). Full POST/PUT DTO uses Bosnian property names; simple POST category uses `{value}`. Dropdown route is authenticated read and returns `[{kod,naziv,opis}]`, no id/value fields. Value mutation response `{id,value,isActive,modifiedBy,modifiedAt}`; delete `{message}`. Full value create requires kategorija<=100,kod<=50,naziv<=200; opis optional<=500. Name/code nonblank. Definition and value identity/code read-only on updates. Uniqueness preflight/SQL keys, referenced/system guards and atomic audit apply; do not expect DELETE to always succeed.

Sources: [runtime](../Backend/BL/Services/RuntimeLocalizationCatalog.cs), [codebook controller](../Backend/API/Controllers/CodeListController.cs), [codebook service](../Backend/BL/Services/CodeListService.cs), [dropdown](../Backend/API/Controllers/ReferenceDataController.cs).

### Audit, module availability and operations

GET `/api/audit-logs` returns `{logs,total,page,pageSize}`, filters tableName/action/username/search/dateFrom/dateTo inclusive calendar end, page>=1,max 200, timestampdescending (no client-sort option). Log `{id,tableName,recordId,action,actionDisplay,areaDisplay,changeSummary,oldValues:string|null,newValues:string|null,username,ipAddress,timestamp}`. Old/new are serialized JSON strings, not guaranteed nested objects; show text safely and parse conditionally. No audit write endpoint. New stable actor subject/username snapshots are server-owned; historical rows retained. Display summaries/labels do not establish identity or replace complete change evidence.

GET `/api/auth/modules` returns `{modules:[{key,name,route,icon,isEnabled}]}`. This legacy advisory list uses a fixed role-to-module mapping and BS names; it is **not** a complete permission-based navigation manifest for dynamically defined roles. Frontend should use profile permissions for authoritative action guards; backend always enforces. Do not infer a missing capital/admin route is unauthorized solely from this four-module list.

Health live/ready are public `{status:"Healthy"}`200 (Unhealthy503; Degraded200); SQL readiness503 for pending migrations, neverDDL. `/metrics` requires administration.read and is Prometheus text, not frontend JSON. Swagger/health are intentional operational endpoints, not business data. Production readiness requires reviewed migrations before traffic, real realm/SQL/mail/bank/finance validation, shared sessions/protected persisted DP keys and approved proxy/OCP/3scale/resource/drain/scheduler/backup policy. PKCS9.0.21 retained; no warning suppression or vulnerability-free claim from no-restore gates.

Sources: [audit](../Backend/API/Controllers/AuditLogController.cs), [modules](../Backend/API/Controllers/ModulesController.cs), [health](../Backend/IoC/Extensions/Health/HealthCheckEndpoints.cs), [metrics](../Backend/IoC/Extensions/HTTPMetricsExtension/HTTPMetricsExtension.cs), [configuration](configuration/ENVIRONMENT-VARIABLES.md).

## Illustrative payloads and workbook columns

Examples show wire shapes only. Replace IDs, active reference codes, subject-independent names and sample amounts with actual authorized application input. They are not production seed data or a promise that a nonexistent catalog/reference will validate.

Administrative profile update (PUT `/api/users/{userId}`):

```json
{"username":"example.user","firstName":"Example","lastName":"User","email":"example.user@example.test"}
```

Legal nonresident create (GCC omitted only during unconfigured rollout):

```json
{"isResident":false,"taxNumber":null,"fbaId":"1234567890","name":"Example Entity","maticniBroj":null,"matbroj":null,"gccNumber":null,"isLargeExposure":false,"basisOfConnection":"<active catalog code>","connectionDescription":"Documented basis description","connectedWithBank":true,"dateFrom":"2026-11-01T00:00:00","dateTo":null}
```

Limits create:

```json
{"legalEntityId":"11111111-1111-1111-1111-111111111111","tipLimita":"<active VrstaLimita code>","odobreniLimit":1000.00,"maksimalnaOcekivanaUtilizacija":500.00,"rokOcekivaneUtilizacije":"2026-12-01T00:00:00","komentar":null}
```

Role decision (POST approve/reject):

```json
{"decisionId":"33333333-3333-3333-3333-333333333333","expectedVersion":1,"note":"Reviewed application request."}
```

Runtime resource PUT and GET pack fragment:

```json
{"label":"Customer name","helper":"Enter the registered name.","placeholder":"Name","isActive":true}
```

```json
{"language":"EN","fallbackLanguage":"BS","version":"opaque-version","items":[{"resourceKey":"customers.form.name.label","label":"Customer name","helper":"Enter the registered name.","placeholder":"Name"}]}
```

Canonical current export columns (ordered, copied from controller source; accepted import aliases are in the service schema). Labels are compatibility protocol values, not culture-specific dropdown data:

| Workbook | Columns | Ordered headers |
| --- | --- | --- |
| LegalEntity | 19 | Naziv → Rezidentnost → Porezni broj → FBA ID → Matbroj → Osnov povezanosti → Opis povezanosti → Povezano lice s Bankom → Datum od → Datum do → Matični broj registracije → GCC broj → GCC naziv → Velika izloženost → Status → Kreirao → Kreirano → Izmijenio → Izmijenjeno |
| RelatedPerson | 25 | Ime → Prezime → Rezidentnost → JMBG → Broj pasoša → FBA ID → Osnov povezanosti → Osnov posebnog odnosa → Datum od → Datum do → Izjava bez članova porodice → Povezano lice s Bankom → Poseban odnos s Bankom → Poseban ugovor → Malus & Clawback → Opis povezanosti → Identifikovani zaposlenik → JMBG povezanog lica → Pasoš povezanog lica → FBA ID povezanog lica → Status → Kreirao → Kreirano → Izmijenio → Izmijenjeno |
| Limit | 18 | Naziv → Rezidentnost → FBA ID → Porezni broj → Matični broj → Matbroj → Tip limita → Odobreni limit → Maksimalna očekivana utilizacija → Rok očekivane utilizacije → Komentar → Regulatorni kapital → Osnovni kapital → Datum kapitala → Kreirao → Kreirano → Izmijenio → Izmijenjeno |
| Codebook | 1 | Vrijednost |

Legal legacy10, physical legacy15 and limits legacy14 remain accepted. Do not treat removed read-only suffix columns as editable provenance/status fields.

## Exact endpoint catalog

All listed business routes are IMPLEMENTED. Authentication redirects and anonymous response bodies below override incomplete Swagger response annotations. Query defaults/types are from the running API; actual clamp/service rules are above. Every entry also inherits common auth/error/localization/audit rules. No arbitrary sort parameter is supported unless explicitly listed. Binary response = XLSX stream. Types link to the complete wire-property catalog below.
### AuditLog

Source: [AuditLogController.cs](../Backend/API/Controllers/AuditLogController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/audit-logs` | page:integer(int32)=1<br>pageSize:integer(int32)=20<br>tableName:string optional<br>action:string optional<br>username:string optional<br>search:string optional<br>dateFrom:string(date-time) optional<br>dateTo:string(date-time) optional | 200 {logs:AuditRow[],total,page,pageSize} | admin.only (ADMINISTRATION_MANAGE) |

### BankReference

Source: [BankReferenceController.cs](../Backend/API/Controllers/BankReferenceController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/bank-reference/customers` | search:string optional<br>page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 BankCustomerReferencePage | legal-persons.read |
| GET `/api/bank-reference/gcc` | search:string optional<br>page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 BankGroupReferencePage | legal-persons.read |

### Capital

Source: [CapitalController.cs](../Backend/API/Controllers/CapitalController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/capital` | none | 200 CapitalDTO or204 empty | capital.read |
| PUT `/api/capital` | body UpdateCapitalDTO | 200 CapitalDTO | capital.edit |
| GET `/api/capital/as-of` | date:string(date-time) optional | 200 CapitalDTO or204 empty | capital.read |
| GET `/api/capital/history` | page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 CapitalHistoryDTOReferencePage | capital.read |
| GET `/api/capital/export` | none | 200 binary XLSX | capital.read |

### CodeList

Source: [CodeListController.cs](../Backend/API/Controllers/CodeListController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/code-lists` | none | 200 {categories:string[]} | code-lists.read |
| POST `/api/code-lists` | body CreateCodeListDTO | 201 CodeListResponseDTO | code-lists.read + administration.write |
| POST `/api/code-lists/categories` | body CreateCodeListDefinitionDTO | 201 {id,name,description} | code-lists.read + administration.write |
| GET `/api/code-lists/categories/definitions` | none | 200 {items:DefinitionMetadata[]} | code-lists.read |
| PUT `/api/code-lists/categories/{name}` | body UpdateCodeListDefinitionDTO | 200 {id,name,description,version,modifiedAt,modifiedBy} | code-lists.read + administration.write |
| DELETE `/api/code-lists/categories/{name}` | none | 200 {message} | code-lists.read + administration.write |
| GET `/api/code-lists/{kategorija}` | none | 200 {category,items:AdminValue[]} | code-lists.read |
| POST `/api/code-lists/{kategorija}` | body SimpleValueRequest | 201 {id,value,isActive,modifiedBy,modifiedAt} | code-lists.read + administration.write |
| GET `/api/code-lists/id/{id}` | none | 200 CodeListResponseDTO | code-lists.read |
| PUT `/api/code-lists/{id}` | body UpdateCodeListDTO | 200 {id,value,isActive,modifiedBy,modifiedAt} | code-lists.read + administration.write |
| DELETE `/api/code-lists/{id}` | none | 200 {message} | code-lists.read + administration.write |
| GET `/api/code-lists/{kategorija}/export` | none | 200 binary XLSX | code-lists.read |
| POST `/api/code-lists/{kategorija}/import` | dryRun:boolean=True<br>multipart file:binary | 200 ImportPreviewDto | code-lists.read + administration.write |

### DATA_CORE

Source: [AuthenticationEndpoints.cs](../Backend/IoC/Extensions/Authentication/AuthenticationEndpoints.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/authentication/login` | returnUrl:string optional | 302 OIDC challenge (not JSON200) | public challenge/legacy sign-out |
| GET `/authentication/logout` | none | 302 sign-out;Deprecation:true | public challenge/legacy sign-out |
| POST `/authentication/logout` | none | 302 sign-out;400 invalid CSRF;401 | authenticated |
| GET `/authentication/antiforgery` | none | 200 {requestToken,headerName}+cookie | authenticated |
| GET `/api/auth/profile` | none | 200 Profile (subject/application ID/roles/permissions) | authenticated effective gost |

### EmailLog

Source: [EmailLogController.cs](../Backend/API/Controllers/EmailLogController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/email-log` | audience:string optional | 200 {provider,emails:PreviewRow[]} | capital.read |

### LegalEntity

Source: [LegalEntityController.cs](../Backend/API/Controllers/LegalEntityController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/legal-entities` | page:integer(int32)=1<br>pageSize:integer(int32)=20<br>search:string optional | 200 LegalEntityListDTO | legal-persons.read |
| POST `/api/legal-entities` | body CreateLegalEntityDTO | 201 LegalEntityDTO | legal-persons.create |
| GET `/api/legal-entities/export` | none | 200 binary XLSX | legal-persons.read |
| GET `/api/legal-entities/{id}` | none | 200 LegalEntityDTO | legal-persons.read |
| PUT `/api/legal-entities/{id}` | body UpdateLegalEntityDTO | 200 LegalEntityDTO | legal-persons.edit |
| DELETE `/api/legal-entities/{id}` | none | 200 {message} | legal-persons.delete |
| GET `/api/legal-entities/limits/search` | search:string optional | 200 LegalEntityLookupDTO[] | limits.read |
| GET `/api/legal-entities/limits/lookup` | search:string optional<br>page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 object | limits.read |
| GET `/api/legal-entities/{id}/limit-form-data` | none | 200 LegalEntityLimitFormDataDTO | limits.read |
| POST `/api/legal-entities/import` | multipart file:binary | 200 ImportResultDTO | legal-persons.create |

### Limit

Source: [LimitController.cs](../Backend/API/Controllers/LimitController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/limiti` | legalEntityId:string(uuid) optional<br>matbroj:string optional<br>asOf:string(date-time) optional | 200 LimitResponseDTO[] | limits.read |
| POST `/api/limiti` | body CreateLimitDTO | 201 LimitResponseDTO | limits.create |
| GET `/api/limiti/page` | legalEntityId:string(uuid) optional<br>search:string optional<br>page:integer(int32)=1<br>pageSize:integer(int32)=20<br>asOf:string(date-time) optional | 200 LimitResponseDTOReferencePage | limits.read |
| GET `/api/limiti/export` | legalEntityId:string(uuid) optional<br>matbroj:string optional<br>asOf:string(date-time) optional | 200 binary XLSX | limits.read |
| GET `/api/limiti/{id}` | none | 200 LimitResponseDTO | limits.read |
| PUT `/api/limiti/{id}` | body UpdateLimitDTO | 200 LimitResponseDTO | limits.edit |
| DELETE `/api/limiti/{id}` | none | 200 {message} | limits.delete |
| POST `/api/limiti/import` | multipart file:binary | 200 object | limits.create |

### Localization

Source: [LocalizationController.cs](../Backend/API/Controllers/LocalizationController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/localization/languages` | none | 200 {items:[{code,name}]} | application.access |
| GET `/api/localization/resources/{language}` | none | 200 RuntimeResourcePack | application.access |
| PUT `/api/localization/resources/{language}/{resourceKey}` | body PutRuntimeResource | 204 empty | administration.write |

### Modules

Source: [ModulesController.cs](../Backend/API/Controllers/ModulesController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/auth/modules` | none | 200 ModulesResponse | authenticated effective gost |

### Notifications

Source: [NotificationsController.cs](../Backend/API/Controllers/NotificationsController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/notifications` | page:integer(int32)=1<br>pageSize:integer(int32)=20<br>unreadOnly:boolean=False | 200 {items:Notification[],total,page,pageSize,unreadCount} | application.access |
| POST `/api/notifications/{id}/read` | none | 204 empty;404 missing/nonowner | application.access |

### PeriodLock

Source: [PeriodLockController.cs](../Backend/API/Controllers/PeriodLockController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/period-lock/current` | none | 200 {year,month,monthDisplay,isLocked,lockedBy,lockedAt} | capital.read |
| GET `/api/period-lock/status` | year:integer(int32) optional<br>month:integer(int32) optional | 200 {year,month,monthDisplay,isLocked,lockedBy,lockedAt} | capital.read |
| POST `/api/period-lock/lock` | body PeriodTargetDto | 200 {message} | capital.read + capital.write |
| POST `/api/period-lock/unlock` | body PeriodTargetDto | 200 {message} | capital.read + capital.write |
| POST `/api/period-lock/request-unlock` | body RequestUnlockDto | 200 {message} | capital.read + capital.write |
| GET `/api/period-lock/unlock-requests` | status:string optional<br>page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 {requests,total,page,pageSize} | capital.read |
| GET `/api/period-lock/unlock-requests/review` | status:string optional<br>page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 {requests,total,page,pageSize} | capital.read + capital.write |
| POST `/api/period-lock/unlock-requests/{id}/reject` | body AdminActionDto | 200 {message} | capital.read + capital.write |
| POST `/api/period-lock/unlock-requests/{id}/request-info` | body AdminActionDto | 200 {message} | capital.read + capital.write |
| POST `/api/period-lock/unlock-requests/{id}/respond` | body RespondDto | 200 {message} | capital.read + capital.write |

### ReferenceData

Source: [ReferenceDataController.cs](../Backend/API/Controllers/ReferenceDataController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/code-lists/dropdown/{kategorija}` | none | 200 CodeListDropdownDTO[] | authenticated effective gost |

### RelatedPerson

Source: [RelatedPersonController.cs](../Backend/API/Controllers/RelatedPersonController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/related-persons` | none | 200 RelatedPersonSummaryDTO[] | physical-persons.read |
| POST `/api/related-persons` | body CreateRelatedPersonDTO | 201 RelatedPersonResponseDTO | physical-persons.read + physical-persons.create |
| GET `/api/related-persons/detailed` | none | 200 RelatedPersonResponseDTO[] | physical-persons.read |
| GET `/api/related-persons/{id}` | none | 200 RelatedPersonResponseDTO | physical-persons.read |
| PUT `/api/related-persons/{id}` | body UpdateRelatedPersonDTO | 200 RelatedPersonResponseDTO | physical-persons.read + physical-persons.edit |
| DELETE `/api/related-persons/{id}` | none | 200 {message} | physical-persons.read + physical-persons.delete |
| GET `/api/related-persons/{id}/relationship-tree` | none | 200 RelatedPersonTreeNodeDTO[] | physical-persons.read |
| GET `/api/related-persons/check-duplicate` | jmbg:string optional<br>passportNumber:string optional<br>fbaId:string optional<br>excludeId:string(uuid) optional | 200 DuplicateIdentityResponseDTO | physical-persons.read |
| GET `/api/related-persons/page` | search:string optional<br>page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 RelatedPersonSummaryDTOReferencePage | physical-persons.read |
| GET `/api/related-persons/export` | none | 200 binary XLSX | physical-persons.read |
| GET `/api/related-persons/all-family-members` | none | 200 FamilyMemberResponseDTO[] | physical-persons.read |
| GET `/api/related-persons/{id}/family-members` | none | 200 FamilyMemberResponseDTO[] | physical-persons.read |
| POST `/api/related-persons/{id}/family-members` | body CreateFamilyMemberDTO | 201 FamilyMemberResponseDTO | physical-persons.read + physical-persons.create |
| GET `/api/related-persons/{id}/family-tree` | none | 200 FamilyMemberResponseDTO[] | physical-persons.read |
| PUT `/api/related-persons/{id}/family-members/{familyMemberId}` | body UpdateFamilyMemberDTO | 200 FamilyMemberResponseDTO | physical-persons.read + physical-persons.edit |
| DELETE `/api/related-persons/{id}/family-members/{familyMemberId}` | none | 200 {message} | physical-persons.read + physical-persons.delete |
| POST `/api/related-persons/import` | multipart file:binary | 200 ImportResultDTO | physical-persons.read + physical-persons.create |

### Report

Source: [ReportController.cs](../Backend/API/Controllers/ReportController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| POST `/api/reports/daily` | none | 201 ReportDTO | reports.read + reports.generate |
| GET `/api/reports/daily` | page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 ReportListDTO | reports.read |
| POST `/api/reports/monthly/{year}/{month}` | none | 201 ReportDTO | reports.read + reports.generate |
| GET `/api/reports/monthly` | page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 ReportListDTO | reports.read |
| GET `/api/reports/export/client/{identifier}` | none | 200 binary XLSX | reports.read |
| GET `/api/reports/export/all-clients` | none | 200 binary XLSX | reports.read |
| GET `/api/reports/export/generated/{reportId}` | none | 200 binary XLSX | reports.read |

### RoleCatalog

Source: [RoleCatalogController.cs](../Backend/API/Controllers/RoleCatalogController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/role-catalog` | page:integer(int32)=1<br>pageSize:integer(int32)=20<br>search:string optional | 200 {items:RoleCatalogDTO[],total,page,pageSize} | administration.read |
| POST `/api/role-catalog` | body CreateRoleCatalogDTO | 201 RoleCatalogDTO | administration.read + admin.only + DB Admin |
| GET `/api/role-catalog/{id}` | none | 200 RoleCatalogDTO | administration.read |
| PUT `/api/role-catalog/{id}` | body UpdateRoleCatalogDTO | 200 RoleCatalogDTO | administration.read + admin.only + DB Admin |
| GET `/api/role-catalog/permissions` | none | 200 {items:[{id,code,module,description}]} | administration.read |

### RoleRequests

Source: [RoleRequestsController.cs](../Backend/API/Controllers/RoleRequestsController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/role-requests/options` | none | 200 {items:[{id,name,displayName}]} | application.access |
| POST `/api/role-requests` | body CreateRoleRequestDTO | 201 RoleRequestDTO;200 matching replay | application.access |
| GET `/api/role-requests` | status:string optional<br>page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 RoleRequestDTOPagedResponse | application.access + admin.only + DB Admin |
| GET `/api/role-requests/mine` | status:string optional<br>page:integer(int32)=1<br>pageSize:integer(int32)=20 | 200 RoleRequestDTOPagedResponse | application.access |
| POST `/api/role-requests/{id}/approve` | body RoleRequestDecisionDTO | 200 RoleRequestDTO | application.access + admin.only + DB Admin |
| POST `/api/role-requests/{id}/reject` | body RoleRequestDecisionDTO | 200 RoleRequestDTO | application.access + admin.only + DB Admin |

### Roles

Source: [RolesController.cs](../Backend/API/Controllers/RolesController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/roles` | none | 200 GetRolesResponseDTO | administration.read |

### Users

Source: [UsersController.cs](../Backend/API/Controllers/UsersController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| GET `/api/users` | page:integer(int32)=1<br>pageSize:integer(int32)=20<br>search:string optional<br>role:string optional | 200 GetUsersResponseDTO | administration.read |
| POST `/api/users` | body CreateUserDTO | 201 UserDTO | administration.read + administration.write |
| PUT `/api/users/{userId}` | body UpdateUserProfileDTO | 200 {message,roles?} | administration.read + administration.write |
| DELETE `/api/users/{userId}` | none | 200 {message,roles?} | administration.read + administration.write |
| POST `/api/users/{userId}/roles` | body AssignRoleRequestDTO | 200 {message,roles?} | administration.read + administration.write |
| PUT `/api/users/{userId}/role` | body AssignRoleRequestDTO | 200 {message,roles?} | administration.read + administration.write |
| POST `/api/users/{userId}/reactivate` | none | 200 {message,roles?} | administration.read + administration.write |
| DELETE `/api/users/{userId}/permanent` | none | 200 {message,roles?} | administration.read + administration.write |

### Verification

Source: [VerificationController.cs](../Backend/API/Controllers/VerificationController.cs).

| Method / route | Query / body | Success | Authority |
| --- | --- | --- | --- |
| POST `/api/verification/physical-person/{id}` | none | 200 RelatedPersonResponseDTO | physical-persons.edit |
| POST `/api/verification/legal-person/{id}` | none | 200 LegalEntityDTO | legal-persons.edit |

### Public operational endpoints

GET `/health/live`, GET `/health/ready` → status-only JSON200/503; GET `/metrics` → Prometheus text, administration.read. Source: Program/health/metrics extensions. These probes are not registry endpoints and carry no user data.

## Complete named wire-property catalog

Each schema below lists exact serialized property names and wire types from the running published API, plus source-defined model constraints. `required` indicates explicit/implicit binding validation, not every business requirement; `default when omitted` refers to CLR binding behavior, not a recommended request. For writes, apply the stricter service rules above. Read-only schemas describe returned values; clients must not post audit, derived permission or identity properties back. Nullable reference annotations and runtime legacy data can differ; handle nullable response strings defensively. JSON examples above and family response envelopes are illustrative only.
### AdminActionDto

Source: [PeriodLockController.cs](../Backend/API/Controllers/PeriodLockController.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `note` | string or null | not explicitly required by schema |

### AssignRoleRequestDTO

Source: [AssignRoleRequestDTO.cs](../Backend/DL/DTO/Roles/AssignRoleRequestDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `roleIds` | string(uuid)[] or null | not explicitly required by schema |
| `roleId` | string(uuid) | not explicitly required by schema |
| `effectiveRoleIds` | string(uuid)[] or null | not explicitly required by schema; readOnly=True |

### BankCustomer

Source: [BankReferenceSource.cs](../Backend/BL/Services/BankReferenceSource.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `taxNumber` | string or null | not explicitly required by schema |
| `matbroj` | string or null | not explicitly required by schema |
| `name` | string or null | not explicitly required by schema |

### BankCustomerReferencePage

Source: [BankReferenceSource.cs](../Backend/BL/Services/BankReferenceSource.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `items` | BankCustomer[] or null | not explicitly required by schema |
| `total` | integer(int32) | not explicitly required by schema |
| `page` | integer(int32) | not explicitly required by schema |
| `pageSize` | integer(int32) | not explicitly required by schema |

### BankGroup

Source: [BankReferenceSource.cs](../Backend/BL/Services/BankReferenceSource.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `number` | string or null | not explicitly required by schema |
| `name` | string or null | not explicitly required by schema |

### BankGroupReferencePage

Source: [BankReferenceSource.cs](../Backend/BL/Services/BankReferenceSource.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `items` | BankGroup[] or null | not explicitly required by schema |
| `total` | integer(int32) | not explicitly required by schema |
| `page` | integer(int32) | not explicitly required by schema |
| `pageSize` | integer(int32) | not explicitly required by schema |

### CapitalDTO

Source: [ReportDTOs.cs](../Backend/DL/DTO/Report/ReportDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `regulatoryCapital` | number(double) | not explicitly required by schema |
| `coreCapital` | number(double) | not explicitly required by schema |
| `supplementaryCapital` | number(double) | not explicitly required by schema |
| `capitalDate` | string(date-time) | not explicitly required by schema |

### CapitalHistoryDTO

Source: [ReportDTOs.cs](../Backend/DL/DTO/Report/ReportDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `revision` | integer(int64) | not explicitly required by schema |
| `effectiveDate` | string(date-time) | not explicitly required by schema |
| `regulatoryCapital` | number(double) | not explicitly required by schema |
| `coreCapital` | number(double) | not explicitly required by schema |
| `supplementaryCapital` | number(double) | not explicitly required by schema |
| `recordedAt` | string(date-time) | not explicitly required by schema |
| `recordedBy` | string or null | not explicitly required by schema |
| `source` | string or null | not explicitly required by schema |

### CapitalHistoryDTOReferencePage

Source: [BankReferenceSource.cs](../Backend/BL/Services/BankReferenceSource.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `items` | CapitalHistoryDTO[] or null | not explicitly required by schema |
| `total` | integer(int32) | not explicitly required by schema |
| `page` | integer(int32) | not explicitly required by schema |
| `pageSize` | integer(int32) | not explicitly required by schema |

### CodeListDropdownDTO

Source: [CodeListDTO.cs](../Backend/DL/DTO/Sifarnici/CodeListDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `kod` | string or null | not explicitly required by schema |
| `naziv` | string or null | not explicitly required by schema |
| `opis` | string or null | not explicitly required by schema |

### CodeListResponseDTO

Source: [CodeListDTO.cs](../Backend/DL/DTO/Sifarnici/CodeListDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | integer(int32) | not explicitly required by schema |
| `kategorija` | string or null | not explicitly required by schema |
| `kod` | string or null | not explicitly required by schema |
| `naziv` | string or null | not explicitly required by schema |
| `opis` | string or null | not explicitly required by schema |
| `redoslijedPrikaza` | integer(int32) or null | not explicitly required by schema |
| `aktivan` | boolean | not explicitly required by schema |
| `kreiranDatum` | string(date-time) | not explicitly required by schema |
| `kreiraoKorisnik` | string or null | not explicitly required by schema |
| `izmijenjenDatum` | string(date-time) or null | not explicitly required by schema |
| `izmijenioKorisnik` | string or null | not explicitly required by schema |

### CreateCodeListDTO

Source: [CodeListDTO.cs](../Backend/DL/DTO/Sifarnici/CodeListDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `kategorija` | string or null | not explicitly required by schema |
| `kod` | string or null | not explicitly required by schema |
| `naziv` | string or null | not explicitly required by schema |
| `opis` | string or null | not explicitly required by schema |
| `redoslijedPrikaza` | integer(int32) or null | not explicitly required by schema |
| `aktivan` | boolean | not explicitly required by schema |

### CreateCodeListDefinitionDTO

Source: [CodeListDTO.cs](../Backend/DL/DTO/Sifarnici/CodeListDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `name` | string or null | not explicitly required by schema |
| `description` | string or null | not explicitly required by schema |

### CreateFamilyMemberDTO

Source: [FamilyMemberDTOs.cs](../Backend/DL/DTO/RelatedPersons/FamilyMemberDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `firstName` | string | required/non-null; minLength=0; maxLength=100 |
| `lastName` | string | required/non-null; minLength=0; maxLength=100 |
| `residency` | ResidencyType | required/non-null |
| `jmbg` | string or null | not explicitly required by schema; minLength=0; maxLength=13 |
| `passportNumber` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `fbaId` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `parentFamilyMemberId` | string(uuid) or null | not explicitly required by schema |

### CreateLegalEntityDTO

Source: [LegalEntityDTOs.cs](../Backend/DL/DTO/LegalEntity/LegalEntityDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `isResident` | boolean | required/non-null |
| `taxNumber` | string or null | not explicitly required by schema |
| `maticniBroj` | string or null | not explicitly required by schema; minLength=0; maxLength=50; pattern=^\d*$ |
| `fbaId` | string or null | not explicitly required by schema; pattern=^\d{1,10}$ |
| `name` | string | required/non-null; minLength=0; maxLength=100 |
| `matbroj` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `gccNumber` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `isLargeExposure` | boolean or null | not explicitly required by schema |
| `basisOfConnection` | string | required/non-null; minLength=0; maxLength=100 |
| `connectionDescription` | string or null | not explicitly required by schema; minLength=0; maxLength=500 |
| `connectedWithBank` | boolean or null | not explicitly required by schema |
| `dateFrom` | string(date-time) or null | not explicitly required by schema |
| `dateTo` | string(date-time) or null | not explicitly required by schema |

### CreateLimitDTO

Source: [LimitDTOs.cs](../Backend/DL/DTO/Limiti/LimitDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `legalEntityId` | string(uuid) | not explicitly required by schema |
| `tipLimita` | string or null | not explicitly required by schema |
| `odobreniLimit` | number(double) or null | not explicitly required by schema |
| `maksimalnaOcekivanaUtilizacija` | number(double) or null | not explicitly required by schema |
| `rokOcekivaneUtilizacije` | string(date-time) or null | not explicitly required by schema |
| `komentar` | string or null | not explicitly required by schema |

### CreateRelatedPersonDTO

Source: [RelatedPersonDTOs.cs](../Backend/DL/DTO/RelatedPersons/RelatedPersonDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `firstName` | string | required/non-null; minLength=0; maxLength=100 |
| `lastName` | string | required/non-null; minLength=0; maxLength=100 |
| `residency` | ResidencyType | required/non-null |
| `jmbg` | string or null | not explicitly required by schema; minLength=0; maxLength=13 |
| `passportNumber` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `fbaId` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `relationBasis` | string or null | not explicitly required by schema; minLength=0; maxLength=500 |
| `relationDescription` | string or null | not explicitly required by schema; minLength=0; maxLength=1000 |
| `specialRelationBasis` | string or null | not explicitly required by schema; minLength=0; maxLength=500 |
| `dateFrom` | string(date-time) or null | not explicitly required by schema |
| `dateTo` | string(date-time) or null | not explicitly required by schema |
| `isIdentifiedStaff` | boolean | not explicitly required by schema |
| `declarationNoFamilyMembers` | boolean | not explicitly required by schema |
| `connectedWithBank` | boolean | not explicitly required by schema |
| `specialRelationshipWithBank` | boolean | not explicitly required by schema |
| `specialContract` | boolean | not explicitly required by schema |
| `malusClawback` | boolean | not explicitly required by schema |
| `relatedToPersonId` | string(uuid) or null | not explicitly required by schema |

### CreateRoleCatalogDTO

Source: [RoleCatalogDTOs.cs](../Backend/DL/DTO/Roles/RoleCatalogDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `name` | string or null | not explicitly required by schema |
| `displayName` | string or null | not explicitly required by schema |
| `description` | string or null | not explicitly required by schema |
| `permissionIds` | string(uuid)[] or null | not explicitly required by schema |

### CreateRoleRequestDTO

Source: [RoleRequestDTOs.cs](../Backend/DL/DTO/Roles/RoleRequestDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `clientRequestId` | string(uuid) | not explicitly required by schema |
| `purpose` | string | required/non-null; minLength=1; pattern=^(new\|change)$ |
| `roleIds` | string(uuid)[] or null | not explicitly required by schema; maxItems=20 |
| `note` | string or null | not explicitly required by schema; maxLength=1000 |

### CreateUserDTO

Source: [GetUsersResponseDTO.cs](../Backend/DL/DTO/Users/GetUsersResponseDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `username` | string | required/non-null; minLength=0; maxLength=100 |
| `firstName` | string | required/non-null; minLength=0; maxLength=100 |
| `lastName` | string | required/non-null; minLength=0; maxLength=100 |
| `email` | string(email) | required/non-null; minLength=0; maxLength=200 |
| `roleIds` | string(uuid)[] or null | not explicitly required by schema |
| `roleId` | string or null | not explicitly required by schema |
| `isActive` | boolean | not explicitly required by schema |

### DuplicateIdentityResponseDTO

Source: [RelatedPersonDTOs.cs](../Backend/DL/DTO/RelatedPersons/RelatedPersonDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `exists` | boolean | not explicitly required by schema |
| `field` | string or null | not explicitly required by schema |
| `message` | string or null | not explicitly required by schema |

### FamilyMemberResponseDTO

Source: [FamilyMemberDTOs.cs](../Backend/DL/DTO/RelatedPersons/FamilyMemberDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `relatedPersonId` | string(uuid) | not explicitly required by schema |
| `firstName` | string or null | not explicitly required by schema |
| `lastName` | string or null | not explicitly required by schema |
| `residency` | ResidencyType | not explicitly required by schema |
| `residencyLabel` | string or null | not explicitly required by schema; readOnly=True |
| `jmbg` | string or null | not explicitly required by schema |
| `passportNumber` | string or null | not explicitly required by schema |
| `fbaId` | string or null | not explicitly required by schema |
| `parentFamilyMemberId` | string(uuid) or null | not explicitly required by schema |
| `createdAt` | string(date-time) | not explicitly required by schema |
| `createdBy` | string or null | not explicitly required by schema |
| `modifiedAt` | string(date-time) or null | not explicitly required by schema |
| `modifiedBy` | string or null | not explicitly required by schema |
| `children` | FamilyMemberResponseDTO[] or null | not explicitly required by schema |

### GetRolesResponseDTO

Source: [GetRolesResponseDTO.cs](../Backend/DL/DTO/Roles/GetRolesResponseDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `roles` | RoleDTO[] or null | not explicitly required by schema |

### GetUsersResponseDTO

Source: [GetUsersResponseDTO.cs](../Backend/DL/DTO/Users/GetUsersResponseDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `users` | UserDTO[] or null | not explicitly required by schema |
| `total` | integer(int32) | not explicitly required by schema |
| `page` | integer(int32) | not explicitly required by schema |
| `pageSize` | integer(int32) | not explicitly required by schema |

### ImportIssue

Source: [ImportResultDTO.cs](../Backend/DL/DTO/ImportResultDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `row` | integer(int32) | not explicitly required by schema |
| `field` | string or null | not explicitly required by schema |
| `code` | string or null | not explicitly required by schema |
| `message` | string or null | not explicitly required by schema |

### ImportPreviewDto

Source: [CodeListDTO.cs](../Backend/DL/DTO/Sifarnici/CodeListDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `totalRows` | integer(int32) | not explicitly required by schema |
| `toImport` | string[] or null | not explicitly required by schema |
| `duplicates` | string[] or null | not explicitly required by schema |
| `errors` | string[] or null | not explicitly required by schema |
| `imported` | integer(int32) | not explicitly required by schema |

### ImportResultDTO

Source: [ImportResultDTO.cs](../Backend/DL/DTO/ImportResultDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `imported` | integer(int32) | not explicitly required by schema |
| `failed` | integer(int32) | not explicitly required by schema |
| `errors` | string[] or null | not explicitly required by schema |
| `details` | ImportIssue[] or null | not explicitly required by schema |

### LegalEntityDTO

Source: [LegalEntityDTOs.cs](../Backend/DL/DTO/LegalEntity/LegalEntityDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `gccName` | string or null | not explicitly required by schema |
| `id` | string(uuid) | not explicitly required by schema |
| `isResident` | boolean | required/non-null |
| `taxNumber` | string or null | not explicitly required by schema |
| `maticniBroj` | string or null | not explicitly required by schema |
| `fbaId` | string or null | not explicitly required by schema |
| `name` | string or null | not explicitly required by schema |
| `matbroj` | string or null | not explicitly required by schema |
| `gccNumber` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `isLargeExposure` | boolean or null | not explicitly required by schema |
| `basisOfConnection` | string or null | not explicitly required by schema |
| `connectionDescription` | string or null | not explicitly required by schema |
| `connectedWithBank` | boolean or null | not explicitly required by schema |
| `dateFrom` | string(date-time) or null | not explicitly required by schema |
| `dateTo` | string(date-time) or null | not explicitly required by schema |
| `status` | string or null | not explicitly required by schema |
| `createdBy` | string or null | not explicitly required by schema |
| `createdAt` | string(date-time) | not explicitly required by schema |
| `modifiedBy` | string or null | not explicitly required by schema |
| `modifiedAt` | string(date-time) or null | not explicitly required by schema |
| `verifiedBy` | string or null | not explicitly required by schema |
| `verifiedAt` | string(date-time) or null | not explicitly required by schema |

### LegalEntityLimitFormDataDTO

Source: [LegalEntityDTOs.cs](../Backend/DL/DTO/LegalEntity/LegalEntityDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `isResident` | boolean | required/non-null |
| `fbaId` | string or null | not explicitly required by schema |
| `taxNumber` | string or null | not explicitly required by schema |
| `maticniBroj` | string or null | not explicitly required by schema |
| `matbroj` | string or null | not explicitly required by schema |
| `name` | string or null | not explicitly required by schema |
| `regulatoryCapital` | number(double) or null | not explicitly required by schema |
| `coreCapital` | number(double) or null | not explicitly required by schema |

### LegalEntityListDTO

Source: [LegalEntityDTOs.cs](../Backend/DL/DTO/LegalEntity/LegalEntityDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `items` | LegalEntityDTO[] or null | not explicitly required by schema |
| `total` | integer(int32) | not explicitly required by schema |
| `page` | integer(int32) | not explicitly required by schema |
| `pageSize` | integer(int32) | not explicitly required by schema |

### LegalEntityLookupDTO

Source: [LegalEntityDTOs.cs](../Backend/DL/DTO/LegalEntity/LegalEntityDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `name` | string or null | not explicitly required by schema |
| `maticniBroj` | string or null | not explicitly required by schema |
| `taxNumber` | string or null | not explicitly required by schema |

### LimitResponseDTO

Source: [LimitDTOs.cs](../Backend/DL/DTO/Limiti/LimitDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | integer(int32) | not explicitly required by schema |
| `legalEntityId` | string(uuid) | not explicitly required by schema |
| `naziv` | string or null | not explicitly required by schema |
| `isResident` | boolean | not explicitly required by schema |
| `fbaId` | string or null | not explicitly required by schema |
| `taxNumber` | string or null | not explicitly required by schema |
| `maticniBroj` | string or null | not explicitly required by schema |
| `matbroj` | string or null | not explicitly required by schema |
| `tipLimita` | string or null | not explicitly required by schema |
| `odobreniLimit` | number(double) or null | not explicitly required by schema |
| `maksimalnaOcekivanaUtilizacija` | number(double) or null | not explicitly required by schema |
| `rokOcekivaneUtilizacije` | string(date-time) or null | not explicitly required by schema |
| `komentar` | string or null | not explicitly required by schema |
| `regulatorniKapital` | number(double) or null | not explicitly required by schema |
| `osnovniKapital` | number(double) or null | not explicitly required by schema |
| `datumKapitala` | string(date-time) or null | not explicitly required by schema |
| `createdAt` | string(date-time) | not explicitly required by schema |
| `createdBy` | string or null | not explicitly required by schema |
| `modifiedAt` | string(date-time) or null | not explicitly required by schema |
| `modifiedBy` | string or null | not explicitly required by schema |

### LimitResponseDTOReferencePage

Source: [BankReferenceSource.cs](../Backend/BL/Services/BankReferenceSource.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `items` | LimitResponseDTO[] or null | not explicitly required by schema |
| `total` | integer(int32) | not explicitly required by schema |
| `page` | integer(int32) | not explicitly required by schema |
| `pageSize` | integer(int32) | not explicitly required by schema |

### ModuleDto

Source: [ModulesController.cs](../Backend/API/Controllers/ModulesController.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `key` | string or null | not explicitly required by schema |
| `name` | string or null | not explicitly required by schema |
| `route` | string or null | not explicitly required by schema |
| `icon` | string or null | not explicitly required by schema |
| `isEnabled` | boolean | not explicitly required by schema |

### ModulesResponse

Source: [ModulesController.cs](../Backend/API/Controllers/ModulesController.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `modules` | ModuleDto[] or null | not explicitly required by schema |

### PeriodTargetDto

Source: [PeriodLockController.cs](../Backend/API/Controllers/PeriodLockController.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `year` | integer(int32) or null | not explicitly required by schema |
| `month` | integer(int32) or null | not explicitly required by schema |

### ProblemDetails

Framework ProblemDetails response type.
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `type` | string or null | not explicitly required by schema |
| `title` | string or null | not explicitly required by schema |
| `status` | integer(int32) or null | not explicitly required by schema |
| `detail` | string or null | not explicitly required by schema |
| `instance` | string or null | not explicitly required by schema |

### PutRuntimeResource

Source: [RuntimeLocalizationCatalog.cs](../Backend/BL/Services/RuntimeLocalizationCatalog.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `label` | string or null | not explicitly required by schema |
| `helper` | string or null | not explicitly required by schema |
| `placeholder` | string or null | not explicitly required by schema |
| `isActive` | boolean | not explicitly required by schema |

### RelatedPersonResponseDTO

Source: [RelatedPersonDTOs.cs](../Backend/DL/DTO/RelatedPersons/RelatedPersonDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `firstName` | string or null | not explicitly required by schema |
| `lastName` | string or null | not explicitly required by schema |
| `residency` | ResidencyType | not explicitly required by schema |
| `residencyLabel` | string or null | not explicitly required by schema; readOnly=True |
| `jmbg` | string or null | not explicitly required by schema |
| `passportNumber` | string or null | not explicitly required by schema |
| `fbaId` | string or null | not explicitly required by schema |
| `relationBasis` | string or null | not explicitly required by schema |
| `relationDescription` | string or null | not explicitly required by schema |
| `specialRelationBasis` | string or null | not explicitly required by schema |
| `dateFrom` | string(date-time) or null | not explicitly required by schema |
| `dateTo` | string(date-time) or null | not explicitly required by schema |
| `isIdentifiedStaff` | boolean | not explicitly required by schema |
| `declarationNoFamilyMembers` | boolean | not explicitly required by schema |
| `connectedWithBank` | boolean | not explicitly required by schema |
| `specialRelationshipWithBank` | boolean | not explicitly required by schema |
| `specialContract` | boolean | not explicitly required by schema |
| `malusClawback` | boolean | not explicitly required by schema |
| `relatedToPersonId` | string(uuid) or null | not explicitly required by schema |
| `relatedToPersonName` | string or null | not explicitly required by schema |
| `relatedToPersonJmbg` | string or null | not explicitly required by schema |
| `relatedToPersonPassport` | string or null | not explicitly required by schema |
| `relatedToPersonFbaId` | string or null | not explicitly required by schema |
| `status` | RelatedPersonStatus | not explicitly required by schema |
| `statusLabel` | string or null | not explicitly required by schema; readOnly=True |
| `createdAt` | string(date-time) | not explicitly required by schema |
| `createdBy` | string or null | not explicitly required by schema |
| `modifiedAt` | string(date-time) or null | not explicitly required by schema |
| `modifiedBy` | string or null | not explicitly required by schema |
| `verifiedBy` | string or null | not explicitly required by schema |
| `verifiedAt` | string(date-time) or null | not explicitly required by schema |
| `familyMemberCount` | integer(int32) | not explicitly required by schema |
| `personType` | string or null | not explicitly required by schema; readOnly=True |
| `personTypeLabel` | string or null | not explicitly required by schema; readOnly=True |

### RelatedPersonStatus

Source: [RelatedPersonEnums.cs](../Backend/DL/Entities/RelatedPersons/RelatedPersonEnums.cs).
Numeric values: `[1, 2, 3, 4, 5, 6]`. Names are defined in RelatedPersonEnums.cs.
### RelatedPersonSummaryDTO

Source: [RelatedPersonDTOs.cs](../Backend/DL/DTO/RelatedPersons/RelatedPersonDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `firstName` | string or null | not explicitly required by schema |
| `lastName` | string or null | not explicitly required by schema |
| `jmbg` | string or null | not explicitly required by schema |
| `status` | RelatedPersonStatus | not explicitly required by schema |
| `statusLabel` | string or null | not explicitly required by schema; readOnly=True |
| `residency` | ResidencyType | not explicitly required by schema |
| `familyMemberCount` | integer(int32) | not explicitly required by schema |
| `isIdentifiedStaff` | boolean | not explicitly required by schema |
| `relatedToPersonId` | string(uuid) or null | not explicitly required by schema |
| `personType` | string or null | not explicitly required by schema; readOnly=True |
| `personTypeLabel` | string or null | not explicitly required by schema; readOnly=True |
| `createdAt` | string(date-time) | not explicitly required by schema |

### RelatedPersonSummaryDTOReferencePage

Source: [BankReferenceSource.cs](../Backend/BL/Services/BankReferenceSource.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `items` | RelatedPersonSummaryDTO[] or null | not explicitly required by schema |
| `total` | integer(int32) | not explicitly required by schema |
| `page` | integer(int32) | not explicitly required by schema |
| `pageSize` | integer(int32) | not explicitly required by schema |

### RelatedPersonTreeNodeDTO

Source: [RelatedPersonDTOs.cs](../Backend/DL/DTO/RelatedPersons/RelatedPersonDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `firstName` | string or null | not explicitly required by schema |
| `lastName` | string or null | not explicitly required by schema |
| `personType` | string or null | not explicitly required by schema |
| `personTypeLabel` | string or null | not explicitly required by schema |
| `children` | RelatedPersonTreeNodeDTO[] or null | not explicitly required by schema |

### ReportDTO

Source: [ReportDTOs.cs](../Backend/DL/DTO/Report/ReportDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `dataSource` | string or null | not explicitly required by schema |
| `isAuthoritative` | boolean | not explicitly required by schema; readOnly=True |
| `id` | string(uuid) | not explicitly required by schema |
| `reportType` | string or null | not explicitly required by schema |
| `reportDate` | string(date-time) | not explicitly required by schema |
| `totalClients` | integer(int32) | not explicitly required by schema |
| `clientsWithBreachedLimit` | integer(int32) | not explicitly required by schema |
| `totalExposure` | number(double) | not explicitly required by schema |
| `createdBy` | string or null | not explicitly required by schema |
| `createdAt` | string(date-time) | not explicitly required by schema |

### ReportListDTO

Source: [ReportDTOs.cs](../Backend/DL/DTO/Report/ReportDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `items` | ReportDTO[] or null | not explicitly required by schema |
| `total` | integer(int32) | not explicitly required by schema |
| `page` | integer(int32) | not explicitly required by schema |
| `pageSize` | integer(int32) | not explicitly required by schema |

### RequestUnlockDto

Source: [PeriodLockController.cs](../Backend/API/Controllers/PeriodLockController.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `reason` | string or null | not explicitly required by schema |
| `year` | integer(int32) or null | not explicitly required by schema |
| `month` | integer(int32) or null | not explicitly required by schema |

### ResidencyType

Source: [RelatedPersonEnums.cs](../Backend/DL/Entities/RelatedPersons/RelatedPersonEnums.cs).
Numeric values: `[1, 2]`. Names are defined in RelatedPersonEnums.cs.
### RespondDto

Source: [PeriodLockController.cs](../Backend/API/Controllers/PeriodLockController.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `message` | string or null | not explicitly required by schema |

### RoleCatalogDTO

Source: [RoleCatalogDTOs.cs](../Backend/DL/DTO/Roles/RoleCatalogDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `name` | string or null | not explicitly required by schema |
| `displayName` | string or null | not explicitly required by schema |
| `description` | string or null | not explicitly required by schema |
| `isActive` | boolean | not explicitly required by schema |
| `version` | integer(int32) | not explicitly required by schema |
| `permissionIds` | string(uuid)[] or null | not explicitly required by schema |
| `isProtected` | boolean | not explicitly required by schema |

### RoleDTO

Source: [GetRolesResponseDTO.cs](../Backend/DL/DTO/Roles/GetRolesResponseDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `name` | string or null | not explicitly required by schema |
| `userCount` | integer(int32) | not explicitly required by schema |

### RoleRequestDTO

Source: [RoleRequestDTOs.cs](../Backend/DL/DTO/Roles/RoleRequestDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `purpose` | string or null | not explicitly required by schema |
| `status` | string or null | not explicitly required by schema |
| `roleIds` | string(uuid)[] or null | not explicitly required by schema |
| `roleNames` | string[] or null | not explicitly required by schema |
| `note` | string or null | not explicitly required by schema |
| `createdAt` | string(date-time) | not explicitly required by schema |
| `processedAt` | string(date-time) or null | not explicitly required by schema |
| `processedBy` | string or null | not explicitly required by schema |
| `decisionNote` | string or null | not explicitly required by schema |
| `version` | integer(int32) | not explicitly required by schema |
| `requesterUsername` | string or null | not explicitly required by schema |
| `requesterEmail` | string or null | not explicitly required by schema |

### RoleRequestDTOPagedResponse

Source: [RoleRequestDTOs.cs](../Backend/DL/DTO/Roles/RoleRequestDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `items` | RoleRequestDTO[] or null | not explicitly required by schema |
| `total` | integer(int32) | not explicitly required by schema |
| `page` | integer(int32) | not explicitly required by schema |
| `pageSize` | integer(int32) | not explicitly required by schema |

### RoleRequestDecisionDTO

Source: [RoleRequestDTOs.cs](../Backend/DL/DTO/Roles/RoleRequestDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `decisionId` | string(uuid) | not explicitly required by schema |
| `expectedVersion` | integer(int32) | not explicitly required by schema; minimum=1; maximum=2147483647 |
| `note` | string or null | not explicitly required by schema; maxLength=1000 |

### RuntimeResource

Source: [RuntimeLocalizationCatalog.cs](../Backend/BL/Services/RuntimeLocalizationCatalog.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `resourceKey` | string or null | not explicitly required by schema |
| `label` | string or null | not explicitly required by schema |
| `helper` | string or null | not explicitly required by schema |
| `placeholder` | string or null | not explicitly required by schema |

### RuntimeResourcePack

Source: [RuntimeLocalizationCatalog.cs](../Backend/BL/Services/RuntimeLocalizationCatalog.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `language` | string or null | not explicitly required by schema |
| `fallbackLanguage` | string or null | not explicitly required by schema |
| `version` | string or null | not explicitly required by schema |
| `items` | RuntimeResource[] or null | not explicitly required by schema |

### SimpleValueRequest

Source: [CodeListDTO.cs](../Backend/DL/DTO/Sifarnici/CodeListDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `value` | string or null | not explicitly required by schema |

### UpdateCapitalDTO

Source: [ReportDTOs.cs](../Backend/DL/DTO/Report/ReportDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `regulatoryCapital` | number(double) or null | not explicitly required by schema |
| `coreCapital` | number(double) or null | not explicitly required by schema |
| `supplementaryCapital` | number(double) or null | not explicitly required by schema |
| `capitalDate` | string(date-time) or null | not explicitly required by schema |

### UpdateCodeListDTO

Source: [CodeListDTO.cs](../Backend/DL/DTO/Sifarnici/CodeListDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `naziv` | string or null | not explicitly required by schema |
| `opis` | string or null | not explicitly required by schema |
| `redoslijedPrikaza` | integer(int32) or null | not explicitly required by schema |
| `aktivan` | boolean | not explicitly required by schema |

### UpdateCodeListDefinitionDTO

Source: [CodeListDTO.cs](../Backend/DL/DTO/Sifarnici/CodeListDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `description` | string or null | not explicitly required by schema |
| `expectedVersion` | integer(int32) | not explicitly required by schema |

### UpdateFamilyMemberDTO

Source: [FamilyMemberDTOs.cs](../Backend/DL/DTO/RelatedPersons/FamilyMemberDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `firstName` | string | required/non-null; minLength=0; maxLength=100 |
| `lastName` | string | required/non-null; minLength=0; maxLength=100 |
| `residency` | ResidencyType | required/non-null |
| `jmbg` | string or null | not explicitly required by schema; minLength=0; maxLength=13 |
| `passportNumber` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `fbaId` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `parentFamilyMemberId` | string(uuid) or null | not explicitly required by schema |

### UpdateLegalEntityDTO

Source: [LegalEntityDTOs.cs](../Backend/DL/DTO/LegalEntity/LegalEntityDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `isResident` | boolean | required/non-null |
| `taxNumber` | string or null | not explicitly required by schema; minLength=0; maxLength=13 |
| `fbaId` | string or null | not explicitly required by schema; minLength=0; maxLength=10; pattern=^\d{1,10}$ |
| `name` | string | required/non-null; minLength=0; maxLength=100 |
| `maticniBroj` | string or null | not explicitly required by schema; minLength=0; maxLength=50; pattern=^\d*$ |
| `matbroj` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `gccNumber` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `isLargeExposure` | boolean or null | not explicitly required by schema |
| `basisOfConnection` | string | required/non-null; minLength=0; maxLength=100 |
| `connectionDescription` | string or null | not explicitly required by schema; minLength=0; maxLength=500 |
| `connectedWithBank` | boolean or null | not explicitly required by schema |
| `dateFrom` | string(date-time) or null | not explicitly required by schema |
| `dateTo` | string(date-time) or null | not explicitly required by schema |
| `status` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |

### UpdateLimitDTO

Source: [LimitDTOs.cs](../Backend/DL/DTO/Limiti/LimitDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `legalEntityId` | string(uuid) | not explicitly required by schema |
| `tipLimita` | string or null | not explicitly required by schema |
| `odobreniLimit` | number(double) or null | not explicitly required by schema |
| `maksimalnaOcekivanaUtilizacija` | number(double) or null | not explicitly required by schema |
| `rokOcekivaneUtilizacije` | string(date-time) or null | not explicitly required by schema |
| `komentar` | string or null | not explicitly required by schema |

### UpdateRelatedPersonDTO

Source: [RelatedPersonDTOs.cs](../Backend/DL/DTO/RelatedPersons/RelatedPersonDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `firstName` | string | required/non-null; minLength=0; maxLength=100 |
| `lastName` | string | required/non-null; minLength=0; maxLength=100 |
| `residency` | ResidencyType | required/non-null |
| `jmbg` | string or null | not explicitly required by schema; minLength=0; maxLength=13 |
| `passportNumber` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `fbaId` | string or null | not explicitly required by schema; minLength=0; maxLength=50 |
| `relationBasis` | string or null | not explicitly required by schema; minLength=0; maxLength=500 |
| `relationDescription` | string or null | not explicitly required by schema; minLength=0; maxLength=1000 |
| `specialRelationBasis` | string or null | not explicitly required by schema; minLength=0; maxLength=500 |
| `dateFrom` | string(date-time) or null | not explicitly required by schema |
| `dateTo` | string(date-time) or null | not explicitly required by schema |
| `isIdentifiedStaff` | boolean | not explicitly required by schema |
| `declarationNoFamilyMembers` | boolean | not explicitly required by schema |
| `connectedWithBank` | boolean | not explicitly required by schema |
| `specialRelationshipWithBank` | boolean | not explicitly required by schema |
| `specialContract` | boolean | not explicitly required by schema |
| `malusClawback` | boolean | not explicitly required by schema |
| `relatedToPersonId` | string(uuid) or null | not explicitly required by schema |

### UpdateRoleCatalogDTO

Source: [RoleCatalogDTOs.cs](../Backend/DL/DTO/Roles/RoleCatalogDTOs.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `displayName` | string or null | not explicitly required by schema |
| `description` | string or null | not explicitly required by schema |
| `isActive` | boolean | not explicitly required by schema |
| `expectedVersion` | integer(int32) | not explicitly required by schema |
| `permissionIds` | string(uuid)[] or null | not explicitly required by schema |

### UpdateUserProfileDTO

Source: [GetUsersResponseDTO.cs](../Backend/DL/DTO/Users/GetUsersResponseDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `username` | string | required/non-null; minLength=0; maxLength=100 |
| `firstName` | string | required/non-null; minLength=0; maxLength=100 |
| `lastName` | string | required/non-null; minLength=0; maxLength=100 |
| `email` | string(email) | required/non-null; minLength=0; maxLength=200 |

### UserDTO

Source: [GetUsersResponseDTO.cs](../Backend/DL/DTO/Users/GetUsersResponseDTO.cs).
| JSON field | Wire type | Model constraints |
| --- | --- | --- |
| `id` | string(uuid) | not explicitly required by schema |
| `username` | string or null | not explicitly required by schema |
| `firstName` | string or null | not explicitly required by schema |
| `lastName` | string or null | not explicitly required by schema |
| `email` | string or null | not explicitly required by schema |
| `roles` | string[] or null | not explicitly required by schema |
| `effectivePermissions` | string[] or null | not explicitly required by schema |
| `identityConfirmed` | boolean | not explicitly required by schema |
| `isActive` | boolean | not explicitly required by schema |
