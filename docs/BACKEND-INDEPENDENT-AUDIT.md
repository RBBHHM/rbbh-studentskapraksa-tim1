# Independent backend audit, hardening and production readiness

Date: 2026-11-03. Branch: `feat/codex-backend-finalization`. Input HEAD `22f0376ccb6cc79d4d79ada1c967dc7d776d6465`. This independent review supersedes blanket completion interpretations, not historical checkpoint results. The final delivery message records subsequent fix/documentation SHAs. No production/shared data was accessed or changed; no push/merge/history rewrite or integration-worktree modification was performed.

## 1. Executive assessment

**Local backend regression is green; production approval is BLOCKED.** Two reproducible HTTP/SQL audit-query failures are corrected with one minimal boundary-hardening patch and three regressions. Routes, DTOs, authorization and schema are unchanged. Remaining security, legacy-data, operational and consumer gaps are explicitly ranked below; passing tests are not proof of complete requirements coverage.

Most urgent: this backend branch still tracks a nonempty frontend Authentication:ClientSecret; cleanup already belongs to the frontend owner and historical exposure still requires issuer-owner rotation/revocation confirmation. Production configuration cannot be certified secret-free. Cookie-authenticated API mutation CSRF/legacy GET logout needs coordinated security review. Real bank/finance/identity/cluster acceptance is unavailable, not simulated.

## 2. Scope and architecture

Inventory:380 tracked backend/test/CI/database-script/deployment files, 67,685 lines including generated migration designers and tests, hashed in ignored `artifacts/backend-independent-audit/source-ledger.json`. This is an inventory, not a line-coverage percentage or a claim of formal verification. Review traversed all runtime areas/controllers/policies/DTOs, model/configuration and migration Up/Down markers, SQL/transaction/HTTP lifetimes, logging/localization, workbooks/reports, workers, Docker/CI and existing regression families. Generated designer/snapshot files are schema history, not refactoring targets.

Requirements inputs: original20-page PDF and POPRAVKE.txt, prior full extraction/image review, BACKEND-FINALIZATION.md, source-checked final contract and current runtime source. No new audit copy of sensitive configuration values was produced.

Structure remains a pragmatic single API project with API/BL/DL and scoped EF context. Existing result wrappers plus business exceptions cause heterogeneous errors; the [final contract](BACKEND-FRONTEND-CONTRACT-FINAL.md) records actual shapes. Replacing them mid-integration would break clients. Large RelatedPersonService/CodeListController/ReportService are maintenance hotspots; no stylistic class-splitting was done. SQL retry/transaction boundaries are substantive, not interchangeable with process locks.

DI: EF, business services, runtime catalog/filter/dispatcher scoped; bounded localization cache/email preview singleton; hosted services obtain scopes. No singleton DbContext capture found. Typed Keycloak client is not captured by a singleton. Named JsonPlaceholder/Temenos registrations and transaction-code helper are inherited scaffolding with no reviewed production caller; preserve rather than remove public/tested symbols opportunistically. Old repository mutation methods remain registered but current period controller/scheduler call PeriodWorkflow; do not accidentally switch back to non-workflow writes.

## 3. Security findings and endpoint review

The108-operation inventory in the final contract was reconciled with actual controller/minimal endpoint policies. Default application access requires authenticated effective gost. Reads use module grants; privileged writes use DB permission codes. Guest can submit its own approved role requests and mark its own notifications read; that is deliberately not blanket read-only storage access. Guest cannot mutate registry/users/capital/limits/report generation. Health is public aggregate status; metrics requires administration.read. Swagger is non-Production only. Operational exceptions are intentional.

Confirmed local controls: signed/issuer/audience/expiry checks; missing Audience/auth fails outside Development; incoming provider grants and forged application markers removed; DB subject resolution and BIN2 owner keys; unknown virtualGuest/inactive denial; SQL application-lock final-Admin/self-management guards; role-request owner/version/idempotency/transactional decision; four-eyes immutable creator; owner-scoped inbox/period response; locked registry/capital/verification route matrix; safe errors and route-template logging. Cases are mapped to actual tests below. No switch to four direct Keycloak roles or Admin-only period processing occurred.

| ID | Severity / state | Evidence and next owner/action |
| --- | --- | --- |
| SEC-01 | P0 BLOCKED | Redacted tracked-file scan found Frontend/appsettings.json Authentication:ClientSecret nonempty in this branch. Backend tracked config scan found no nonempty credential key candidate. Frontend owner must preserve its removal on integration; realm/security owner must rotate/revoke and confirm use sites. Do not reuse/expose the value, rewrite history or assume deletion revokes it. |
| SEC-02 | P1 PARTIAL | AuthenticationServiceExtensions selector accepts Cookies without Bearer. Backend antiforgery validates POST logout only; ordinary unsafe MVC actions have no global antiforgery enforcement. SameSite=Lax/CORS mitigate some cross-site cases, not a universal CSRF guarantee (especially trusted/same-site origins). Coordinate bearer-only API versus cookie+antiforgery policy and frontend token/cookie-origin contract before alteration. Source-confirmed missing enforcement, not a claimed live browser exploit. Reproduce review with a genuine backend cookie, unsafe action and absent CSRF token; do not weaken tests. Security/backend/frontend ownership. |
| SEC-03 | P1 PARTIAL | GET /authentication/logout still signs out without CSRF, intentionally deprecated for compatibility. POST protection is tested. Frontend must adopt the correct origin's logout contract; retire GET in a coordinated security change. |
| SEC-04 | P1 BLOCKED | No AddRateLimiter/UseRateLimiter or per-principal request/concurrency budget is implemented. Uploads/imports and reports have per-request bounds, not global admission control. Approve ingress/3scale per-user limits, burst/concurrency and429 contract, including authenticated expensive exports and connection budgets. No arbitrary local throttling policy introduced during integration. |
| SEC-05 | P2 NOT TESTED | Real realm audience/refresh/revocation/email_verified and service-account least privilege not exercised. JWT/metadata fixtures are local proofs only. Require actual realm export/token and logout acceptance with secrets supplied externally. |

SEC-06 — P1 NOT TESTED, backend/security owner: the sentinel log tests cover application outcome/global/auth categories, not all EF/provider diagnostic categories. EF SaveChanges/command/query-failure diagnostics can carry provider exceptions; SensitiveDataLogging=false alone is not a proof that a duplicate-key provider message cannot contain submitted values. Before UAT, capture a real SQL constraint failure through the configured logging pipeline and sanitize provider diagnostics if reproduced. This audit does not claim a confirmed leak or a completed all-provider log-redaction proof.

An in-memory comparison confirmed that the current frontend credential matches the value immediately before the known frontend removal commit cf2f745; it is not an obvious placeholder. Neither value nor fingerprint was printed or written to the audit artifacts. Current validity/revocation is unverified.

Secret scanning was a redacted JSON credential-key/private-key/token-pattern review, not a full-history gitleaks certification (gitleaks unavailable). Current values are never printed. Historical exposure is already confirmed by the project; owner rotation and a clean merged-source/package scan remain mandatory.

## 4. Database integrity and migrations

Reviewed56 migration C# files (including designers/snapshot); no migration was changed or proposed in this batch. Existing additive Nov migrations preserve normalized role relationships, nullable historical provenance, resource translations and persisted snapshots. Computed normalized uniqueness/FKs/preflight reject duplicates/orphans; opaque subject indexes use BIN2 while user-facing searches intentionally normalize/collate. Capital revisions have EF guard plus SQL append-only trigger. Period rowversion, role-request version, unique pending/request/response keys and user SQL application locks are used rather than trusting a disabled UI action.

SQL user writes/audit share the mutation-boundary transaction even when AuditService saves separately. Other approved workflows stage state/audit/inbox/outbox together; limits generated IDs/audit use retry-aware transaction and commit-ack evidence. Read paths use AsNoTracking where appropriate. InMemory cannot prove rollback/concurrency semantics and is not the production store. Relational proofs use only owned GUID LocalDB databases.

| Legacy migration (Up) | Risk / required approval |
| --- | --- |
| Backend/Migrations/20260820133719_RemoveCodeListInUseFlag.cs | Drops UUpotrebi; DBA must reconcile existing use/reference semantics. |
| Backend/Migrations/20260911102450_RemoveFamilyRelationshipAndGccFields.cs | Drops historical relationship/GCC columns; re-added nullable GCC does not recover deleted data. |
| Backend/Migrations/20260928123241_RedesignLimitiClientLinkage.cs | Drops limit/capital columns and adds a required FK with empty-GUID default; nonempty old tables can fail or lose data. |

These historical migrations are **not safe to run blindly on an unknown production baseline**. Empty/disposable upgrade success is not a populated legacy-data backfill proof. Four historical docs/database/changes scripts also depend on their original schema/history, including synthetic historic migration labels; treat them as historical procedures, not a fresh production runbook. DBA must inventory applied history/schema, export/retain old fields, reconcile valid FK mappings, prove backup restore and rehearse a masked real-shaped copy. Never mark history applied or rewrite files to bypass a failure.

Deployment order for existing additive migrations is in [the integration handoff](INTEGRATION-HANDOFF-CODEX.md). No new migration requires deployment here. Existing deployments need explicit DBA approval after duplicate/orphan/preflight/history review; retain additive data on app rollback. Down methods can drop tables/columns and are not a safe data-preserving rollback. Forward repair or backup restore requires separate approved design. Historical creators/owners/sub links/capital are not inferred locally.

## 5. Performance and resource findings

Measured local production-option paging test uses2,500 synthetic users/5,000 assignments. SQL commands are bounded for page sizes 1/100; fresh measured output is recorded in evidence below. It is not a production SLO, network benchmark or before/after speedup. Audit extreme-page failure is proven and fixed; query uses int64 saturating offset and retains page metadata. No index was added without a representative plan/workload.

Confirmed existing bounds: workbook10MiB/5,000 rows, ZIP2,000 entries/32MiB entry/64MiB expansion, formulas/macros/external-link paths rejected; registry exports reject>5,000; report snapshot build20,000 and monthly source20,000 keys in500-key batches; client-request role/note and resource-key/label budgets; versioned localization cache50,000 size units/pack10,000 entries; outbox50-per-batch/two-minute lease/five-attempt budget; SQL pooling100 and command/connection timeouts. These are per-process/per-request limits, not a total multi-pod memory/connection guarantee.

| ID | Severity / status | Evidence / decision |
| --- | --- | --- |
| PERF-02 | P1 PARTIAL | RelatedPersonService.GetRelationshipTree reads all active people and recursively builds nodes with a copied HashSet per descendant. Deep chains imply depth-dependent stack and quadratic path-copy allocation. No production-scale depth/serialization limit is agreed or load-tested. Backend/frontend must coordinate bounded/flat/paged tree behavior; no silent topology truncation or arbitrary validation limit added. |
| PERF-03 | P1 PARTIAL | ReportService.GetLimitExportRowsAsync materializes all matching limits for current client/all-client exports without a Take bound; generated snapshots and registry exports have different limits. Approve export admission/streaming/background or explicit maximum/error contract with frontend before restricting an existing download. Resource risk is source-confirmed; no crash/latency measurements invented. |
| PERF-04 | P2 PARTIAL | Legacy physical GetAll/detailed/family lists remain unpaged for compatibility; use additive /page in frontend. Case-normalized Contains/UPPER and finance CONVERT key queries can scan; no new index claim without real row counts/plans. Real bank views/native key types require source-owner review. |
| PERF-05 | P2 PARTIAL | Period broadcast stages every active confirmed user plus inbox/outbox in one transaction (correct atomicity, unbounded fan-out). Rehearse real cardinality/memory/log capacity and rollout/retention limits. Chunking must not silently weaken state/audit/recipient atomicity. |
| PERF-06 | P2 NOT TESTED | Full concurrent-user memory/GC, real SQL/bank latency, optimizer plans, metrics cardinality and production cancellation latency were not measured. Existing index/AsNoTracking/batching and bounded paging tests do not prove scale. |

## 6. Production/deployment assessment

Backend Dockerfile builds only Backend from root context, excludes local secrets/artifacts/tests, uses vendor .NET10 runtime, non-root APP_UID, port8080 and actual DATA_CORE.dll. Local build/publish/startup does not establish that vendor-registry image/CA can be pulled. No Docker/oc/actionlint executable is available in this session; actual image build, workflow execution and OCP startup remain NOT TESTED. Deploy workflow validates input, pins final digest, uses environment approvals/job-scoped KUBECONFIG/cleanup, trusted CA, config/secret references, no privilege escalation and explicit probe Host. Supply-chain provenance and base-image digest/patch policy still need the image owner.

Outside Development, SQL and fully configured auth/audience/known proxies/hosts and real mail fail closed. Forwarded peers/networks must be actual bounded infra values; headers from arbitrary clients are not trusted. CORS grants only configured origins, credentials allowed for those origins; no wildcard fallback. HTTPS HSTS/security headers exist but TLS termination/cert chains must be validated on the real route. Database connection encrypts and only explicit Development permits trust-server-certificate; review runtime environment inputs, do not emulate production by weakening trust.

Readiness checks SQL connectivity and pending migrations without DDL; status probes expose no exceptions. It does **not** prove realm, mail, cache, finance or worker freshness all healthy. Worker backlog/scheduler metrics exist and are protected through metrics authorization; scrape credentials/alerts/retention/failover are external. Scheduler uses host-local08:00 and UTC period date/weekdays; timezone/holidays/HA-overlap authority is not a bank-approved calendar merely because TZ is set. Period rowversion/idempotent lock limits duplicate mutation, not a complete leader/drain policy.

Production blockers: shared session/cache provider, persisted protected DataProtection keys and app-name/rotation ownership, actual proxy/hosts/CA/3scale/CORS, resource requests/limits, termination/drain/ShutdownTimeout budget and worker cancellation, cluster/SQL/mail/realm validation, backup/restore/retention/audit privacy, scheduler ownership/overlap, live dependency/security-feed confirmation and credential rotation. Deployment YAML currently supplies no resource requests/limits or persisted DP volume; no fictional values were added. These remain approvals/prerequisites, not local fixes.

Dependency check: PKCS9.0.21 retained; no NU1900/NU1902/NU1903 suppression. Fresh `dotnet list Backend/RelatedPartiesRegister.csproj package --vulnerable --include-transitive --no-restore --format json` exited 0 and reported no vulnerable package entries against its listed NuGet source. This is the exact tool result, not proof of a fresh authenticated corporate restore, vulnerability-free base image or current production inventory. Live corporate feed/runtime/image/SBOM approval remains external. No speculative package upgrade was made.

## 7. External integration contracts and acceptance

| Integration | Adapter/config/auth and request/response | Bounds/retry/failure/logging | Local coverage / external state |
| --- | --- | --- | --- |
| Keycloak browser/bearer | AuthenticationServiceExtensions; issuer/publicIssuer/client/secret/API Audience; code+PKCE, signed JWT and issuer/exp checks; profile is DB-derived | Metadata refresh10min/retry10s, framework HTTP pipeline; absolute app cookie8h/token expiry earlier; safe failure category/trace, no token logging | JwtAudienceConfigurationTests/JwtAudienceHttpTests/AbsoluteSessionHttpTests/MetadataTransportTests. BLOCKED actual realm/email_verified/SSO/refresh/revocation/redirect/logout/cert validation. |
| Keycloak administrative lookup | KeycloakAdminService; AdminClientId/secret independently selected, compatibility client fallback; client_credentials then exact email user lookup max2; id/username/names/email/enabled response; no realm-role writes |10s HTTP,1MiB response buffer, cancellation; no custom automatic operation retry;503 unavailable/409 ambiguity; sanitized failure type | KeycloakIdentityLookupTests and user HTTP/SQL tests. BLOCKED actual read-only service-account privileges, address/realm data and rotation. |
| Bank customer/GCC | SqlBankReferenceSource; BankReference Enabled/connection and validated table/column identifiers; configured SELECT-only SQL credentials; parameterized search/paging, customer tax/matbroj/name or group number/name |5s connect/10s command,max100/search100,cancellation; no custom retry/fake fallback;503 source/400 unknown/409 ambiguity; sanitized failure category | BankReferenceSqlTests/RegistryGccHttpTests. BLOCKED actual kod/glava source schema/access/types/unique keys/samples. |
| Monthly Finrep | SqlFinanceMonthlySource; FinanceMonthly enabled/connection/approved YYYY-MM tables/customer key/BAM/components; parameterized SELECT-only batches, keyed balance/offbalance response |5s connect/15s command,500-key batches,20k bound; no custom retry/local fallback on configured failure; rejects duplicate/null/currency/precision and incomplete data409/503; safe category | FinanceMonthlySourceSqlTests/ReportFinalization tests. BLOCKED actual spreadsheet reconciliation and credentials; daily authoritative mapping missing. |
| HTTPS mail API | HttpApiEmailService/EmailSettings endpoint/sender/appURL/AdminEmail; JSON From/To/Subject/Text. Existing adapter defines no explicit auth header/token mechanism; provider auth must be confirmed | IHttpClientFactory default timeout100s unless externally overridden; strict outbox sends throw on failure; dispatcher lease/retry budget; no sensitive bodies/addresses in logs | EmailDiagnosticsTests and RoleRequestWorkflowSqlTests/PeriodAtomicSqlTests. BLOCKED actual provider authentication/response/delivery and timeout-vs-lease rehearsal. No assumption that bare HTTPS is sufficient authorization. |
| SMTP | SmtpEmailService/EmailSettings.Smtp host/port/TLS/user/password/sender; encoded HTML message | async cancellation token; no explicit per-send hard deadline configured by adapter; worker cancellation not a delivery receipt; failures retried by outbox, safe category | No live SMTP test. BLOCKED actual TLS/certificate/auth/network and cancellation/drain checks. |
| GitHub release notes | No backend adapter/DTO/controller/config exists | Timeout/retry/auth/error contract absent, no token exposed | MISSING, not VERIFIED. Requires approved repo/environment/tag selection and frontend contract. |

Mail delivery is at-least-once with an attempt budget, **not exactly-once**. SQL lease claims prevent simultaneous claims during a valid lease; a worker crash/provider acceptance/lease expiry or slow delivery can duplicate an email. An exhausted/last-attempt crashed row remains persisted for operator review, not silently deleted, but eventual successful delivery is not guaranteed. No public dead-letter requeue endpoint or live alert routing exists. No repeated grants are tied to provider success.

## 8. Requirements traceability

Status means the stated backend scope only. VERIFIED is local source+test evidence; PARTIAL is an implemented slice with a concrete remaining gap; BLOCKED requires real data/approval/environment; MISSING has no implementation; NOT TESTED has no adequate evidence. No tests listed below establish real SSO/bank/production acceptance. Test references are family evidence, not a claim that one class covers every sentence of the PDF. Alias B=Backend/BL/Services, C=Backend/API/Controllers, A=Backend/IoC/Extensions/Authentication. Test filename index after the matrix provides exact paths.

| Requirement | Status | Implementation / endpoint / authority | Unit evidence | HTTP/integration evidence | SQL evidence | Remaining gap |
| --- | --- | --- | --- | --- | --- | --- |
| AUTH-01 | VERIFIED | A/AuthenticationServiceExtensions, RoleClaimsTransformation; protected /api/*; DB module policies | ApplicationAuthorizationPolicyTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Local backend behavior only; external acceptance is separate. |
| AUTH-02 | VERIFIED | A/AuthenticationServiceExtensions, RoleClaimsTransformation; protected /api/*; DB module policies | RoleClaimsTransformationTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Local backend behavior only; external acceptance is separate. |
| AUTH-03 | VERIFIED | A/AuthenticationServiceExtensions, RoleClaimsTransformation; protected /api/*; DB module policies | SubjectLinkAndVirtualGuestTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Local backend behavior only; external acceptance is separate. |
| AUTH-04 | PARTIAL | A/AuthenticationServiceExtensions, RoleClaimsTransformation; protected /api/*; DB module policies | ApplicationAuthorizationPolicyTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Legacy ModulesController still uses a fixed advisory role map; DB grants/catalog work. |
| AUTH-05 | VERIFIED | A/AuthenticationServiceExtensions, RoleClaimsTransformation; protected /api/*; DB module policies | ApplicationAuthorizationPolicyTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Local backend behavior only; external acceptance is separate. |
| AUTH-06 | VERIFIED | A/AuthenticationServiceExtensions, RoleClaimsTransformation; protected /api/*; DB module policies | ApplicationAuthorizationPolicyTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Local backend behavior only; external acceptance is separate. |
| AUTH-07 | BLOCKED | A/AuthenticationServiceExtensions, RoleClaimsTransformation; protected /api/*; DB module policies | ApplicationAuthorizationPolicyTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Real realm/frontend8h refresh/logout; SEC-02/03. |
| AUTH-08 | VERIFIED | A/AuthenticationServiceExtensions, RoleClaimsTransformation; protected /api/*; DB module policies | JwtAudienceConfigurationTests | JwtAudienceHttpTests | Not applicable; signed bearer HTTP | Local backend behavior only; external acceptance is separate. |
| ID-01 | VERIFIED | A/ApplicationIdentityResolver, B/AppUserService; /api/auth/profile,/api/users; DB administration/subject | SubjectLinkAndVirtualGuestTests | UserProfilesHttpTests | SubjectLinkSqlServerTests | Local backend behavior only; external acceptance is separate. |
| ID-02 | BLOCKED | A/ApplicationIdentityResolver, B/AppUserService; /api/auth/profile,/api/users; DB administration/subject | SubjectLinkAndVirtualGuestTests | UserProfilesHttpTests | SubjectLinkSqlServerTests | Approved real subject-link/backfill/export; switch remainsfalse. |
| ID-03 | VERIFIED | A/ApplicationIdentityResolver, B/AppUserService; /api/auth/profile,/api/users; DB administration/subject | SubjectLinkAndVirtualGuestTests | UserProfilesHttpTests | UserProfilesSqlTests | Local backend behavior only; external acceptance is separate. |
| ID-04 | BLOCKED | A/ApplicationIdentityResolver, B/AppUserService; /api/auth/profile,/api/users; DB administration/subject | KeycloakIdentityLookupTests | UserProfilesHttpTests | SubjectLinkSqlServerTests | Live service-account privileges/realm identity contract. |
| ID-05 | VERIFIED | A/ApplicationIdentityResolver, B/AppUserService; /api/auth/profile,/api/users; DB administration/subject | SubjectLinkAndVirtualGuestTests | UserProfilesHttpTests | SubjectLinkSqlServerTests | Local backend behavior only; external acceptance is separate. |
| SAFE-01 | VERIFIED | B/AppUserService.Mutations, RegistryVerification; /api/users/*,/api/verification/*; admin/edit+four-eyes | AuthenticatedActorTests | UsersSafetyHttpTests | UsersLastAdminSqlServerTests | Local backend behavior only; external acceptance is separate. |
| SAFE-02 | VERIFIED | B/AppUserService.Mutations, RegistryVerification; /api/users/*,/api/verification/*; admin/edit+four-eyes | AuthenticatedActorTests | RegistryVerificationHttpTests | RegistryVerificationSqlTests | Local backend behavior only; external acceptance is separate. |
| DB-01 | VERIFIED | Backend/DL/Persistence and Backend/Migrations; no public migration endpoint; reviewed DBA deployment | ConnectionHelperTests | ProductionHostingHttpTests | PhysicalIdentityConstraintSqlTests | Local backend behavior only; external acceptance is separate. |
| DB-02 | BLOCKED | Backend/DL/Persistence and Backend/Migrations; no public migration endpoint; reviewed DBA deployment | ConnectionHelperTests | ProductionHostingHttpTests | PhysicalIdentityConstraintSqlTests | Populated legacy baseline/destructive historical migrations/backup restore. |
| DB-03 | VERIFIED | Backend/DL/Persistence and Backend/Migrations; no public migration endpoint; reviewed DBA deployment | ConnectionHelperTests | ProductionHostingHttpTests | PhysicalIdentityConstraintSqlTests | Local backend behavior only; external acceptance is separate. |
| LEG-01 | VERIFIED | B/LegalEntityService; /api/legal-entities,/api/bank-reference/*; legal read/create/edit/delete | CreateLegalEntityTests | LegalEntityHttpValidationTests | RegistryWriteSqlTests | Local backend behavior only; external acceptance is separate. |
| LEG-02 | VERIFIED | B/LegalEntityService; /api/legal-entities,/api/bank-reference/*; legal read/create/edit/delete | CreateLegalEntityTests | LegalEntityHttpValidationTests | RegistryWriteSqlTests | Local backend behavior only; external acceptance is separate. |
| LEG-03 | BLOCKED | B/LegalEntityService; /api/legal-entities,/api/bank-reference/*; legal read/create/edit/delete | CreateLegalEntityTests | LegalEntityHttpValidationTests | RegistryWriteSqlTests | Live bank source/read-only access/reconciliation. |
| LEG-04 | VERIFIED | B/LegalEntityService; /api/legal-entities,/api/bank-reference/*; legal read/create/edit/delete | CreateLegalEntityTests | LegalEntityHttpValidationTests | RegistryWriteSqlTests | Local backend behavior only; external acceptance is separate. |
| LEG-05 | VERIFIED | B/LegalEntityService; /api/legal-entities,/api/bank-reference/*; legal read/create/edit/delete | CreateLegalEntityTests | LegalEntityHttpValidationTests | RegistryWriteSqlTests | Local backend behavior only; external acceptance is separate. |
| XLSX-01 | VERIFIED | B/LegalEntityService,RelatedPersonService,LimitService; /import,/export; module create/read | WorkbookEnvelopeTests | RegistryWorkbookHttpTests | RegistryWriteSqlTests | Local backend behavior only; external acceptance is separate. |
| XLSX-02 | VERIFIED | B/LegalEntityService,RelatedPersonService,LimitService; /import,/export; module create/read | WorkbookEnvelopeTests | RegistryWorkbookHttpTests | RegistryWriteSqlTests | Local backend behavior only; external acceptance is separate. |
| PHY-01 | VERIFIED | B/RelatedPersonService, RelatedPersonValidator; /api/related-persons; physical module policies | UnitTests_FizickaLica_RelatedPersonValidator | RegistryReadAndChoicesHttpTests | PhysicalIdentityConstraintSqlTests | Local backend behavior only; external acceptance is separate. |
| PHY-02 | VERIFIED | B/RelatedPersonService, RelatedPersonValidator; /api/related-persons; physical module policies | UnitTests_FizickaLica_RelatedPersonValidator | RegistryReadAndChoicesHttpTests | PhysicalIdentityConstraintSqlTests | Local backend behavior only; external acceptance is separate. |
| LIM-01 | VERIFIED | B/LimitService; /api/limiti,/page,/import,/export; limits module policies | LimitServiceTests | LimitImportHttpTests | LimitImportSqlTests | Local backend behavior only; external acceptance is separate. |
| LIM-02 | VERIFIED | B/LimitService; /api/limiti,/page,/import,/export; limits module policies | LimitServiceTests | LimitImportHttpTests | LimitImportSqlTests | Local backend behavior only; external acceptance is separate. |
| CAP-01 | VERIFIED | B/CapitalHistory,CapitalService; /api/capital,/history,/as-of; capital read/edit+lock | No dedicated unit; HTTP/SQL | CapitalHistoryHttpTests | CapitalHistorySqlTests | Local backend behavior only; external acceptance is separate. |
| REP-01 | VERIFIED | B/ReportService,FinanceMonthlySource; /api/reports/*; Guest read,KAPITAL_CREATE generate | ReportGenerationTests | ReportHttpAuthorizationTests | ReportFinalizationSqlTests | Local backend behavior only; external acceptance is separate. |
| REP-02 | BLOCKED | B/ReportService,FinanceMonthlySource; /api/reports/*; Guest read,KAPITAL_CREATE generate | ReportGenerationTests | ReportHttpAuthorizationTests | ReportFinalizationSqlTests | Real finance reconciliation and daily source mapping. |
| REP-03 | VERIFIED | B/ReportService,FinanceMonthlySource; /api/reports/*; Guest read,KAPITAL_CREATE generate | ReportGenerationTests | ReportHttpAuthorizationTests | ReportFinalizationSqlTests | Local backend behavior only; external acceptance is separate. |
| NOT-01 | VERIFIED | B/NotificationDispatcher,RoleRequestService,PeriodWorkflow; /api/notifications; exact recipient | EmailDiagnosticsTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Local backend behavior only; external acceptance is separate. |
| NOT-02 | VERIFIED | B/NotificationDispatcher,RoleRequestService,PeriodWorkflow; /api/notifications; exact recipient | EmailDiagnosticsTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Local backend behavior only; external acceptance is separate. |
| NOT-03 | PARTIAL | B/NotificationDispatcher,RoleRequestService,PeriodWorkflow; /api/notifications; exact recipient | EmailDiagnosticsTests | RoleRequestWorkflowHttpTests | RoleRequestWorkflowSqlTests | Role requests/periods notify; direct AppUserService create/replace/deactivate/delete paths stage audit, not inbox/outbox. Broader lifecycle notification coverage needs coordinated events/recipients; not silently added. |
| PER-01 | VERIFIED | B/PeriodWorkflow,PeriodLockRepository; /api/period-lock/*; CapitalRead/Write retained | GlobalPeriodContractTests | PeriodLockCoverageHttpTests | PeriodAtomicSqlTests | Local backend behavior only; external acceptance is separate. |
| PER-02 | VERIFIED | B/PeriodWorkflow,PeriodLockRepository; /api/period-lock/*; CapitalRead/Write retained | GlobalPeriodContractTests | PeriodLockCoverageHttpTests | PeriodAtomicSqlTests | Local backend behavior only; external acceptance is separate. |
| PER-03 | VERIFIED | B/PeriodWorkflow,PeriodLockRepository; /api/period-lock/*; CapitalRead/Write retained | GlobalPeriodContractTests | PeriodLockCoverageHttpTests | PeriodAtomicSqlTests | Local backend behavior only; external acceptance is separate. |
| CODE-01 | VERIFIED | B/CodeListService,C/CodeListController; /api/code-lists; read/admin manage | CodeListServiceTests | CodeListEndpointsIntegrationTests | ControllerResourcesSqlTests | Local backend behavior only; external acceptance is separate. |
| CODE-02 | PARTIAL | B/CodeListService,C/CodeListController; /api/code-lists; read/admin manage | CodeListServiceTests | CodeListEndpointsIntegrationTests | ControllerResourcesSqlTests | Label-add workbook only, not full metadata/language-pack backup roundtrip. |
| CODE-03 | VERIFIED | B/CodeListService,C/CodeListController; /api/code-lists; read/admin manage | CodeListServiceTests | CodeListEndpointsIntegrationTests | ControllerResourcesSqlTests | Local backend behavior only; external acceptance is separate. |
| LOC-01 | PARTIAL | B/RuntimeLocalizationCatalog,Localization/*; /api/localization/*; Guest read/admin write | BusinessResourceContractTests | LocalizationHttpTests | LocalizationSqlTests | Runtime pack/error isolation tested; remaining display/legacy scalar hardcoded text. |
| AUD-01 | PARTIAL | C/AuditLogController,Backend/Helpers/BusinessAudit + transactions; /api/audit-logs; ADMINISTRATION_MANAGE | No dedicated unit; SQL/HTTP | UsersMutationContractHttpTests | AuditQuerySqlTests | Explicit approved-flow audit tested; no universal future-model/folder action interception; exports/read cases vary. |
| ERR-01 | VERIFIED | Exceptions/GlobalExceptionHandler, ControllersExtensions; all endpoints; existing policies | FieldValidationExceptionTests | SafeDiagnosticsHttpTests | ControllerResourcesSqlTests | Local backend behavior only; external acceptance is separate. |
| OBS-01 | VERIFIED | IoC/Middleware,Services/BackgroundOperationsMetrics; /metrics; administration.read | EmailDiagnosticsTests | MetricsAuthorizationHttpTests | NotificationBacklogSqlTests | Local backend behavior only; external acceptance is separate. |
| OBS-02 | BLOCKED | IoC/Middleware,Services/BackgroundOperationsMetrics; /metrics; administration.read | EmailDiagnosticsTests | MetricsAuthorizationHttpTests | NotificationBacklogSqlTests | Live monitoring/scrape/alerts/retention and dependency freshness. |
| OPS-01 | VERIFIED | Program,Dockerfile,IoC/Health; /health/live,/ready; status-only public | ProductionStartupTests | ProductionHostingHttpTests | DatabaseReadinessSqlTests | Local backend behavior only; external acceptance is separate. |
| OPS-02 | BLOCKED | Program,Dockerfile,IoC/Health; /health/live,/ready; status-only public | ProductionStartupTests | ProductionHostingHttpTests | DatabaseReadinessSqlTests | Real shared cache/DP/proxy/resources/drain/SSO/SQL/mail/certs/rotation. |
| PERF-01 | BLOCKED | AsNoTracking,bounded reads,cache/batches; existing page routes/read policies | No dedicated broad benchmark | RegistryReadAndChoicesHttpTests | PagedUsersPerformanceSqlTests | Real cardinality/load/plans/latency/SLOs; PERF-02..06. |
| TEST-01 | PARTIAL | tests/Backend.* and pipelines; all policies/routes | Full unit suite | Full integration suite | All SQL-labelled classes | Backend verified; full application BrowserHost/roles/browser remain frontend integration. |
| DOC-01 | PARTIAL | README,contracts,configuration and handoffs; no new route | Not applicable | Local source/link/contract review | Not applicable | Backend evidence complete; repository consolidation/release notes cross-owner. |
| FE-01 | BLOCKED | BrowserHost frontend/backend assembly collision; no API policy change | Not applicable | Not run: frontend ownership | Not applicable | Retain Claude frontend assembly fix and rerun integrated browser gates. |

Additional requirements: TAX-01 BLOCKED (maximum13 vs existing exact13/checksum decision); DOC-02 MISSING (GitHub release notes); SEC-01 BLOCKED (tracked/history secret and rotation); SEC-02/03 PARTIAL (cookie/logout coordination); SEC-04 BLOCKED (approved admission controls); PERF-02/03 PARTIAL (unbounded tree/export contract); LEG-06 PARTIAL (identifier immutability comment contradicts current validated replacement behavior). These refine earlier broad VERIFIED rows; no historical text was rewritten to hide them.

### Exact test-source index

- [tests/Backend.UnitTests/Authentication/ApplicationAuthorizationPolicyTests.cs](../tests/Backend.UnitTests/Authentication/ApplicationAuthorizationPolicyTests.cs)
- [tests/Backend.IntegrationTests/Tests/Operations/AuditQuerySqlTests.cs](../tests/Backend.IntegrationTests/Tests/Operations/AuditQuerySqlTests.cs)
- [tests/Backend.UnitTests/Authentication/AuthenticatedActorTests.cs](../tests/Backend.UnitTests/Authentication/AuthenticatedActorTests.cs)
- [tests/Backend.UnitTests/Localization/BusinessResourceContractTests.cs](../tests/Backend.UnitTests/Localization/BusinessResourceContractTests.cs)
- [tests/Backend.IntegrationTests/Tests/CapitalHistoryHttpTests.cs](../tests/Backend.IntegrationTests/Tests/CapitalHistoryHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/CapitalHistorySqlTests.cs](../tests/Backend.IntegrationTests/Tests/CapitalHistorySqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/CodeListEndpointsIntegrationTests.cs](../tests/Backend.IntegrationTests/Tests/CodeListEndpointsIntegrationTests.cs)
- [tests/Backend.UnitTests/Services/CodeListServiceTests/CodeListServiceTests.cs](../tests/Backend.UnitTests/Services/CodeListServiceTests/CodeListServiceTests.cs)
- [tests/Backend.UnitTests/Helpers/ConnectionHelperTests.cs](../tests/Backend.UnitTests/Helpers/ConnectionHelperTests.cs)
- [tests/Backend.IntegrationTests/Tests/ControllerResourcesSqlTests.cs](../tests/Backend.IntegrationTests/Tests/ControllerResourcesSqlTests.cs)
- [tests/Backend.UnitTests/Services/LegalEntityServiceTests/CreateLegalEntityTests.cs](../tests/Backend.UnitTests/Services/LegalEntityServiceTests/CreateLegalEntityTests.cs)
- [tests/Backend.IntegrationTests/Tests/Operations/DatabaseReadinessSqlTests.cs](../tests/Backend.IntegrationTests/Tests/Operations/DatabaseReadinessSqlTests.cs)
- [tests/Backend.UnitTests/Services/EmailDiagnosticsTests.cs](../tests/Backend.UnitTests/Services/EmailDiagnosticsTests.cs)
- [tests/Backend.UnitTests/Exceptions/FieldValidationExceptionTests.cs](../tests/Backend.UnitTests/Exceptions/FieldValidationExceptionTests.cs)
- [tests/Backend.UnitTests/Authentication/GlobalPeriodContractTests.cs](../tests/Backend.UnitTests/Authentication/GlobalPeriodContractTests.cs)
- [tests/Backend.UnitTests/Authentication/JwtAudienceConfigurationTests.cs](../tests/Backend.UnitTests/Authentication/JwtAudienceConfigurationTests.cs)
- [tests/Backend.IntegrationTests/Tests/Operations/JwtAudienceHttpTests.cs](../tests/Backend.IntegrationTests/Tests/Operations/JwtAudienceHttpTests.cs)
- [tests/Backend.UnitTests/Services/KeycloakIdentityLookupTests.cs](../tests/Backend.UnitTests/Services/KeycloakIdentityLookupTests.cs)
- [tests/Backend.IntegrationTests/Tests/LegalEntity/LegalEntityHttpValidationTests.cs](../tests/Backend.IntegrationTests/Tests/LegalEntity/LegalEntityHttpValidationTests.cs)
- [tests/Backend.IntegrationTests/Tests/LimitImportHttpTests.cs](../tests/Backend.IntegrationTests/Tests/LimitImportHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/LimitImportSqlTests.cs](../tests/Backend.IntegrationTests/Tests/LimitImportSqlTests.cs)
- [tests/Backend.UnitTests/Services/LimitServiceTests/LimitServiceTests.cs](../tests/Backend.UnitTests/Services/LimitServiceTests/LimitServiceTests.cs)
- [tests/Backend.IntegrationTests/Tests/LocalizationHttpTests.cs](../tests/Backend.IntegrationTests/Tests/LocalizationHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/LocalizationSqlTests.cs](../tests/Backend.IntegrationTests/Tests/LocalizationSqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/Operations/MetricsAuthorizationHttpTests.cs](../tests/Backend.IntegrationTests/Tests/Operations/MetricsAuthorizationHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/Operations/NotificationBacklogSqlTests.cs](../tests/Backend.IntegrationTests/Tests/Operations/NotificationBacklogSqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/Operations/PagedUsersPerformanceSqlTests.cs](../tests/Backend.IntegrationTests/Tests/Operations/PagedUsersPerformanceSqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/PeriodLock/PeriodAtomicSqlTests.cs](../tests/Backend.IntegrationTests/Tests/PeriodLock/PeriodAtomicSqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/PeriodLock/PeriodLockCoverageHttpTests.cs](../tests/Backend.IntegrationTests/Tests/PeriodLock/PeriodLockCoverageHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/LegalEntity/PhysicalIdentityConstraintSqlTests.cs](../tests/Backend.IntegrationTests/Tests/LegalEntity/PhysicalIdentityConstraintSqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/Operations/ProductionHostingHttpTests.cs](../tests/Backend.IntegrationTests/Tests/Operations/ProductionHostingHttpTests.cs)
- [tests/Backend.UnitTests/Authentication/ProductionStartupTests.cs](../tests/Backend.UnitTests/Authentication/ProductionStartupTests.cs)
- [tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryReadAndChoicesHttpTests.cs](../tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryReadAndChoicesHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryVerificationHttpTests.cs](../tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryVerificationHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryVerificationSqlTests.cs](../tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryVerificationSqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryWorkbookHttpTests.cs](../tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryWorkbookHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryWriteSqlTests.cs](../tests/Backend.IntegrationTests/Tests/LegalEntity/RegistryWriteSqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/Report/ReportFinalizationSqlTests.cs](../tests/Backend.IntegrationTests/Tests/Report/ReportFinalizationSqlTests.cs)
- [tests/Backend.UnitTests/Services/ReportServiceTests/ReportGenerationTests.cs](../tests/Backend.UnitTests/Services/ReportServiceTests/ReportGenerationTests.cs)
- [tests/Backend.IntegrationTests/Tests/Report/ReportHttpAuthorizationTests.cs](../tests/Backend.IntegrationTests/Tests/Report/ReportHttpAuthorizationTests.cs)
- [tests/Backend.UnitTests/Authentication/RoleClaimsTransformationTests.cs](../tests/Backend.UnitTests/Authentication/RoleClaimsTransformationTests.cs)
- [tests/Backend.IntegrationTests/Tests/Users/RoleRequestWorkflowHttpTests.cs](../tests/Backend.IntegrationTests/Tests/Users/RoleRequestWorkflowHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/Users/RoleRequestWorkflowSqlTests.cs](../tests/Backend.IntegrationTests/Tests/Users/RoleRequestWorkflowSqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/Operations/SafeDiagnosticsHttpTests.cs](../tests/Backend.IntegrationTests/Tests/Operations/SafeDiagnosticsHttpTests.cs)
- [tests/Backend.UnitTests/Authentication/SubjectLinkAndVirtualGuestTests.cs](../tests/Backend.UnitTests/Authentication/SubjectLinkAndVirtualGuestTests.cs)
- [tests/Backend.IntegrationTests/Tests/Users/SubjectLinkSqlServerTests.cs](../tests/Backend.IntegrationTests/Tests/Users/SubjectLinkSqlServerTests.cs)
- [tests/Backend.UnitTests/RelatedPersons/UnitTests_FizickaLica_RelatedPersonValidator.cs](../tests/Backend.UnitTests/RelatedPersons/UnitTests_FizickaLica_RelatedPersonValidator.cs)
- [tests/Backend.IntegrationTests/Tests/Users/UserProfilesHttpTests.cs](../tests/Backend.IntegrationTests/Tests/Users/UserProfilesHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/Users/UserProfilesSqlTests.cs](../tests/Backend.IntegrationTests/Tests/Users/UserProfilesSqlTests.cs)
- [tests/Backend.IntegrationTests/Tests/Users/UsersLastAdminSqlServerTests.cs](../tests/Backend.IntegrationTests/Tests/Users/UsersLastAdminSqlServerTests.cs)
- [tests/Backend.IntegrationTests/Tests/Users/UsersMutationContractHttpTests.cs](../tests/Backend.IntegrationTests/Tests/Users/UsersMutationContractHttpTests.cs)
- [tests/Backend.IntegrationTests/Tests/Users/UsersSafetyHttpTests.cs](../tests/Backend.IntegrationTests/Tests/Users/UsersSafetyHttpTests.cs)
- [tests/Backend.UnitTests/Helpers/WorkbookEnvelopeTests.cs](../tests/Backend.UnitTests/Helpers/WorkbookEnvelopeTests.cs)

Coverage gaps without invented tests: live SMTP/mail provider, real realm login/refresh, bank spreadsheets, deep-tree/real-load/cross-pod drains, irreversible historical SQL rollback, full browser integration and GitHub release source. The local secret-pattern scan is not a credential-revocation test. No load crash or security exploit is claimed solely from source risk.

## 9. Confirmed backend correction

AUD-BOUNDS-01/02 (P2, RESOLVED): authorized GET `/api/audit-logs?page=2147483647&pageSize=200` produced500 from negative SQL OFFSET; `dateFrom=9999-12-31&dateTo=9999-12-31` produced500 from AddDays overflow. Before patch: three SQL HTTP regressions2 failed / 1 passed. Patch uses int64 saturating offset and applies next-day exclusive upper bound only when representable; final day naturally includes all remaining SQL DateTime2 values. After patch3/3 PASS, including normal inclusive-day filter, unauthorized/Guest denial, empty far page/total/page metadata and no audit mutation. No migration/policy/DTO/schema change, no UI workaround. Tests: AuditQuerySqlTests; code: Backend/API/Controllers/AuditLogController.cs.

## 10. Remaining issues ranked and assigned

P0: SEC-01 configuration/history credential, frontend+realm/security owner; production cannot proceed without rotation and clean merged configuration.

P1: SEC-06 provider diagnostic privacy verification (backend/security); SEC-02/03 cookie/CSRF and GET logout coordination (security/backend/frontend); SEC-04 admission/concurrency policy (platform/backend); legacy production migration mapping/backup (DBA); real realm/bank/finance/mail/infrastructure and scheduler/session/DP/drain policy (named environment owners); PERF-02/03 tree/export bounds (backend/frontend, coordinated contract). No unauthenticated privilege-escalation or last-Admin bypass was reproduced in the reviewed regression paths; absence of such a reproduction is not a global proof.

P2: direct user lifecycle notice coverage (backend/product/front-end events), generic/future audit coverage (backend/security policy), residual localization and advisory module map (backend/frontend), language/codebook full-fidelity bulk format (product contract), real query plans/load/retention/health/monitoring drills (DBA/platform), release-notes source (product/frontend/backend), legacy result-wrapper/cancellation/unpaged compatibility maintenance (backend, avoid breaking integration).

P3: unused scaffold clients/helpers and large-class organization. Deliberately no cleanup-only refactor. No new schema/index is justified without approved migration impact and real query plans. No feature or policy was added to make a checklist appear green.

## 11. Exact verification evidence

- Unit: **337 passed / 0 failed / 0 skipped (337 total)**.
- Integration: **395 passed / 0 failed / 0 skipped (395 total)**.
- Focused audit SQL: before **1 passed / 2 failed / 0 skipped (3 total)**; after **3 passed / 0 failed / 0 skipped (3 total)**.

Commands (PowerShell, intended backend checkout):

```powershell
dotnet test tests/Backend.UnitTests -c Release --no-restore -m:1 -nr:false --logger "trx;LogFileName=unit.trx" --results-directory artifacts/backend-independent-audit/results
$env:RBBH_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true'
dotnet test tests/Backend.IntegrationTests -c Release --no-restore -m:1 -nr:false --logger "trx;LogFileName=integration.trx" --results-directory artifacts/backend-independent-audit/results
dotnet build Backend/RelatedPartiesRegister.csproj -c Release --no-restore -m:1 -nr:false -p:RunAnalyzers=true
dotnet publish Backend/RelatedPartiesRegister.csproj -c Release --no-restore --no-build -o artifacts/backend-independent-audit/publish
git diff --check
```

Only owned GUID LocalDB test databases are created/dropped. Ignored TRX/logs/ledger/migration-marker/secret-candidate files are under artifacts/backend-independent-audit; credential values are not stored in scan output. Full counters, SQL subset, build/publish/startup and git result are recorded in the final verification entry below. No assertions/timeouts/retries/skips were weakened. No frontend/browser test was modified or run in the integration worktree.

## 12. Required approvals and recovery

- Realm/security: historical client-secret rotation/revocation confirmation, correct current client/API audience, redirect/refresh/logout and verified email; approved one-time subject-link export/reconciliation, no guessed sub.
- DBA: actual migration baseline, old destructive-field/FK mapping, duplicate/orphan preflight, restore rehearsal, reviewed script in timestamp order, production least-privilege accounts, monitoring and forward repair. This audit has no new migration to approve.
- Platform: shared cache/session and DP protection, proxy/cert/network/egress/CORS/3scale values; per-replica and global SQL/memory/ingress budgets, resources/drain/shutdown and scheduler singleton/overlap/calendar policy; scrape/auth/alerts/retention/failover and image/dependency approval.
- Product/frontend/backend: consumer contract adoption, cookie-CSRF transport decision, bounded tree/export behavior, user lifecycle events/recipient rules, release-notes/source contract and tax-length clarification. Pending changes are not silently implemented while Claude resolves browser failures.

## 13. Before UAT / production

Integrate only the focused audit boundary fix and approved documentation through the existing owner-controlled process; retain Claude's frontend assembly/secret cleanup and assertions. Run the full integrated .NET/six-role browser/Axe/viewport/light-dark gate, then validate actual Keycloak/SQL/mail/bank/finance/proxy behavior in approved UAT with representative data. Exercise denied actions, rollback, restart/cache/session continuity, worker backlog/failure, backup restore and migration recovery. Production promotion remains blocked until named owners accept all P0/P1 prerequisites and unresolved policies. Backend test success alone does not authorize deployment.

## Final verification entry

Tested implementation tree is commit **bddc9136f948c7d8a3c1c3f141d1e461fcc09f5d**; subsequent delivery changes are documentation only.

- Unit337 passed / 0 failed / 0 skipped; integration395 passed / 0 failed / 0 skipped; combined732/732. SQL-classified 86/86 are a subset, including UsersLastAdminSqlServerTests 6/6 and the new AuditQuerySqlTests 3/3. Three existing last-Admin races pass. No count is added twice.
- Focused audit regression before:2 failed / 1 passed; after:3passed. Normal date boundaries, far-page metadata/empty results, Guest403/anonymous401 and no read-side audit mutations are asserted against real disposable SQL.
- Analyzer-enabled backend Release build:0 warnings / 0 errors. Release publishPASS. Published artifact startup on owned loopback5373 in explicit Development/InMemory: healthlive200/ready200, profile/legal-list/report-list200, XLSX legalexport200, Guest audit/metrics403, Guest legal/report-generationPOST403, unconfigured GCC503. No fake source or realSSO success. Owned smoke process stopped via Ctrl+C; other hosts/integration worktree unchanged.
- The first full integration runner was interrupted during the execution-tool runtime update and emitted no final TRX; it is not counted as success or as failed assertions. A new complete run produced the395PASS TRX. Test retries/timeouts/assertions were not changed.
- Focused existing SQL paging performance test separately 1/1 PASS; query counts 10/9 for page sizes 1/100. **Full-suite HTTP samples: 27,244.4ms and 2,403.9ms; isolated samples: 25,750.1ms and 25,114.9ms.** These local timings are high even in the isolated run. The count assertion passes but proves neither acceptable latency nor the cause. PERF-07 is **P1 PARTIAL**, backend/DBA owner: record SQL command timings/plans and host/LocalDB resource telemetry before accepting latency or changing indexes/query semantics. No improvement or production SLO is claimed. Exact output remains in performance-isolated.trx and integration.trx. This observation is not hidden by the overall pass count.
- NuGet vulnerability-list command exited 0, no vulnerable entries in the reported source output; no production/corporate-feed/image/SBOM approval inferred. PKCS9.0.21 unchanged; no NU warning suppression.
- Full solution/native browser not rerun: known frontend-owned duplicate DATA_CORE assembly reference issue in this source checkout; actual integration and its28 Admin failures are Claude-owned. No assumption that those failures are solved by this backend patch.
- `git diff --check` and staged diff check PASS. Backend fix committed separately; audit/integration notes committed subsequently. Final HEAD and clean working tree recorded in delivery. No push, merge, rebase, cherry-pick, migration against shared data or frontend file edit.

Source-preservation review: the only runtime change is two query-boundary expressions in AuditLogController; the only new test source is AuditQuerySqlTests. Existing migrations, DTOs, policies, financial formulas, SQL schemas, historical data, frontend/browser sources and existing user launchSettings are unchanged.
