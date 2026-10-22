# User administration safety blocker

2026-10-22. Discovery checkpoint, not a completed user-module migration.
No production backend behavior, SQL data, Keycloak identity or schema changed.

## Confirmed existing behavior

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

## Reproduction and state

Uncommitted discovery file:
`RelatedPartiesRegister/IntegrationTests/Tests/Users/UsersSelfRemovalDiscoveryTests.cs`.

Run `dotnet test RelatedPartiesRegister/IntegrationTests/IntegrationTests.csproj
--no-restore --filter FullyQualifiedName~UsersSelfRemovalDiscoveryTests`.
Receipt: ignored `artifacts/migration-template/users-self-removal-discovery.log`.
Result: **2 FAIL / 1 PASS**, no skips. Assertions were not weakened. This discovery
file is deliberately not committed pending the safety decision; a full test run
of the current working tree must not be described as green.

The test host uses unique EF InMemory stores and real HTTP authorization. It does
not start backend Program/.env, SQL, jobs, SMTP or actual Keycloak. Its injected
Keycloak client rejects every outbound request. Only synthetic data is mutated.
The accepted Audit checkpoint immediately before discovery has 407/407 .NET and
122/122 browser passes; those are historical checkpoint results, not a claim that
the new red discovery tests pass.

## Decision required before destructive user workflows

Recommended narrow scope, subject to explicit approval:

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

No such business-policy change has been implemented without approval. Users is
BLOCKED; period administration, code-list administration, guide and integrated
environment/deployment gates remain incomplete. React/deployment are unchanged.
