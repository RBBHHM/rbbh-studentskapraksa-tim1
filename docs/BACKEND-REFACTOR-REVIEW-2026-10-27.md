# Backend implementation review — 2026-10-27

Baseline: `1147e97fdb9c96a0df938f9337bc93a3ef74464e`. Findings recorded before implementation. This is an implementation review, not permission to change business behavior. No production data, schema or deployment changes are planned.

## Findings and decisions

| Category | Finding | Decision |
| --- | --- | --- |
| HIGH_VALUE_REFACTOR | UsersController (267 lines) owns target resolution, self/final-Admin rules, EF writes, mutation boundary and audits. | Move existing operations into the existing AppUserService interface/implementation. Keep HTTP mapping in controller and keep UsersMutationBoundary unchanged. Preserve validation order, aliases, audit payloads and reactivation lookup semantics. |
| CONSISTENCY | Actor email is already centralized, but UsersController repeats subject/local-ID extraction. | Extend the existing AuthenticatedActor helper with a small immutable snapshot. No policy decisions, fabricated identity or new DI framework. Reuse subject extraction in resolver/profile without changing caller-specific normalization. |
| DUPLICATION | User mutation audit metadata repeats table, actor email and IP. | Add a focused AuditEntry factory; keep JSON payloads and action names explicit at operation sites. Subject-link and scheduler audits have different identities and remain explicit. |
| PERFORMANCE | Final-Admin lookup materializes all qualifying IDs although only zero, one or multiple matters. | Bound distinct result to two inside the existing serialized transaction. Keep confirmed-subject, active-role and active-assignment predicates. |
| PERFORMANCE | Relationship tree repeatedly scans the ordered people list for each node. | Build one ordered parent lookup; retain ancestor traversal, cycle leaves, branch order, active filtering and one DB query. |
| PERFORMANCE | User listing computes permission DTOs for every candidate before paging. | Compute DTO/permission lists only for selected page. Keep all three reads, in-memory culture-sensitive sorting and ordinal-ignore-case role filtering. No SQL collation substitution. |
| DUPLICATION / TESTABILITY | AutoLockHostedService duplicates the already tested last-working-day helper. | Use AutoLockHelper with identical date calculation. Keep scheduler times, system actor, notification ordering and persistence unchanged. Reuse existing UsersHttpFixture for added HTTP contracts. |
| DEAD_CODE | DeactivateUserResponseDTO has no references; controller returns its existing anonymous message. | Remove unused DTO only. Remove unused controller dependencies/usings as part of the service move. |
| READABILITY | Global period-lock repository deliberately keeps historical Department rows as fail-closed markers. | No behavior change: null Department is canonical; historical rows cannot grant unlock access. DepartmentHelper/department arguments are already absent. Keep migrations and compatibility column. |
| NO_ACTION | Audit paging, reports, code-list dropdowns, legal paging and limit reads already filter/page or project appropriately. | Preserve Count + page queries (total is part of contract), report snapshots and export join cardinality. No speculative cache/index/schema changes. |
| NO_ACTION | RoleClaimsTransformation owns fresh DB grants; RolePermissionMatrix owns seed/virtual Guest baseline. | Keep DB permissions authoritative and recomputation on each transform. Do not replace DB grants with static Resolve or cache authorization. Unknown authenticated identities retain current virtual Guest behavior; inactive identities remain denied. |
| NO_ACTION | Existing ProblemDetails, Result and errors-array responses differ. | Preserve wire shapes/messages. Map typed user-operation outcomes at HTTP boundary. Do not globally normalize errors. Add HTTP contract tests before moving logic. |
| NO_ACTION | Excel paths parse once, bound imports to 5,000 rows and enforce upload sizes; exports intentionally build complete workbooks. | Keep formats, validation order, partial-row import behavior and memory copies needed for byte-array response. No export truncation or streaming redesign. |
| NO_ACTION | Existing email notifications follow persistence; broadcast failures are best-effort, HTTP email catches transport timeout/failure. | Preserve failure policy; do not mechanically propagate request cancellation after persistence or turn email failure into failed business mutation. Dispose HTTP email responses. Broader cancellable mail/export interfaces require a separate contract decision. |
| NO_ACTION | Startup already groups registrations and fails closed for absent production audience/database. | Keep registration/config behavior. Named example HTTP clients, module/reference endpoints and compatibility assembly naming are potential legacy debt, not proven safe external-contract removals. |
| NO_ACTION | Backend packages are used by runtime, EF tooling or explicit security pin. | No upgrade/removal. Keep System.Security.Cryptography.Pkcs 9.0.21 and all vulnerability warnings enabled. |
| NO_ACTION | TransactionHelpers is only test-referenced; legacy compatibility methods/DTOs may be public contracts. | Do not delete tested helpers or endpoints just because the native UI has no caller. No migration history rewrites. |

## Review scope

Production inventory covers controllers and service interfaces; users/roles/permissions; physical/family and legal registries; capital/limits/reports; code lists; period/unlock repository, middleware and scheduler; audit; SMTP/HTTP/demo email and templates/store; workbook helpers/validators; actor/identity/JWT/OIDC/policies; exception/Result mapping; DbContext partials, entity mappings/DTOs/seeds/factory; startup/IoC/HTTP/health/logging/security; package references and migration operations/snapshot. Generated migration designers are historical schema evidence, not refactoring targets. Backend HTTP and SQL fixtures are retained rather than replaced by a generic test framework.

## Verification plan

Before the service move: focused HTTP response/audit compatibility tests. After changes: backend unit/integration tests, actual LocalDB safety/concurrency tests, full .NET solution, analyzers, format verification, all six native browser roles with one worker and zero retries, and `git diff --check`. Skips must be reported separately. Use isolated build artifacts without stopping or replacing preview processes. Preserve the existing untracked BrowserHost launchSettings.json byte-for-byte. No push.

## Implemented changes and measurements

- UsersController: **267 → 134 lines**, four constructor dependencies → one. Target resolution, self checks, final confirmed-active-Admin validation, role writes, destructive-mutation locking and audits now live in AppUserService.Mutations (143 lines). Existing AppUserService read/create partial: 185 → 177 lines. This moves responsibility; it does not claim a net reduction in service code. No new repository, DI service, cache, lock or middleware.
- The existing SQL execution strategy, transaction-owned `sp_getapplock`, resource name, zero lock timeout, tracker clearing, transaction scope and audit placement are unchanged. Validation precedence remains invalid roles → missing target → final Admin → self-role rejection. Reactivation remains outside the destructive boundary and retains its existing local-GUID lookup behavior.
- AuthenticatedActor now captures raw `sub`/mapped subject, trimmed authenticated email and the recomputed application user ID. UsersController no longer resolves claims itself. Resolver still trims subject; profile still returns raw subject; display claims stay presentation data. RoleClaimsTransformation reuses the application-user-ID claim constant. No permission/Guest/subject-link policy changed.
- Four user mutation audit sites use AuditEntry.ForActor for common metadata. JSON serialization, raw route RecordId, action names, nulls and old/new values remain visible in operations. Other families, subject-link's subject-based UserId and scheduler's deliberate system actor stay explicit. The existing audit service interface and persistence behavior are unchanged.
- Final-Admin query: **one DB round trip before/after**, all qualifying distinct IDs → **at most two IDs**. Multiple admins always permit removal of one; a single result is checked against the target. Existing SQL tests verify confirmed subjects, competing requests, invariant retention and lock release.
- Users listing: **three DB reads before/after**. Candidate identity/role reads remain unchanged. DTO/effective-permission construction changes from every candidate to **at most the selected page (maximum 200)**. Total count, culture-dependent sort, case-insensitive role filter and inactive-user permissions remain unchanged. This is an allocation/computation improvement, not a claim of SQL paging or reduced DB traffic.
- Relationship tree: **one DB read before/after**. Repeated full-list child scans are replaced with one ordered parent lookup. Ancestor traversal, path copies/cycle termination and active/orphan behavior remain unchanged. Lookup construction/edge selection avoids repeated N-row scans; deep-path copying costs remain. No timing/latency benchmark claim.
- AutoLockHostedService: 173 → 165 lines; duplicate last-working-day algorithm removed in favor of the existing tested AutoLockHelper. Schedule, global period state, system audit and best-effort notifications remain unchanged.
- HTTP email responses are disposed after use; send/error policy is unchanged.
- Deleted one unreferenced six-line DeactivateUserResponseDTO (no route, response annotation or caller). Removed unused UsersController dependencies and AppUserService's unused Mapster using. No tested compatibility helper, endpoint, package, migration or schema was removed.
- Added **12 tests**: seven user contracts/cancellation cases, two actor snapshot cases and three relationship-tree cases. Reused UsersHttpFixture and InMemoryContextFactory; no generalized test framework or assertion weakening. The first five wire/audit characterizations passed against the pre-move implementation; tree and user-page characterizations also passed before their query refactor.

## Deliberate nonchanges / remaining debt

1. User list candidate loading and in-memory sorting remain unbounded to preserve current culture/collation behavior. A future SQL paging change needs explicit ordering compatibility requirements, not just a benchmark.
2. PeriodLockController (503 baseline lines), CodeListController (443), RelatedPersonService (739) and LegalEntityService (523) still have broader responsibilities. This batch does not split them merely for line count. Existing validation ordering, historical global-lock reconciliation and partial-success imports remain intact.
3. Physical/family parent validation follows ancestors with repeated queries; replacing it with a full-table snapshot would change concurrency/read timing and can cost more on small trees. Report daily/monthly construction and legal create/update validation have duplication, but abstraction alone offers little benefit here. No speculative rewrite.
4. Period broadcasts currently request 500 users from a listing capped at 200. Changing recipient selection would change observable notification behavior; record this for a separately approved correction rather than silently altering it in a behavior-preserving pass.
5. Cancellation now flows from RequestAborted through moved user mutations to EF and the existing lock/transaction. A pre-cancelled mutation test proves no write/audit. Read/create, mail and synchronous workbook interfaces have not been mechanically rewritten. Existing best-effort email catches include transport timeouts; changing post-persist cancellation/failure semantics needs a separate decision. Audit's existing save interface remains unchanged.
6. KeycloakAdmin compatibility methods and example named HTTP clients have little/no current production call evidence, but deleting them would remove public/internal integration surfaces. Realm-role fetching still performs per-user calls in the administration compatibility client; this never becomes application authorization. No fake identity or static permission substitution.
7. Result/ProblemDetails/custom errors arrays and the alternate ProblemDetails configuration remain legacy consistency debt. A global rewrite could change response contracts; only users result construction was consolidated. Production error/notification policy was not changed.
8. Package review removed **zero references**: runtime imports, provider/EF tooling, logging/metrics/OpenAPI and ClosedXML/Mapster all have uses. PKCS 9.0.21 is an intentional direct security override even though SqlClient also brings it transitively. No package upgrades or NU1900/NU1902/NU1903 suppression.
9. Whole-solution format verification reports **325 WHITESPACE diagnostics in 29 untouched files**, including two frontend files. All diagnostic paths were compared against git's changed-file list: zero overlap. They are existing formatting debt; no mass reformat of unrelated code or frontend was performed. Changed/new C# format verification passes.

Business behavior, authorization/Guest/reporting policy, self/final-Admin safety, global lock, identity binding/schema, API routes/status/payload/localization, workbook formats and deployment architecture are preserved. Request cancellation is now honored during moved users EF operations as requested. Frontend production/test source, snapshots, package versions, database models and migrations are unchanged. No production F3 backfill or real SSO/database operation was executed.

## Fresh verification receipts

Isolated outputs: `artifacts/backend-refactor/` (ignored). Restore uses the existing isolated NuGet config/package cache; no global configuration changes. Tests use only generated disposable LocalDB databases.

| Gate | Result |
| --- | --- |
| Backend unit | 270 passed, 0 failed, 0 skipped |
| Backend integration | 156 passed, 0 failed, 0 skipped |
| Backend total | 426 passed (includes the seven SQL tests below) |
| Full .NET solution | 636 passed: 270 + 156 + 210 Blazor tests; 0 skipped |
| SQL/LocalDB | 7 passed: six last-Admin/concurrency/lock tests and one subject-link upgrade/audit test; included in integration total |
| Analyzer build | PASS, zero warnings/errors |
| Changed/new C# format verification | PASS |
| Whole-solution format verification | Existing whitespace debt above; exit 2, not reported as PASS |
| Native browser Admin | 77 passed |
| Native browser Guest | 27 passed |
| Native browser Kapital | 40 passed |
| Native browser PL | 35 passed |
| Native browser Limiti | 31 passed |
| Native browser FL | 37 passed |
| Native browser total | **247 passed, 0 failed, 0 skipped, 0 flaky**, workers=1, retries=0 |
| git diff --check | PASS |

Browser JSON/log receipts are under `artifacts/backend-refactor/browser/`; final full-solution TRX receipts under `artifacts/backend-refactor/results/final/`. The seven SQL results were individually checked in the integration TRX, not inferred from a prior run. Browser coverage includes user safety/CRUD, revocation, authorization, period lock, reports/exports, imports, audit, BS/EN and responsive behavior. No screenshot baseline was changed; this is not a new visual-parity acceptance claim.

Source commits (all local, not pushed):

1. `a5265fb6d1629bac5af833b5a4234cecddb57e7d` — `refactor(users): move guarded mutations behind service boundary`
2. `9b7338a6f4b8f7346422f5f69a353f8ca4f9988e` — `perf(data): index relationship tree children without changing traversal`
3. `b2019b11eedc6c27b15df00bcafacd25f90139ac` — `refactor(backend): reuse lock date helper and release email responses`

This report is committed separately after those source commits. The final documentation commit/branch HEAD is reported in the handoff message and git history.

Preserved user file: `tests/Web.Blazor.BrowserHost/Properties/launchSettings.json`, still untracked, SHA-256 before/after **07DAA11FF2D5D953B78D8FE4C7209AF625C3D76F2FBEC747986EB6F8DF39EC93**. No frontend source, browser harness/config/assertion, snapshot, package version, database model/migration, real production data or authentication setting changed. The only remaining untracked file is this existing user file. No push.

Commands (repository root unless indicated):

```powershell
dotnet restore RelatedPartiesRegister.slnx --artifacts-path artifacts/backend-refactor --packages artifacts/migration-template/nuget-cache --configfile artifacts/migration-template/NuGet.config
dotnet build RelatedPartiesRegister.slnx --artifacts-path artifacts/backend-refactor --no-restore -p:RunAnalyzers=true
$env:RBBH_TEST_SQLSERVER='Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true'
dotnet test RelatedPartiesRegister.slnx --artifacts-path artifacts/backend-refactor --no-restore --no-build --results-directory artifacts/backend-refactor/results/final --logger 'trx;LogFilePrefix=final'
$env:ArtifactsPath = Join-Path (Get-Location) 'artifacts/backend-refactor'
$env:UseArtifactsOutput='true'
dotnet format RelatedPartiesRegister.slnx --verify-no-changes --no-restore
# Also verify only the exact changed/new C# paths with --include.
# In tests/Web.Blazor.Browser, for each of admin,gost,kapital,PL,limiti,FL:
$env:BROWSER_ARTIFACTS_PATH='../../artifacts/backend-refactor'
$env:BROWSER_FIXTURE_URL='http://127.0.0.1:5288'
$env:BROWSER_FIXTURE_ROLE='<role>'
npm.cmd test -- --workers=1 --retries=0
git diff --check
```
