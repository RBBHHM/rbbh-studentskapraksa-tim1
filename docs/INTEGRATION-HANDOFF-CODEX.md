# Codex backend integration handoff

Reviewed 2026-11-03. Backend branch `feat/codex-backend-finalization`; input/tested implementation HEAD **82760bb513f6fa2efe2f9b970bb612b79d509e77**. This delivery adds documentation only. Its final documentation commit is reported in the delivery message (`git rev-parse HEAD`), not embedded as a self-referential hash.

## Decision and ownership

The backend is locally integration-ready **with explicit contract gaps**, not production-certified or full-app accepted. Fresh 337unit+392integration=729PASS, zero failed/skipped, including 83SQL-classified cases and 6 last-Admin SQL cases. No backend/test/permission changes were needed in this handoff. No push, merge, rebase, cherry-pick, frontend edit, new business feature, warning suppression or shared data migration was performed by Codex.

Use [the final exact contract](BACKEND-FRONTEND-CONTRACT-FINAL.md), not proposed DTOs from frontend coverage requests. It covers108HTTP operations/66named schemas, anonymous responses, model/service validation, policy/period boundaries, pagination, language/error behavior and ownership/replay semantics. Existing [v1 contract](BACKEND-FRONTEND-CONTRACT-V1.md) and [backend evidence](BACKEND-FINALIZATION.md) remain historical; the final contract corrects incomplete/stale display and shape assumptions rather than changing the API.

## Repository snapshots and safest integration strategy

Backend checkout: `studentskapraksaTema1-codex-backend-finalization` (separate Git directory).
Frontend checkout inspected read-only: `studentskapraksaTema1-claude-frontend`, branch `feat/claude-frontend-finalization`, HEAD `4d49c8a7d8f04404779e9798529fe030d6858a92`, clean at inspection.
Their common ancestor is **48cf8da8f0bb77129ea0b14ad5366c7cea7ce14b**. The backend checkout does not have a local Claude branch; do not assume branch names resolve to the same objects in independent repositories.

Claude's repository also already contains **`integration/frontend-backend` at `b3e5e9e1fd247e97fd6dd7bd2f42176529a52a28`**, checked out separately in `studentskapraksaTema1-integration`. That merge has parents `4d49c8a7d8f04404779e9798529fe030d6858a92` and `82760bb513f6fa2efe2f9b970bb612b79d509e77`. It includes this backend input HEAD; the original frontend branch itself remains unmerged at 4d49c8a. This existing integration branch was observed, not created or modified by Codex. Do not recreate the merge or cherry-pick backend commits already present there. Integration branch/browser results are not claimed from merely reading its history.

After explicit integration approval, the owner should preserve that branch's two-parent history, bring in this documentation-only delivery if required, adopt the frontend actions below, review shared-file resolutions semantically, then run the combined gate. Do not reset either source branch or rewrite migrations/old reports/identity links. Backend history is the complete dependency-ordered unit; selective omission of schema/validation/security commits is unsafe. Existing frontend fix59b8d38 must be retained; do not independently rename the backend assembly to evade it.

### Shared files requiring semantic review

Compared with48cf8da, only these tracked paths overlap between backend82760bb and frontend4d49c8a:

| Path | Frontend delta | Backend delta | Resolution requirement |
| --- | --- | --- | --- |
| README.md | onboarding consolidation | backend operation/config/contract notes | Preserve frontend source launch/assembly commands and backend migration/security/external prerequisite guidance. |
| docs/configuration/ENVIRONMENT-VARIABLES.md | deleted in consolidation | updated | Modify/delete conflict: migrate current backend settings into agreed README/onboarding or deliberately retain this guide; do not discard bank/finance/session/mail/migration requirements. |
| docs/development/LOCAL-DEVELOPMENT.md | deleted in consolidation | updated | Modify/delete conflict: retain backend SQL test safety and commands in agreed onboarding; preserve one authoritative developer entry point. |

No Backend runtime path or migration overlaps in those two input branches. New final handoff files should remain discoverable from the consolidated README/onboarding; do not remove evidence merely because older general documents were retired.

## Three BrowserHost failures: precise diagnosis

Fresh analyzer-enabled Release solution build in this backend checkout still fails before browser execution:

| Source | Compiler error | Classification / action |
| --- | --- | --- |
| tests/Web.Blazor.BrowserHost/BrowserHostProgram.cs:25 | CS0234: RBBH.RelatedPartiesRegister namespace missing | Frontend project resolves to the same DATA_CORE assembly identity as backend. |
| same:26 | CS0246: RBI namespace missing | Same reference resolution collision; not an API endpoint/test assertion failure. |
| same:192 | CS0234: RBBH.RelatedPartiesRegister namespace missing | BrowserApp references frontend Program through the colliding project identity. |

Both input csproj files declare AssemblyName DATA_CORE; BrowserHost references both. Backend's deployed entrypoint legitimately remains DATA_CORE.dll. Claude already owns **59b8d382ce5cd192e32e9a3a51c472adf8ea960a**, removing the frontend AssemblyName override so it builds as RBBH.RelatedPartiesRegister.Web. It is present in frontend4d49c8a and the observed integration merge. No backend or shared-harness workaround is justified. Confirm Docker/publish frontend entrypoint retains that resolved name and rerun BrowserHost after authorized integration. No browser tests were edited, weakened, retried or represented as passing here.

## Required frontend actions (source compatibility review)

Inspection covered this checkout's Frontend/Api clients plus the separate frontend HEAD's API clients and docs/FRONTEND-COVERAGE.md. Existing CRUD routes generally remain; proposed new shapes are **not** implemented aliases.

| Feature / current assumption | Actual backend / required action | Status |
| --- | --- | --- |
| Role request type=Request/Change, role-name list, GET roles for all | Use purpose=new/change, GUID roleIds, clientRequestId; choices GET role-requests/options; decisions require decisionId/expectedVersion. Consume RoleRequestDTO and requester fields on admin list. | Frontend adoption |
| Role-request detail/currentRoles or invented status options | No detail/currentRoles field; mine/review list has lowercase statuses. Reuse profile for current roles; keep server-owned states. | Contract clarified |
| Admin detection from provider roles/isAdministrator | Profile permissions are DB codes; ADMINISTRATION_MANAGE, plus service recheck of named DB Admin on decisions/catalog. No isAdministrator field. | Frontend guard alignment |
| Profile/user editing and identity | PUT api/users/{id} accepts profile-only fields; UserDTO includes identityConfirmed. Application id is not sub; refresh profile after grants change. Hide own-row role actions; preserve server denials. | Frontend adoption |
| NotificationsApi reads api/email-log | Switch production inbox/bell to api/notifications; route not link, parameters object, unreadCount from list. No read-all/unread-count/type endpoint/field. Mark single read POST returns204. Preview remains isolatedDevelopment/demo. | Frontend adoption |
| GCC lookup api/lookups/gcc or exact-number detail | Use paged api/bank-reference/gcc and customers; server resolves name. Add gccNumber/gccName/isLargeExposure nullable fields, preserve null vs explicitfalse and conditional configured-source requirement. Handle503 without fake options. | Frontend adoption + external bank |
| Capital POST/validFrom/validTo/isActive | PUT capital appends, history/as-of GET; effectiveDate/revision/recordedAt/recordedBy/source. GET can204; null historical capital staysunknown. | Frontend adoption |
| Physical form trusts nullable DTO dates/looser annotation lengths | Service requires relation/special basis/description and both dates; main nonresident passport+FBA; passport5–20 alnum/hyphen; names2–100 Unicode letters/spaces/apostrophe/hyphen. Full rules in final contract. | Frontend validation alignment |
| Legal form omits false residency or connected flag | isResident must be explicitly true/false; connectedWithBank nonnull; name/basis/description/dateFrom required; exact13tax retained pending clarification. Update is full business DTO, not PATCH. | Frontend validation alignment |
| Verification code verification.same-user | Actual403 code verification.self_denied;409 provenance_required on historical unknown creator; no DTO field exposes immutable creator sub. Existing creator-name tooltip is advisory; endpoint remains authority. | Frontend error alignment |
| Import errors are structured objects | Existing errors:string[] retained; details:[{row,field,code,message}] additive. Show imported/failed even on200; multipart field file, XLSX binary, canonical workbook columns. | Frontend parser alignment |
| Period processor uses unlock-requests list | That list now owner-only; review queue uses unlock-requests/review. Preserve CapitalWrite, owner-only respond10–490, uppercase period states, locked registry/capital/verification controls. | Frontend routing alignment |
| Runtime flat map at localization/{culture} | GET localization/resources/{language} returns language/fallbackLanguage/version/items; languages returns items code/name (no isDefault). Feed existing RBI localization, propagate Accept-Language every request; retain scoped isolation. | Frontend adoption |
| Definition lastModified fields | GET code-lists/categories/definitions uses createdAt/createdBy/modifiedAt/modifiedBy/version; PUT requires expectedVersion. Admin values differ from full Bosnian-name DTO/dropdown. | Frontend parser alignment |
| PeriodRequestStatus/RecordState generic catalogs already seeded | No new guarantee/seed. Wire statuses invariant; no fabricated options. Coordinate existing UI allowlist/catalog decision. | PARTIAL cross-owner |
| Eight-hour login-free guarantee | Backend absolute cap8h, token expiry may be earlier; frontend RBI cookie/cache and real realm must align; backend POSTlogout CSRF differs from frontend own endpoint. | PARTIAL/external |
| Release-notes route backed by GitHub | No backend route/DTO/source/environment-tag selection. Explicit MISSING; requires separately coordinated implementation, not invented response. | MISSING |
| Every display string is already runtimeDE | Business/validation pack implemented; audit/module/person-type/demo-preview legacy display values have limits. Frontend localizes canonical codes; remaining backend text cleanup separately scoped. | PARTIAL |

Sources: [all exact routes/DTOs](BACKEND-FRONTEND-CONTRACT-FINAL.md); Frontend/Api/UsersApi.cs, NotificationsApi.cs, PeriodApi.cs, CapitalApi.cs, LegalEntitiesApi.cs, RelatedPersonsApi.cs, CodeListsApi.cs, AppAuthorization.cs; separate frontend docs/FRONTEND-COVERAGE.md requests inspected at 4d49c8a. No frontend work was performed here.

## Migrations and safe deployment order

Existing migration history remains intact. Relative to48cf8da, apply these additive migrations **in existing timestamp order**, using a reviewed deployment script/bundle and a backup/restore-rehearsed target. Do not run them automatically on production startup. The two existing apps must not receive traffic before compatible schema is ready. SQL integration tests exercise populated disposable LocalDB upgrades and concurrency, not real bank or production data.

| Migration ID | Purpose / precondition |
| --- | --- |
| `20261102221105_AddRegistryCreatorProvenance` | Nullable immutable creator sub. Historical provenance stays null; verification409 pending approved evidence. |
| `20261102221643_AddPeriodRequestOwnership` | Nullable trusted owner sub. Do not infer historical owner from displayed email. |
| `20261102223035_AddRoleRequestsAndNotifications` | Requests/role links/inbox/leased outbox. No production recipient/identity invention. |
| `20261102225344_EnforceNormalizedApplicationUsers` | Normalized uniqueness/FKs with duplicate/orphan preflight. Stop for reconciliation; never merge users silently. |
| `20261102231423_AddRuntimeLocalizationCodebooks` | Two runtime codebooks/semantic missing seeds and integrity. Existing translations retained. |
| `20261102232309_AddRegistryGccAndExposure` | Nullable GCC/name/large-exposure fields. No historical bank-data inference. |
| `20261103062339_EnforceReferenceAndPhysicalIdentityKeys` | Normalized definitions and active physical identity keys. Preflight duplicate/reference conflicts. |
| `20261103063026_AddImmutableCapitalHistory` | Append-only revisions and SQL mutation trigger. Existing known observations preserved, no fabricated history. |
| `20261103064045_AddReportSnapshotProvenance` | Source provenance defaults for old snapshots; JSON retained. |
| `20261103065839_AddLimitImportIdempotency` | Fingerprint/idempotency storage and constraints. Existing ordinary records preserved. |
| `20261103075242_AddPeriodConcurrencyAndNotificationResources` | Rowversion and missing templates; runtime cache version handling. |
| `20261103083033_EnforceOpaqueSubjectComparisons` | BIN2 case-sensitive sub comparisons across mappings/owners/inbox. Values not recased/backfilled. |
| `20261103084605_AddAdministrativeRoleCatalog` | Definition display/version metadata and normalized role-name preflight. |
| `20261103085814_AddBusinessErrorResources` | Missing BS/EN/DE semantic error resources; existing labels retained. |
| `20261103112709_AddControllerAndValidationResources` | Missing controller/model resources; existing labels retained. |
| `20261103115034_AddServiceValidationAndWorkbookResources` | Missing service/import/period resources; canonical workbook protocol retained. |

Never run Down/destructive rollback casually: app rollback should retain additive data/schema; schema rollback needs separate data-loss review. Rehearse pending migration detection, constraints/trigger deployment, permission grants, audit/no-mutation rollback and backup restore. Review baseline historical destructive migrations before selecting a real DB baseline. The tested existing opt-in verified-email first-link remains false pending real realm/export/backfill approval; this delivery runs no subject migration. Unknown historical creators/period owners require evidence, not guessed email-to-sub updates.

## Verification commands and current results

Run from the intended checkout. Do not run test SQL against shared UAT/PROD servers/catalogs. RBBH_TEST_SQLSERVER selects a disposable local SQL server; tests create GUID databases and clean only their own fixtures.

```powershell
git status --short
git rev-parse HEAD
git diff --check
dotnet test tests/Backend.UnitTests -c Release --no-restore -m:1 -nr:false --logger "trx;LogFileName=unit.trx" --results-directory artifacts/backend-integration-handoff/results
$env:RBBH_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true'
dotnet test tests/Backend.IntegrationTests -c Release --no-restore -m:1 -nr:false --logger "trx;LogFileName=integration.trx" --results-directory artifacts/backend-integration-handoff/results
dotnet build Backend/RelatedPartiesRegister.csproj -c Release --no-restore -m:1 -nr:false -p:RunAnalyzers=true
dotnet publish Backend/RelatedPartiesRegister.csproj -c Release --no-restore --no-build -o artifacts/backend-integration-handoff/publish
dotnet build RelatedPartiesRegister.slnx -c Release --no-restore -m:1 -nr:false -p:RunAnalyzers=true
```

Observed fresh results: unit337/337, integration392/392, failed0/skipped0; SQL-classified83/83 are a subset of integration (not extra tests), including UsersLastAdminSqlServerTests6/6 with three concurrent races. Backend build0warnings/0errors; publishPASS. Full solution build0warnings/3BrowserHost errors as above. No vulnerability-feed certification is inferred from no-restore; PKCS9.0.21 remains pinned and NU1900/NU1902/NU1903 are not suppressed.

Published startup (owned loopback 5372, explicit Development/InMemory):

```powershell
Push-Location artifacts/backend-integration-handoff/publish
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet DATA_CORE.dll --environment Development --urls http://127.0.0.1:5372 --Database:ServerName= --Database:Name= --Database:ApplyMigrations=false --Email:Provider=demo --KeycloakSettings:Enabled=false
# In a second terminal:
Invoke-WebRequest http://127.0.0.1:5372/health/live
Invoke-WebRequest http://127.0.0.1:5372/health/ready
```

Actual smoke: live200/ready200/profile200/legal-list200/inbox200; Guest legalPOST403; unconfigured GCC503; empty nonseeded localizationEN404 (expected; startup does not seed). No fake bank/finance/realSSO success. Stop only this owned process with Ctrl+C when finished; other users' hosts remain untouched. Existing EF required Limit/filtered LegalEntity runtime notice remains; active limit reads explicitly join filtered parents. A zero-warning compiler result does not claim no runtime notices.

Fresh ignored local evidence: artifacts/backend-integration-handoff/results/*.trx, openapi.json, analyzers.log, publish.log, solution-build.log, startup.json. Reusable test sources are committed; ignored machine artifacts are not staged. Original PDF20-page extraction/image review from the same audit is preserved under artifacts/backend-finalization/requirements-pdf.txt and pdf-images; requirements and source were rechecked against POPRAVKE and the current matrix.

After approved frontend integration, additionally run the **full solution tests and the frontend owner's existing six-role browser/Axe/light-dark/viewport/keyboard checks** using that integration branch's README/onboarding commands, workers1/retries0. Do not claim those gates from backend-only results and do not weaken assertions or update screenshots to hide incompatibilities.

## Remaining owners / blockers

- Frontend: adopt the exact implemented contracts above, retain59b8d38 and complete native browser gates. This backend checkout intentionally cannot pass BrowserHost without that frontend-owned integration.
- Product/frontend/backend coordination: release-notes API is MISSING; runtime status-catalog/display-label gaps are PARTIAL. These are precisely documented, not fabricated APIs or hidden completion claims. No speculative implementation was added.
- Identity/SSO: actual audience/issuer/redirect/refresh/email_verified/service-account privileges, account-recreation reconciliation, approved one-time subject-link/backfill and historical creator/owner reconciliation. Do not enable first-link as a shortcut.
- Bank/finance: source access, exact mappings/types/period tables/keys/BAM, spreadsheets and reconciled sample totals; daily authoritative finance mapping outstanding. No real-source truth asserted from local adapters.
- Operations: backup/restore and legacy baseline, shared session cache/protected DP keys, real OCP/proxy/3scale/CA/hosts, resources/drain/scheduler HA/calendar, SQL/mail/cert/failover, telemetry/alerting ownership and live vulnerability-feed confirmation. Historical exposed credentials require owner rotation, not publishing their values.
- Business: maximum13 versus preserved exact13/checksum tax rule remains unresolved (TAX-01).

## Backend-owned commits to preserve

All commits since the common48cf8da base are listed in dependency order. The current handoff commit is documentation-only and intentionally follows this frozen list.

| SHA | Subject |
| --- | --- |
| `43670616b2a46a7b91310d09f02222f8187094f0` | docs: audit backend requirements and propose shared contracts |
| `2bb9b9aca6f6ab9ba1add044d53bf458b0b4d779` | fix(backend): redact operational diagnostics and bound correlation IDs |
| `292acba0e06846a44fdb183eef4b7e64936bbd97` | fix(auth): require configured API audience outside Development |
| `42a13d546fb42a64288445f8715dd32a2b4df007` | fix(notifications): encode business messages and correct application links |
| `e491a6e312c78013da4ee0da2cf7c666858d4158` | fix(hosting): respect migration opt-in and align backend entrypoint |
| `927532680e28b684d5905e58cad6f4627b0f0eb9` | fix(import): enforce field bounds and reject malformed registry workbooks |
| `99ccca18441c6e98e489326701a80122c044baec` | fix(codebooks): preserve stable keys and guard referenced deactivation |
| `872ddaa5aae2736b84f2eb7763ea8dcb2bb61d54` | fix(logging): keep business and mail data out of operational events |
| `05b63b4a3d0e44a65eb1b2b72727d03b331f6b83` | docs: record backend evidence and approval-gated security proposals |
| `7f2585d4f1aa7f1920017717545a5eccccced913` | fix(verification): enforce subject-bound four-eyes and atomic audit |
| `5735ba0d54aa999d438d183f425956a188992263` | fix(period): scope requests by subject and serialize responses |
| `73e4d7fc3422e017b888664a85e8f5392a97b20f` | docs: publish additive frontend contracts and S2 S3 evidence |
| `6ababe7bfeafc189b53cf3fe9354c00f6d67e211` | feat(authorization): add atomic role requests and recipient inbox |
| `2f6da19a04f68a76f8205245028a5075ae2942ac` | test(authorization): verify role changes and verified-email boundary |
| `b27a6d4737d1e3b506e8d1e2855389a7d1218fe3` | fix(authentication): cap sessions and add CSRF-protected logout |
| `96cca27712bdabf9ed3aa25eff6973dcfeaa82ee` | feat(users): complete profile management and normalized identity safeguards |
| `2d29ebcde09c627d794b0b26712c44b741167372` | feat(localization): add runtime codebooks and transactional resource updates |
| `05d173b876313004fb16ab765c7700d82be47a27` | feat(registry): add source-backed GCC and paged bank lookups |
| `69fabd59ba671212fa910690bfacac9da8669a97` | fix(registry): roundtrip workbook data and persist audit atomically |
| `a61ebc4086cb08001ce5286f64ee2127ed45e140` | fix(registry): validate reference choices and bound paged reads |
| `11d852202ab34935bff062493d3a2e8cefa50dbc` | feat(capital): preserve immutable effective-date revisions and as-of limits |
| `61acce321bdab2910bf4d9df11b28284b2589700` | feat(reporting): validate snapshot provenance and add safe monthly finance source |
| `d51246827360aa85063e853a732462cce263c083` | feat(limits): add transactional workbook import and generated-ID audit |
| `4e1748a2b5dd6203beb1851185ce8a51879710ca` | fix(period): commit workflow audit and recipient notifications atomically |
| `24b9351ef17a19f10e2afb21a1463ad2a4ce7595` | fix(auth): preserve opaque subject identity and owner isolation |
| `5b03a83bfe198677463e49d0cbd012de2a5de8c0` | fix(operations): verify schema readiness and expose safe worker diagnostics |
| `4083da135c99cee20f4efa3a0cf90b90655901be` | fix(audit): preserve stable actors in user administration |
| `28356cfd0698362a90280c17355700b9d5b68941` | feat(roles): maintain the database permission catalog safely |
| `fc32b69df495103d0e7013e6785bb87f2533e4c8` | test(performance): prove bounded paged user queries on SQL |
| `5123e2c77f7c449ff82fba88e505560dafcd9d54` | feat(localization): resolve business errors through runtime language packs |
| `23a5a787465479b8896f5b5a72da0577787b7275` | feat(localization): scope runtime controller and field messages per request |
| `0236d57fb6352c54cddefd3aeb01e78d86ffa8de` | feat(localization): cover service validation and workbook feedback |
| `4fb998a319caf881d27cbbb4b9983d97049d2095` | fix(registry): require explicit legal residency in JSON requests |
| `a524fd135251f9d6159097cd8b52042ac82eec3b` | feat(reporting): capture registry classification in new snapshots |
| `6de7cc7445637e2db6d06c6dc00c0ac1f4e07ccc` | fix(logging): redact period lock request identifiers |
| `d2689de9bddb0acc7229df06cb3d603ca21da457` | fix(import): reject codebook formulas before reading cell values |
| `82760bb513f6fa2efe2f9b970bb612b79d509e77` | docs: close backend verification and record deployment dependencies |

No integration operation is authorized by the commands in this document; approval is required before changing the integration/source branch history or pushing.
