# Backend finalization: audit and proposed contract

Audit date: 2026-11-02. Repository: `RBBHHM/rbbh-studentskapraksa-tim1`.
Baseline: `origin/main` = `48cf8da8f0bb77129ea0b14ad5366c7cea7ce14b`.
Working branch: `feat/codex-backend-finalization`. No production environment was accessed.

Sources: all 20 pages of `[Related Parties Register] Dokumentacija.pdf`, including the embedded screenshots, and `docs/POPRAVKE.txt`. The current main is authoritative for implemented behavior. Earlier migration handoffs are historical evidence, not verification of this baseline.

## Phase 0: scope and baseline

The file inventory covers all tracked Backend files, backend tests and workflow configuration (257 entries across those areas, including generated migrations). A local audit ledger with hashes, signatures and persistence/security markers is under ignored `artifacts/backend-finalization/`. Review follows controller → permission → service → persistence → test paths; generated migration designers are treated as schema history, not rewritten runtime code.

Fresh baseline results:

- Solution restore: PASS.
- Release solution build with analyzers: FAIL, three BrowserHost reference errors (`RBBH.RelatedPartiesRegister` and `RBI` namespaces). Frontend and backend now both use the `DATA_CORE` assembly identity. Frontend ownership must resolve this integration defect; backend-only validation remains possible.
- Backend unit tests: 269 PASS, 9 FAIL, 278 total. All failures are existing `JwtAudienceConfigurationTests` expectations. Main sets `ValidateAudience = false` in every environment.
- Backend integration tests: 162/162 PASS, no skipped tests, with `RBBH_TEST_SQLSERVER` set to LocalDB. This includes disposable SQL identity-upgrade and last-Admin concurrency coverage. Passing existing tests does not prove the missing requirements below.

No frontend file, existing migration, production database or identity record is changed by this audit.

### Requirement-to-code matrix

Status describes the audited baseline. DONE means the stated narrow requirement exists with local evidence; PARTIAL means a route or implementation exists but does not satisfy the full requirement; MISSING means no implementation was found; BLOCKED identifies an external input or approval dependency. P0 blocks secure release, P1 functional correctness, P2 maintenance/performance. Paths below are relative to the repository; `S` = `Backend/BL/Services`, `C` = `Backend/API/Controllers`, `E` = `Backend/DL/Entities`, `A` = `Backend/IoC/Extensions/Authentication`, `UT`/`IT` = existing backend unit/integration test projects. These aliases identify affected paths without repeating long prefixes.

| ID | Source | Status | Existing implementation / gap | Required change; affected files | Dependency | Required evidence | Priority |
|---|---|---|---|---|---|---|---|
| AUTH-01 | PDF 5; corrections security | PARTIAL | Controllers have authentication and application policies; reference-data/modules require authentication only; health is deliberately anonymous | Complete endpoint/verb matrix, include minimal auth routes and metrics; C, Program, IT | Approved public endpoint exceptions | Real-pipeline 401/403/423 matrix, no mutation on denial | P0 |
| AUTH-02 | PDF 5 | DONE | Provider role/permission claims stripped; active local role-permission grants authoritative | Preserve `A/RoleClaimsTransformation.cs` | None | Existing transformation/HTTP tests | P0 |
| AUTH-03 | PDF 5; Phase 3 | DONE | Unknown authenticated identities get virtual `gost` without AppUser creation; inactive users denied | Preserve resolver and baseline; correct obsolete known-user-only comments | Latest requirement supersedes older unknown!=Guest decisions | `SubjectLinkAndVirtualGuestTests`, transformation tests | P0 |
| AUTH-04 | PDF 5 | PARTIAL | Roles and grants persisted; assignable-name allow-list and Guest grant matrix are code constants | Catalog-backed display/configurable choices; stable permission keys stay technical invariants; A, S/RoleService, role entities | C1 contract approval | Active/inactive roles, multi-role union, provider forgery | P1 |
| AUTH-05 | PDF 5 | DONE | Multi-role create/replace persists UserRoles; self-management/final Admin guards | Preserve `AppUserService.Mutations`, `UsersMutationBoundary` | None | Existing users HTTP and SQL races | P0 |
| AUTH-06 | PDF 5 | MISSING | No role-request entity, approval endpoint or assignment workflow | Implement C1 request/approval transaction and notifications in existing projects | C1 + security decision S3 | Owner/approver IDOR, duplicate approval, atomic roles+audit, rejection | P1 |
| AUTH-07 | PDF 5 | PARTIAL | Cookie/OIDC exist; no explicit eight-hour app session; server token/cookie expiry differ | Agree C2 lifetime/refresh contract, then configure backend cookie and coordinate frontend session | C2 + realm policy | 8h/expiry/refresh failures and restart behavior | P1 |
| AUTH-08 | Phase 3/8; PDF 5 | BLOCKED | JWT Audience disabled unconditionally; nine baseline tests fail | S1: restore configured audience and fail-closed startup outside Development; A/AuthenticationServiceExtensions | Security-policy approval | Existing audience suite plus real bearer HTTP rejection | P0 |
| ID-01 | PDF 6 | DONE | Confirmed subject-first resolution survives email/username changes without matching new privileged identities by email | Preserve `A/ApplicationIdentityResolver` | None | Changed claims/new subject/missing claims/conflicts tests | P0 |
| ID-02 | PDF 6 | PARTIAL | Verified unique email first-link is opt-in, persisted and audited; legacy backfill not performed | Keep switch off until approved reconciliation; normalized uniqueness and account recreation runbook | Real realm/export + approved backfill | SQL upgrade and parallel first-link; new-sub does not inherit roles | P0 |
| ID-03 | PDF 6, 15 | PARTIAL | Normalized schema supports multiple roles; username/email unique indexes; input prechecks can race or differ from SQL collation | Keep relational identity tables; normalize prechecks, safe duplicate result, creation audit atomic; S/AppUserService | C1; database collation/duplicate preflight for any index change | Duplicate/trim/case/concurrent create on SQL | P1 |
| ID-04 | PDF 15 | PARTIAL | AppUser creation optionally calls Keycloak Admin; wrong AdminClientId selection; unused KC role mutation surface | Separate identity lookup from application grants; disposal/exact lookup; S/KeycloakAdminService | Real service-account permissions; no invented secret | Stub HTTP failure/cancel/disposal; real SSO external | P1 |
| ID-05 | PDF 14–15 | PARTIAL | Create/roles/deactivate/delete/reactivate exist; no general profile edit API; corporate email restriction contradicts editable other domains | Add explicit edit DTO/endpoint; C/Users, S/AppUserService | C1 and accepted email-domain policy | Unique email/username edits, no subject/privilege reassignment | P1 |
| SAFE-01 | PDF 10 | BLOCKED | Both Verify methods allow creator verification; Legal Update accepts arbitrary Status | S2: enforce four-eyes and server-owned status; S/LegalEntityService, RelatedPersonService, VerificationController | Security-policy approval; stable actor semantics | Creator denied, other verifier allowed, update cannot bypass Verify, period lock | P0 |
| SAFE-02 | PDF 5 | DONE | Own deactivate/delete/role changes denied; SQL application lock serializes final active confirmed Admin reduction | Preserve mutation boundary | None | Existing HTTP denials, rollback audit, SQL mutual races | P0 |
| DB-01 | PDF 7, 16 | PARTIAL | Mapping/history inspected; soft filters, Restrict and unique indexes exist; audit lengths/names mixed; role UserId has no AppUser FK | Preflight orphan users and field-length mismatches; additive constraints only after review | Data inventory/backup | Disposable migration upgrade with data and rollback rehearsal | P1 |
| DB-02 | Phase 4 | BLOCKED | Historical migrations drop GCC/limit columns; limits migration adds empty Guid FK; fresh upgrade tests use empty tables | Do not rewrite applied history or run against populated DB without approved data plan | Production backup, legacy mapping | Representative nonempty upgrade tests; restore proof | P0 |
| DB-03 | PDF 4, 17 | PARTIAL | Program now runs MigrateAsync unconditionally in Development, ignores ApplyMigrations; InMemory path cannot migrate | Respect explicit startup migration configuration and relational provider; Program | Deployment migration contract, no production DDL | Development no-SQL startup; no unexpected DDL | P1 |
| LEG-01 | PDF 7–8 | PARTIAL | HTTP validation exists; imports bypass annotations; service does not bound name/Matbroj/description; FBA regex accepts Unicode digits | Share service validation across HTTP/import without changing DTO shape; S/LegalEntityService, DTO/LegalEntity | Tax rule clarification: PDF max13 vs implemented exact13 checksum | Direct service + HTTP + XLSX boundary values | P1 |
| LEG-02 | PDF 8 | DONE | DateFrom required and DateTo consistency enforced; bank-related flag/basis/description validated | Preserve service guards; residency bool omission needs contract review | C3 for required bool omission | Existing validation tests plus import rows | P1 |
| LEG-03 | PDF 8, 18 | BLOCKED | No GCC fields/lookups; prior migration removed them | C3 additive GCC fields and bank lookup source adapter | `bosnadb\HEAD`, kod/glava access + approved schema/API | Authorized source contract tests, missing/unavailable source rejects safely | P1 |
| LEG-04 | PDF 10 | MISSING | No large-exposure boolean | Add field only after C3 agreement | Meaning/default/backfill and report mapping | Create/edit/export/report inclusion | P1 |
| LEG-05 | PDF 8 | PARTIAL | LIKE depends on SQL collation; active-only duplicate checks conflict with legal unfiltered unique indexes | Confirm case-insensitive collation and inactive duplicate policy; no silent index relaxation | Data/business decision | SQL case tests, concurrent/inactive duplicates | P1 |
| XLSX-01 | PDF 8–11 | PARTIAL | Header/5000-row/10MB checks; valid rows batched; legal export11 vs import10 columns and differing order | C3 document versioned round-trip format incl metadata; maintain legacy import aliases | Shared workbook contract | Export→import, mixed invalid rows, exact counts | P1 |
| XLSX-02 | PDF 8–11 | PARTIAL | Legal/physical malformed workbook can escape as 500; missing-name nonempty rows skipped; catch-all exposes exception text | Safe malformed-file/row errors; field-level validation; define partial success and preserve transaction boundary | Existing shape retained; C3 for new format | Corrupt ZIP/XLSX, formulas/values/long fields, duplicates/resource limits | P1 |
| PHY-01 | PDF 10–11 | PARTIAL | CRUD/family graph/duplicate identifiers/validation exist; verification not four-eyes; DateTo required for physical | Preserve graph and cycle checks; align approved requirements | S2; do not apply legal optional-DateTo rule blindly | Existing validator/tree/HTTP plus self-verification tests | P1 |
| PHY-02 | PDF 11 | PARTIAL | Physical export lacks requested created/modified metadata and linked-person JMBG/description; import15 columns differs | C3 versioned format, batch resolve references | Contract approval | Family identifier/export description/round-trip | P1 |
| LIM-01 | PDF 12 | PARTIAL | DB-backed limit types and joined legal lookups; blank search Take20 lacks paging | C3 paged searchable lookup; preserve current consumers | Frontend contract coordination | Blank first page, page2, search, inactive types | P1 |
| LIM-02 | PDF 12 | PARTIAL | CRUD/export available, import missing; global capital used for all dates | Agree history-aware capital resolution + import format | C3/C4 | Dated limits/capital, import and authorization | P1 |
| CAP-01 | PDF 12 | MISSING | Single mutable global capital upsert; no effective-date history | C4 additive capital revisions with as-of selection, no invented historical values | Contract and data backfill approval | Multi-date/as-of, immutable history, duplicate/concurrent effective date | P1 |
| REP-01 | PDF 13–14 | DONE | Read/export Guest baseline; generate/persist restricted Capital/Admin with audit | Preserve side-effecting generate workflow and policies | None | Existing reporting HTTP authorization tests | P0 |
| REP-02 | PDF 13–14,18 | BLOCKED | Reports persist local ClientLimits snapshot; no authoritative finance integration | Read-only source contract; Finrep_db Monthlyhistory date tables and approved two-column mapping | Missing finance spreadsheets, SQL access, approved reconciliation | Finance examples/total reconciliation, timeout/source outage | P0 |
| REP-03 | Phase 7/9 | PARTIAL | Report paging not bounded/stable; arbitrary month/year can throw | Validate period and clamp paging without changing response shape; S/ReportService, C/ReportController | None | Invalid ranges/overflow, stable ordering, existing export regression | P1 |
| NOT-01 | PDF 14 | MISSING | Email preview queue is not durable recipient-scoped in-app notifications; no unread state/role notices | C1 notification/inbox + mail delivery state, recipient ownership | Schema/API + security S3 | Owner-only list/read/unread; retries/delivery failure | P1 |
| NOT-02 | PDF 14 | PARTIAL | HR emails still sent after physical changes; requirement says retire until redesigned | Explicitly retire HR flow as specified, coordinate existing tests | Confirm replacement recipient/workflow requirements | No HR message for create/expiry; business response remains correct | P1 |
| NOT-03 | PDF 14 | PARTIAL | Email links use old /admin/period and /notifikacije; template fields unencoded; broadcasts request500 but users clamp200 | Encode dynamic HTML; canonical authenticated links; page all recipients | None for encoding/routes; delivery idempotency C1 | Injection payload, encoded href, >200 recipients, no recipient PII logs | P1 |
| PER-01 | Phase 7 | DONE | Global middleware locks physical/legal/limits/capital/verification mutations; reports persist exempt by established policy | Preserve routes and policies | None | Existing route/unit + HTTP lock matrix | P0 |
| PER-02 | Phase 7 | PARTIAL | Unlock response10–490+prefix guarded; other period inputs unbounded; state/audit/mail not atomic | Validate calendar/note bounds; C/PeriodLock, repositories; transactional workflow | S3 for owner/status rules | Boundary/invalid input, no persistence, correct audit | P1 |
| PER-03 | Phase 3/7 | BLOCKED | Request lists visible through CapitalRead baseline; response lacks requester ownership; process permissions need review | S3 owner-scoped requests, explicit approver grant, transitions/replay rules | Security-policy approval | Cross-user request IDs denied, duplicate approval, status/owner checks | P0 |
| CODE-01 | PDF 16 | PARTIAL | Definition/value CRUD; deletion checks references; no category FK/composite unique index; Update deactivation bypasses Delete checks | Align all disable paths, normalized uniqueness, system-value protection | C1 codebook integrity contract + duplicate preflight | In-use update/delete/category, inactive duplicates/races | P1 |
| CODE-02 | PDF 16 | PARTIAL | Import exists; some service failures not added to preview; category definition update/metadata incomplete; export missing | Complete defined format/errors/audit and CRUD | C1/C3 | Row errors/atomicity/import-export, definition updates | P1 |
| CODE-03 | PDF 16 | PARTIAL | Backend uncached reads are fresh; no central versioning/invalidation for consumers | Add bounded versioned cache only with mutation invalidation; no speculative cache | Frontend cache contract | Update immediately visible, inactive values excluded | P2 |
| LOC-01 | PDF 19 | MISSING | No Languages/LanguagePacks backend; current business/error labels literal BS/EN | C5 two logical codebooks + stable semantic keys/fallback/version, backend API error localization | Shared language-pack contract | Runtime edit, fallback, missing key diagnostics, Accept-Language | P1 |
| AUD-01 | PDF 16,18 | PARTIAL | Manual audits widespread; user reductions atomic; many controllers save data before separate audit; actor uses email | Stable subject + username snapshot, service transaction for data+audit; avoid global interceptor logging secrets | C1 actor contract and field backfill | Successful writes exactly once, rejected writes none, audit failure rollback | P1 |
| ERR-01 | PDF 17 | PARTIAL | Safe global handler exists; competing ProblemDetails customization exposes exception/innerException; Period UnsafeStringConverter bypasses JSON validation | Keep existing response shapes; remove internal detail leakage and unsafe raw JSON | C5 before uniform localized envelope | Real handler/fallback error redaction and valid Unicode/control JSON | P0 |
| OBS-01 | PDF 6,17 | PARTIAL | Correlation middleware accepts arbitrary header; Console logs duplicate JWT/PII; mail operational logs include recipients | Bounded correlation + route-template/status/trace logs; no raw subject/email/claims/exception text | No permission/DTO changes | Captured logs with sentinel PII/tokens, auth failures and 403 | P0 |
| OBS-02 | PDF 17 | PARTIAL | Prometheus, liveness/readiness SQL exist; no mail/KC/cache/scheduler health contract or failure alerts | Define readiness vs telemetry/dependency degradation, slow-query metrics | Deployment telemetry/retention policy | Outages, redaction, SLO/alert evidence | P2 |
| OPS-01 | PDF 4,17 | PARTIAL | Trusted proxies/cookie HTTPS/CORS fail closed; backend Docker entrypoint differs from actual DATA_CORE.dll | Fix backend entrypoint only; validate artifact startup | Container registry access for real image | Release publish + entry assembly startup; OCP external | P0 |
| OPS-02 | PDF 4,17 | BLOCKED | No verified real OCP/3scale, shared sessions, persisted DP keys, resources/drain/scheduler overlap | Keep deployment prerequisites explicit; no invented infrastructure values | RBBH infrastructure/SSO/SQL/mail/cert owners | Real route, proxy, failover, backups/restore/load | P0 |
| PERF-01 | PDF 4,17 | PARTIAL | Several joins/AsNoTracking and batched imports; users all-candidate materialization, KC lookup N+1, unpaged physical/limits | Measure realistic authorized dataset before optimization; batch exact KC identity lookup | Representative data and contracts | Query count/latency/allocation/cancellation, no change to permission sets | P2 |
| TEST-01 | PDF 17 | PARTIAL | Existing278 unit+162 integration; SQL tests execute with explicit LocalDB; new main fails full build and JWT tests | Preserve real-path tests; add discovered boundaries; restore full gate after coordinated fixes | Frontend build + S1 | Backend/SQL/full .NET/browser/build/publish actual totals | P0 |
| DOC-01 | PDF 18 | PARTIAL | Root README/onboarding exist; many historical audit reports remain | Consolidation must preserve evidence; this single working report combines matrix/contract/results | Cross-owner documentation review | Accurate startup/config links; no fake production claims | P2 |
| FE-01 | PDF 9–11,15,19–20 | BLOCKED | Placeholder/focus/validation-summary, contrast, navigation, release notes, keyboard/pager rendering are frontend ownership | Handoff only; no changes under Frontend | Frontend agent | Frontend/browser acceptance | P1 |

### Architecture and schema findings

Keep the existing small controller/service/EF structure. Moving every query behind another repository or merging normalized identity/role tables into generic codebook text would reduce safety and readability. Specific demonstrated problems are split data/audit commits, identity lookup coupled to KC realm-role calls, mixed Result/exception handling, manual string JSON serialization, missing cancellation, stale scaffolding (`JsonPlaceholder`, Temenos and transaction helpers), and permission duplication in module navigation. Do not remove a helper merely because its only caller is a test until the integration/ownership review confirms it is obsolete.

Key schema risks: unfiltered legal unique IDs conflict with active-only checks; physical IDs have active-filtered unique indexes; CodeList category/code has no composite uniqueness or definition FK; UserRoles.UserId lacks FK; historical applied migrations have irreversible data drops; effective-date capital history does not exist. Email/username indexes rely on deployment collation and do not encode normalized identity rules. `CodeListDefinition` lacks modified metadata. Account/audit fields allow only100 characters while email allows200. Do not silently truncate or relax indexes. PKCS9.0.21 is preserved; no NU1900/NU1902/NU1903 suppression is proposed.

## Phase 1: proposed shared architecture contract (not yet agreed)

Existing route/DTO formats stay unchanged until frontend coordination. This proposal is reviewable guidance, not an announcement that new endpoints already exist.

**C1 — Identity, authorization, requests and catalogs.** Keep normalized AppUsers, Roles, Permissions, UserRoles and RolePermissions. Present business role definitions through codebook administration if desired, with one authoritative role catalog rather than a second generic copy. Keycloak owns authentication; only the application database owns privileged grants. An unprovisioned authenticated identity receives virtual Guest without an AppUser insert. Confirmed subject is the durable key; names/emails are mutable profile attributes. First-link remains explicitly opt-in and restricted to unique verified email/unconfirmed records; new subjects never inherit a confirmed user's grants by matching email. Reject ambiguous/inactive identity matches. A role request stores trusted subject, display-name/email snapshots, requested role IDs, optional note, purpose(new/change), state, timestamps and version. Never accept applicant/approver identity from a request body. Proposed additive routes: authenticated `GET /api/role-requests/options`, `POST /api/role-requests`, owner-scoped `GET /api/role-requests/mine`, approver-scoped `GET /api/role-requests`, `POST /api/role-requests/{id}/approve` or `/reject`. Approval uses a version/idempotency key and one SQL transaction for request transition, identity provisioning/link verification, role changes and audit, under the existing final-Admin mutation boundary where applicable. Self-approval denied. Reject stale/replayed incompatible decisions with409. Replaying an identical successful decision must not grant or notify twice. A durable pending-delivery/outbox record is committed with business state; mail failure does not roll back a completed approval or pretend delivery succeeded. Email links navigate to authenticated pages; they never approve by GET or carry bearer tokens. These routes and entities require agreement before implementation.

**C2 — Session.** Propose an absolute eight-hour application session with explicit cookie expiry and no sliding extension beyond that ceiling. Access-token expiry remains enforced; server-side refresh may continue only while both the app session and real realm refresh policy permit it. Central Keycloak SSO/access/refresh lifetimes cannot be extended locally. Logout/session revoke must clear server state. The Blazor token session is frontend/platform ownership: do not configure only the backend cookie and claim the complete8h UX works. Agree refresh/re-authentication behavior and shared cache/DataProtection persistence first.

**C3 — Registry and workbook contracts.** Agree additive legal GCC number/name and large-exposure flag, define null/default/backfill semantics, preserve existing Matbroj/MaticniBroj fields until a reviewed migration resolves duplication. Confirm tax-number max13 versus current exact13+checksum. Bank customer/GCC source is `kod.dbo.komitent` and `glava.dbo.grupekom` on the supplied server name; connectivity and authoritative examples are unavailable. No fabricated fallback data. Version the import/export format with common ordered business columns and optional read-only created/modified metadata columns; import must ignore declared metadata rather than trust it as actor data. Keep legacy header aliases during transition. Define partial success as individually validated rows staged then committed once; failed validation rows are counted, blank entire rows ignored, storage failure commits no rows. Add resource limits before workbook expansion where practicable. Paged legal lookup is additive until consumers adopt it.

**C4 — Capital and finance.** Add immutable capital revisions with effective date and audit/version, plus an explicit as-of read contract. Do not infer past capital from the current mutable row. Agree duplicate-date/version/correction semantics and initial-data reconciliation. Report snapshots remain persisted; generate remains a privileged mutation. Authoritative finance mapping requires the missing referenced spreadsheets, connection permissions and sample reconciled totals; do not compose arbitrary dynamic table names from input or claim the present local snapshot is a real bank report.

**C5 — Codebooks/localization/errors.** Provide two logical codebooks in the application's standard administrative area: supported languages (BS,EN,DE) and language-pack resources. Names are application-specific; no DP_ prefix requirement. Pack records have active, language, resourceKey, label, helper and placeholder, with unique normalized language+key. New keys use lowercase semantic dot notation. DataProducts ID1/12/13/18/19/20 is a reference mapping only, not an imposed SQL model. Proposed additive read API returns language, version and resources; authenticated reads, administrative writes, bounded caching with version/invalidation. Fallback requestedlanguage→configured default→stable key, with rate-limited missing-key diagnostics without submitted data. Error responses retain status and field compatibility, add stable error/resource keys and safe parameters; no raw SQL/exception/identity data. Agree the localized error extension before unifying existing ProblemDetails and `errors[]` consumers.

**Migration strategy.** No destructive migration or production backfill is authorized. Start with read-only duplicate/orphan/length/collation and subject-export reconciliation. Backup and prove restore; rehearse against a masked production-shaped copy. Apply reviewed additive migrations as a deployment step with an expand/contract rollout. Do not automatically run DDL just by starting an app against a configured database. Backfill only approved mappings in batches with counts and audit; add validated constraints afterwards. New code must tolerate pre-backfill state; rollback the application while retaining additive data, and restore backup for destructive rollback. Never rewrite already-applied migrations or delete old production identity links.

### Approval boundary and next order

The task's execution rule requires approval before security-policy changes and incompatible contracts. Outstanding concrete decisions:

1. **S1:** configured JWT Audience validated in every environment; missing Audience rejected outside Development; only unconfigured local Development retains the existing documented exception. Existing tests already describe this behavior, but current main explicitly disables it.
2. **S2:** four-eyes verification (creator cannot verify own legal/physical record), server-owned verification status, and durable actor binding for this check. Existing historical audit emails alone are not immutable identity proof.
3. **S3:** role-request approver grant, request/inbox ownership, period-request visibility and processing authority; current read baseline exposes broad request/email-preview data. C1 defines the proposed direction, not a silently applied permission change.
4. **C1–C5:** approve/coordinate new shared request, localization, history, workbook and registry contracts; agree external email domains/tax-number rule and populated-data migration before changing consumers.

Independently implementable first batch: safe operational logging and error redaction, HTML encoding/canonical mail links, backend image entrypoint alignment and compatible validation fixes. No permission grant, shared DTO or schema needs to change for these. Remaining functionality follows the approved contracts, then migrations, then real-path tests, then measured performance and environment acceptance. External bank/realm/OCP verification stays BLOCKED until real inputs are supplied.

### Concrete S2/S3 proposal for approval (not implemented)

**S2: four-eyes and server-owned verification status.**

- New legal/physical records and imports store a server-populated creator subject, separate from the existing display/email audit fields. The value comes from the validated identity, never a DTO, submitted local user ID or an email comparison. Keep it immutable through updates. Add nullable subject columns through an additive migration; do not drop or reinterpret historical fields.
- Reject verification by the same creator subject with403 and a safe business message. Other authorized verifiers keep the existing application permission and period-lock checks. Verified status can change only through the verification operation; legacy Update.Status may repeat the current status for compatibility but a submitted transition is rejected with400. A normal edit must not become a status-grant bypass.
- Historical records without proven creator subject cannot safely pass four-eyes by comparing today's email. Proposed default:409 requiring creator-identity reconciliation; no unlogged administrator override. Apply an approved per-record historical mapping only after evidence/backup review. This can temporarily block verification of old records and needs explicit acceptance before deployment.
- Commit verification mutation and its audit atomically; rejected attempts create no success audit. Test creator/noncreator with changed email/username, forged body fields, missing provenance, repeated verification, status-overposting and locked periods through the actual HTTP pipeline, plus a SQL upgrade retaining existing records.

**S3: request ownership, approver authority and notification privacy.**

| Operation | Proposed authority and scope | Failure behavior |
|---|---|---|
| Submit/request own role changes | Authenticated subject, verified usable email; virtual Guest may request without pre-existing AppUser | Missing trusted identity401/validation400; applicant identity ignored/rejected in body |
| View own role requests/inbox | Current authenticated subject owns record | Cross-owner ID404; no recipient/body disclosure |
| Review/approve/reject role requests | Active application Admin (new stable permission seeded only to Admin) | Nonapprover403; self-approval403; stale/incompatible replay409 |
| Period request creation/list/response | Existing write permission for submission retained; owner-only view/response; Admin additionally reviews all | Cross-owner404; response only to NEEDS_INFO, otherwise409 |
| Period lock/unlock/reject/request-more-info | Proposed Admin-only processing, replacing current broad CapitalWrite processing where applicable | Capital without Admin403; this is a policy change, not a bug fix to apply silently |
| Notification/email preview | Recipient-scoped inbox; raw global HTML email preview restricted to isolated Development test/demo; Admin production delivery metadata excludes message body/recipient detail unless separately approved | Nonowner404; no broad CapitalRead access to other recipients' messages |

Approval is one conditional transition with a request version; role replacement, final-Admin check and audit share one transaction. Competing approve/reject produces one winner and409 for the other. An identical successful idempotency-key replay returns the prior result without second mutation/mail. Do not rebind a confirmed subject or transfer privileges to a recreated Keycloak account by email. Role-request approval must use the same explicit subject-link procedure as C1. Delivery records are committed atomically; retry only pending mail deliveries, with deduplication and bounded attempts. This needs the additive C1 contracts/schema and frontend coordination; it is not covered by S1 approval.

Required S3 HTTP/SQL tests: anonymous401; Guest/functional-role/Admin matrix; owner/nonowner IDs; forged applicant/approver; inactive/ambiguous identities; self-approval; duplicated/racing decisions; final-Admin protection on change requests; no audit/role mutation on failure; notification isolation; failed mail without duplicate grants. Existing Capital period-processing behavior remains unchanged until this proposal is approved.

## Implementation and fresh verification

The first backend implementation batch is complete locally; overall backend finalization is **PARTIAL**, pending S2/S3 and shared-contract decisions plus external dependencies. S1 was explicitly approved by the user during this task. Baseline matrix statuses above remain historical; the following records the implemented deltas.

| Area | Delivered delta | Remaining limit |
|---|---|---|
| AUTH-08/S1 | Configured API Audience validated everywhere; missing audience fails startup outside Development; unconfigured Development exception preserved. OIDC code+PKCE/client identity retained. | Real realm audience mapper, browser OIDC and UAT/PROD tokens require environment verification. |
| OBS-01/ERR-01 | Removed raw console JWT/identity logs; route-template/status/elapsed/trace diagnostics include auth short-circuits; exception fallback no longer emits messages/inner exceptions/stack traces. Correlation IDs bounded and malformed/multiple headers replaced; response includes correlation even on handled500. Mail and legal operational logs exclude recipient/subject/actor/business-name payloads. | Broader telemetry/retention, framework/dependency configuration and S3 notification privacy remain open. |
| NOT-03/ERR-01 | Eight mail templates HTML-encode dynamic values and use existing canonical /app routes; period error strings use a proper JSON writer for all32 control characters, preserving Unicode. | Durable recipient inbox/outbox, ownership and delivery semantics await C1/S3. |
| OPS-01/DB-03 | Backend image runs actual DATA_CORE.dll. Startup migration is off by default, explicit Development opt-in only, relational-provider guarded; no production migration at startup. Warning no longer claims nonexistent seeded data. | Duplicate frontend/backend assembly identity prevents BrowserHost build; coordinate with frontend owner. Real image/OCP acceptance not performed. |
| LEG-01/XLSX-02 | Shared service validation enforces name100, description500, basis100 and optional numeric50 bounds through create/update/import; non-ASCII numeric input rejected safely. Missing-name/nonempty rows counted as failures. Corrupt legal/physical XLSX returns safe file validation; row parser internals not exposed. Validated rows retain existing batch/partial-success model. | Versioned round-trip workbook/metadata, ZIP expansion limits, physical complete field parity, inactive/concurrent duplicate handling, tax-rule decision and bank mapping remain PARTIAL. Exact13+checksum tax behavior is preserved. |
| CODE-01 | Duplicate key precheck normalizes case/whitespace and includes inactive values; Update deactivation obeys Delete reference guard; service field lengths match persisted limits. | Concurrent-create SQL uniqueness, definition FK/system protection/versioned cache need reviewed constraints/contracts. |

No shared frontend DTO, frontend source, frontend localization, database entity/model or existing migration was edited. No new migration was created. No production data was read or modified. The tracked Backend project .user file is unchanged (only IDE debug-profile metadata); it is a repository-hygiene finding, not a secret leak. PKCS remains9.0.21 and no NuGet vulnerability/feed warnings were suppressed.

### Commands and actual results

Run from the dedicated backend-finalization checkout, in Release with no restore after the initial solution restore:

| Check | Exact command or scope | Result |
|---|---|---|
| Restore | `dotnet restore RelatedPartiesRegister.slnx --disable-parallel -m:1` | PASS |
| Backend build/analyzers | `dotnet build Backend/RelatedPartiesRegister.csproj -c Release --no-restore -p:RunAnalyzers=true -m:1 -nr:false` | PASS,0 warnings,0 errors |
| Full backend unit | `dotnet test tests/Backend.UnitTests/UnitTests.csproj -c Release --no-restore -m:1 -nr:false --logger trx --results-directory artifacts/backend-finalization/final-tests` | **310/310 PASS**,0 skipped |
| Full backend integration | `dotnet test tests/Backend.IntegrationTests/IntegrationTests.csproj -c Release --no-restore -m:1 -nr:false --logger trx --results-directory artifacts/backend-finalization/final-tests` with `RBBH_TEST_SQLSERVER=Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true` | **219/219 PASS**,0 skipped, including8 actual SQL tests |
| S1 configuration regression | `--filter FullyQualifiedName~JwtAudienceConfigurationTests` | 11/11 PASS, including all9 prior failures |
| S1 real bearer HTTP | `--filter FullyQualifiedName~JwtAudienceHttpTests` | 9/9 PASS: exact/multiple audiences, wrong, absent, empty, whitespace, numeric and empty-array cases; OIDC configuration assertions retained |
| Safe diagnostics HTTP | `--filter FullyQualifiedName~SafeDiagnosticsHttpTests` | 7/7 PASS |
| Correlation middleware | `--filter FullyQualifiedName~IncludeCorrelationIDMiddlewareTests` | 9/9 PASS |
| Encoding + existing period response + safe diagnostics | `--filter 'FullyQualifiedName~BusinessMessageEncodingTests\|FullyQualifiedName~UnlockResponseHttpTests\|FullyQualifiedName~SafeDiagnosticsHttpTests'` | 59/59 PASS |
| Import field/parser/tax and physical tests | `--filter 'FullyQualifiedName~PhysicalImportValidationTests\|FullyQualifiedName~LegalImportValidationTests\|FullyQualifiedName~TaxNumberValidatorTests'` | 26/26 PASS |
| Legal validation HTTP | `--filter FullyQualifiedName~LegalEntityHttpValidationTests` | 4/4 PASS, including malformed file400/no persistence |
| Codebook service | `--filter FullyQualifiedName~CodeListServiceTests` | 25/25 PASS |
| Email operational diagnostics | `--filter FullyQualifiedName~EmailDiagnosticsTests` | 4/4 PASS |
| Release publish | `dotnet publish Backend/RelatedPartiesRegister.csproj -c Release --no-restore -p:RunAnalyzers=true -m:1 -nr:false -o artifacts/backend-finalization/publish` | PASS |
| Published startup | DATA_CORE.dll in isolated Development/InMemory/demo-mail mode; ApplyMigrations=false and true tested separately | PASS both; /health/live and /health/ready each200/Healthy; owned smoke processes stopped afterwards |
| Whole solution build | `dotnet build RelatedPartiesRegister.slnx -c Release --no-restore -p:RunAnalyzers=true -m:1 -nr:false` | FAIL with the same3 baseline BrowserHost namespace/reference errors; no claim of full .NET/browser success |
| Whitespace | `git diff --check` and `git diff --check origin/main...HEAD` | PASS |

Backend total: **529/529**, no skips. Focused results are subsets, not additional tests to add to529. The8 SQL cases cover actual SQL provider use, mutual Admin removal, demotion/deactivation, deactivate/delete, released locks, unconfirmed placeholder exclusion, subject-link migration/persistence/audit and readiness. These tests use uniquely named disposable databases, never the application database.

The initial published startup attempt correctly failed missing real email configuration. Successful smoke probes explicitly selected `Email:Provider=demo`; they do not prove real email or SSO works. The pre-fix InMemory startup failed even with ApplyMigrations=false; the post-fix two-mode probe confirms that regression is removed.

Startup command from the publish directory (random free port, no additional saved script):

```powershell
dotnet DATA_CORE.dll --environment Development --urls http://127.0.0.1:0 --Email:Provider demo --KeycloakSettings:Enabled false --Database:ServerName "" --Database:Name "" --Database:ApplyMigrations false
```

Repeating with `--Database:ApplyMigrations true` must warn/skip migration for InMemory. Real SQL migrations remain an explicit reviewed deployment action; no migration command against a populated database was executed here.

### Commits and handoff

Local implementation commits (before this evidence update):

- `4367061`: requirement audit and proposed shared contracts.
- `2bb9b9a`: safe request/auth/exception diagnostics and correlation IDs.
- `292acba`: approved S1 Audience validation and HTTP evidence/configuration.
- `42a13d5`: mail HTML encoding, canonical routes and safe period JSON.
- `e491a6e`: startup migration opt-in and actual backend entrypoint.
- `9275326`: import field bounds, incomplete rows and malformed workbooks.
- `99ccca1`: stable codebook keys and referenced-deactivation guard.
- `872ddaa`: mail/business operational log redaction.

Migration instructions for this batch: no schema upgrade/backfill is needed. Configure the approved API audience before UAT/PROD startup. Default startup does not run migrations; opt in only in isolated Development when intentionally applying reviewed relational migrations. Keep first-link fallback disabled until real export/identity reconciliation is approved. Do not run historical destructive migrations on populated data on the strength of fresh-database tests.

Frontend handoff: current HTTP DTO/route shapes remain compatible; no new role-request/history/localization routes are available yet. Coordinate the duplicate DATA_CORE assembly identity and approve C1–C5 before frontend implements proposed contracts. Error responses no longer expose internal exception details; correlation header is additive. Permission sets and Guest semantics are unchanged except for the explicitly approved S1 token audience enforcement.

Release is not ready: S2/S3 security findings, populated-data migration decisions, bank finance/GCC contracts,8h shared session policy and real Keycloak/OCP/3scale/SQL/mail/TLS/proxy/cache/DP-key/retention/backup operations remain open. No credentials or infrastructure values were invented. No push was performed.

### Exact changed files


- `Backend/API/Controllers/PeriodLockController.cs`
- `Backend/BL/Services/CodeListService.cs`
- `Backend/BL/Services/EmailTemplates.cs`
- `Backend/BL/Services/HttpApiEmailService.cs`
- `Backend/BL/Services/LegalEntityService.cs`
- `Backend/BL/Services/MockEmailService.cs`
- `Backend/BL/Services/RelatedPersonService.cs`
- `Backend/BL/Services/SmtpEmailService.cs`
- `Backend/Dockerfile`
- `Backend/Exceptions/GlobalExceptionHandler.cs`
- `Backend/Helpers/Excel/ExcelImportSchema.cs`
- `Backend/Helpers/Validators/TaxNumberValidator.cs`
- `Backend/IoC/Extensions/Authentication/AuthenticationServiceExtensions.cs`
- `Backend/IoC/Extensions/Databases/DatabaseExtensions.cs`
- `Backend/IoC/Extensions/ExceptionHandling/ProblemDetailsConfig.cs`
- `Backend/IoC/Middleware/IncludeCorrelationIDMiddleware.cs`
- `Backend/IoC/Middleware/RequestOutcomeLoggingMiddleware.cs`
- `Backend/Program.cs`
- `Backend/appsettings.json`
- `docs/BACKEND-FINALIZATION.md`
- `docs/configuration/ENVIRONMENT-VARIABLES.md`
- `tests/Backend.IntegrationTests/Tests/LegalEntity/LegalEntityHttpValidationTests.cs`
- `tests/Backend.IntegrationTests/Tests/Operations/BusinessMessageEncodingTests.cs`
- `tests/Backend.IntegrationTests/Tests/Operations/JwtAudienceHttpTests.cs`
- `tests/Backend.IntegrationTests/Tests/Operations/SafeDiagnosticsHttpTests.cs`
- `tests/Backend.UnitTests/Helpers/Validators/TaxNumberValidatorTests.cs`
- `tests/Backend.UnitTests/Middleware/IncludeCorrelationIDMiddlewareTests.cs`
- `tests/Backend.UnitTests/RelatedPersons/PhysicalImportValidationTests.cs`
- `tests/Backend.UnitTests/Services/CodeListServiceTests/CodeListServiceTests.cs`
- `tests/Backend.UnitTests/Services/EmailDiagnosticsTests.cs`
- `tests/Backend.UnitTests/Services/LegalEntityServiceTests/LegalImportValidationTests.cs`

## Continued implementation after 05b63b4 (approved S2/S3, 2026-11-02)

The user approved S2/S3 and additive C1–C5 implementation. Preserve Capital period-processing permissions; the earlier Admin-only period-processing proposal is superseded and is **not applied**. New contract handoff: [BACKEND-FRONTEND-CONTRACT-V1.md](BACKEND-FRONTEND-CONTRACT-V1.md).

- `7f2585d` (SAFE-01/S2): server-derived immutable CreatorSubject, additive nullable provenance and SQL rowversion on legal/physical records; creator403, ownerless historical409; legal Update cannot transition status; physical overposting ignored; verification+audit saved atomically.310/310 unit PASS;23/23 affected HTTP/SQL/period/subject-link tests PASS, including real SQL racing verification and audit-trigger rollback. No historical provenance invented.
- `5735ba0` (PER-03/S3 initial): owner-only period-request list; additive CapitalWrite `/review` queue; response subject/state checks; one-response parent key and rowversion; response/audit atomic; production raw-mail preview disabled and isolated Development preview recipient-filtered.310/310 unit PASS;34/34 affected HTTP/SQL/period tests PASS. Original NEEDS_INFO→new PENDING child workflow retained, including the original record status; duplicate child blocked409. Existing Capital processing grants preserved.

Additive migrations `AddRegistryCreatorProvenance` and `AddPeriodRequestOwnership` are local source changes, tested only on disposable SQL databases. Apply before upgraded API rollout; leave historical creator/requester subjects null until an approved reconciliation. Never use current email to invent historical ownership. Generated Down operations remove the new fields: do not run a data-losing downgrade in shared environments without explicit approval/backup; application rollback can retain additive columns.

These batches do not complete role-request/inbox/localization/history/registry/reporting scope. Continue the dependency-ordered backlog; final statuses must be VERIFIED / BLOCKED / NOT_IMPLEMENTED after complete backend regression.


## Continued C1/C2/C5 implementation, 2026-11-03

This section supersedes pending implementation descriptions from the historical baseline above. It does not declare the overall backend finished.

- C1/AUTH-04/USR/NOT-01: `6ababe7`, `2f6da19` implement database-owned multi-role request/change/approval/rejection, trusted subject/verified email, virtual Guest, owner-scoped inbox, atomic audit/grants/outbox and idempotent replay. SQL application-lock boundary preserves final active Admin access; own decisions denied. HTTP/SQL approval races, rollback and concurrent delivery are covered. No provider role grants are trusted. Mail retries never repeat grants; exactly-once external email is not claimed.
- C2: `b27a6d4` enforces an absolute8h application cookie cap without sliding, honors earlier token expiry, and provides CSRF-protected POST logout with an additive antiforgery token endpoint.19/19 focused session+JWT HTTP cases PASS. Existing GET logout is explicitly deprecated but retained until frontend adopts POST; end-to-end Keycloak/Blazor refresh/logout is still BLOCKED on coordinated browser/realm acceptance.
- C1 user management/DB-01/PERF-01: `96cca27` adds profile edits, normalized uniqueness with duplicate/orphan preflight and UserRoles FK, transactional create/reactivation/profile audit, exact read-only Keycloak identity adapter, opaque subjects and SQL-first pagination.317/317 unit PASS;54/54 user HTTP checks and17/17 migration/admin/SQL subset PASS (subsets are not cumulative totals). Populated duplicate preflight preserves source rows. Missing real Keycloak service-account access remains external; no email-based takeover/backfill is performed.
- C5/CODE-01/02/03/LOC-01: runtime languages/packs reside in the existing codebook area. Configurable names; default migration adds BS/EN/DE and safe initial error/notification labels without overwriting historical rows. Semantic new keys, legacy case compatibility, per-field fallback, bounded missing-key diagnostics, SQL versioned cross-request/cache freshness, administrator-only mutation, recipient email template resolution and safe exception localization are implemented. Generic label-only Excel export/import is compatible with the established one-column contract, bounded before ZIP expansion, batches valid rows+audit atomically and reports collisions. Definition metadata updates use expectedVersion. Required/default resources and in-use values cannot be removed through alternate CRUD paths.

C5 deployment: apply additive `AddRuntimeLocalizationCodebooks` before upgraded API. Its first step rejects normalized category/code duplicates without deleting/merging rows. The optional DefinitionId FK leaves historical records unassigned rather than inventing associations. Default seeds are additive and do not reactivate/replace pre-existing data. Custom codebook names must be provisioned alongside the application's other codebooks through its normal administration flow. Generic import/export is a label workflow, not a backup/restore of runtime resource or audit metadata. Retain additive schema for application rollback; destructive Down/removal of new runtime resources is not authorized for shared data. All SQL tests use disposable GUID databases, never shared UAT/PROD.

Remaining independent batches: C3 registry/GCC/roundtrip workbooks, C4 capital/limits, reporting/period atomic delivery, remaining audit/operations/performance gaps and final full backend regression. External bank/finance/SSO/deployment inputs block only their own acceptance. No frontend/browser files changed and no push performed.

C5 gate: backend analyzer build PASS (0 warnings/errors); full backend unit321/321 PASS; affected localization/codebook/role-notification HTTP+SQL40/40 PASS with no skips, including5 new localization SQL cases, populated preflight, audit-trigger rollback, cross-scope cache versions and concurrent normalized-key creation. Migration model check reports no pending changes; git diff --check PASS. Focused counts overlap other suites and are not added to a full backend total. Full integration regression follows after the remaining implementation batches.
