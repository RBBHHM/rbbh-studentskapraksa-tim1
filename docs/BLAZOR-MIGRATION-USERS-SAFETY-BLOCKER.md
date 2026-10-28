# User administration safety blocker

2026-10-22. **RESOLVED** by the explicitly approved narrow backend safety fix.
This is not a completed native user-module migration. No SQL data, Keycloak
identity, schema or unrelated permission/workflow was changed.

## Historical discovery before the fix

The real GET `/api/users` returns `AppUser.Id`, a local database GUID. Identity
claims retain the Keycloak subject, stored separately in `AppUser.KeycloakId`.
These identifiers can legitimately differ. UsersController compares the incoming
route string directly with NameIdentifier/sub before resolving the local user.
Consequently the explicit self-deactivation/self-deletion guard is bypassed by
the local ID returned by the real list endpoint and used by the current client.

Isolated HTTP tests exercise real UsersController, AppUserService, AuditService,
RoleClaimsTransformation and application authorization with one synthetic active
database Admin whose local ID differs from the synthetic Keycloak subject:

| Request using the listed local ID | Actual response/state | Next administration GET |
|---|---|---|
| DELETE `/api/users/{id}` | 200; local row remains but IsActive=false | 403 |
| DELETE `/api/users/{id}/permanent` | 200; local row physically removed | 403 |
| PUT `/api/users/{id}/role` with roleIds=[] | 200; local user and role assignments removed | 403 |

The first two cases assert the controller's existing intended 400/self-protection
contract and fail. The third is a passing characterization of current destructive
behavior, **not approval** of a last-Admin removal policy. Empty roles already
mean removing the local application mapping; this must not silently be redefined
for ordinary users. Provider roles cannot restore application permissions: the
current database-backed transformation remains authoritative.

## Historical red reproduction

At discovery the uncommitted file was:
`RelatedPartiesRegister/IntegrationTests/Tests/Users/UsersSelfRemovalDiscoveryTests.cs`.

Run `dotnet test RelatedPartiesRegister/IntegrationTests/IntegrationTests.csproj
--no-restore --filter FullyQualifiedName~UsersSelfRemovalDiscoveryTests`.
Receipt: ignored `artifacts/migration-template/users-self-removal-discovery.log`.
Result: **2 FAIL / 1 PASS**, no skips. Assertions were not weakened. This discovery
file was deliberately not committed pending the safety decision. It has now been
replaced by UsersSafetyHttpTests, retaining and expanding the protection checks.

The test host uses unique EF InMemory stores and real HTTP authorization. It does
not start backend Program/.env, SQL, jobs, SMTP or actual Keycloak. Its injected
Keycloak client rejects every outbound request. Only synthetic data is mutated.
The accepted Audit checkpoint immediately before discovery has 407/407 .NET and
122/122 browser passes; those are historical checkpoint results, not a claim that
the new red discovery tests pass.

## Approved remediation

The user explicitly approved these rules after the red discovery:

1. Resolve the actual local target before comparing it to the authenticated
   user's identity, preserving the existing prohibition on self-deactivation and
   self-deletion across supported route-ID forms.
2. Define and enforce preservation of at least one active database Admin when
   deactivating/deleting users or replacing/removing role assignments, including
   the empty-role route. Concurrency/atomicity must be considered, not just UI
   button disabling or an unprotected count-before-write.
3. Preserve all other current role workflows and the separation of Keycloak
   identity from database-owned application permissions. Add backend regressions
   before resuming the native Users module.

Self-deactivation/deletion now resolves the persisted application target before
comparing its KeycloakId or the current authorization email link with authenticated
identity claims. Both local GUID and Keycloak subject route forms are covered.
Self rejection remains safe HTTP 400. No request ID or provider role grants access.

Role replacement/removal, deactivation and permanent deletion reject removal of
the final active database Admin with safe HTTP 409 and a business explanation.
Only active users with active assignments to the active admin role count. Other
Admin/ordinary-user operations, including non-final empty-role mapping removal,
reactivation, role validation and not-found results retain their current meaning.

SQL Server uses a transaction-owned, database-scoped exclusive sp_getapplock
before rereading the target/admin set, changing data and writing audit. Every API
replica uses the same resource. The transaction runs inside the existing EF
execution strategy; a failed lock acquisition returns safe 409 without mutation.
The existing Development/test InMemory store uses an in-process async gate because
it has no transactions. Unsupported providers fail closed. No schema is needed.
This coordinates these API operations, not arbitrary out-of-band SQL writers.

## Green evidence

- UsersSafetyHttpTests: **28/28 real HTTP integration cases**, including mapped
  and raw OIDC claim forms, email-link self protection, both role routes, empty
  roles, final-Admin deactivate/delete by a non-self DB-authorized writer,
  inactive-user/assignment exclusions, preserved ordinary/other-Admin operations,
  exact allowed audit metadata and forged-provider denial.
- A deterministic two-request barrier obtains both authorization snapshots before
  concurrent self-role removals; one succeeds, the other is rejected and one
  active Admin/audit remains. No sleeps, retries or weakened assertions.
- Full backend: **210 unit + 95 integration = 305/305 PASS**; including frontend:
  **435/435 .NET PASS**, no skips.
- Five browser safety cases call the real protected Users endpoints through the
  isolated test host and verify native Audit remains accessible after rejection,
  and displays the actual allowed other-Admin mutation. These are not a claim
  that the native Users CRUD page is already complete.
- Full browser gate: Admin32 + gost15 + kapital23 + PL19 + limiti17 + FL21 =
  **127/127 PASS**, workers=1/retries=0; receipts are
  `artifacts/migration-template/users-safety-browser-*.log`. Changed-C# format
  verification and diff checks also pass. Existing formatter workspace-load
  warning and Pkcs NU1902/NU1903 advisories remain unresolved.

Reproduce focused tests with the IntegrationTests project and
`--filter FullyQualifiedName~UsersSafetyHttpTests`; full tests use
RelatedPartiesRegister.Migration.slnx. This run uses
`--artifacts-path artifacts/users-safety --no-restore` to avoid replacing a running
local host's locked executable. Browser uses BROWSER_SAFETY_BUILD=true and the
same isolated build. No running user host was stopped or restarted.

Live SQL Server multi-replica lock/rollback acceptance remains an integrated
environment gate; local HTTP/concurrency tests use InMemory and do not prove SQL
deployment configuration. At this historical checkpoint native Users could resume;
period/code-list administration and guide were incomplete. Their subsequent local
closure is recorded in REACT-TO-BLAZOR-FINAL-HANDOFF.md; integrated deployment
remains gated. React implementation/deployment were unchanged; no real records
were removed or modified. The historical Pkcs warnings above are now locally
remediated in BLAZOR-MIGRATION-COMPLETION-SECURITY.md.
