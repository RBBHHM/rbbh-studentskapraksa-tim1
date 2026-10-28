# Native user administration checkpoint

Historical module checkpoint. Current2026-10-26: empty roles retain AppUser,
status and subject metadata; active zero-role users have virtual Guest access.
Guest is not assignable, disabled users stay denied, and final-Admin continuity
counts only active confirmed subjects. See the current final handoff/runbook.

2026-10-23. Local module closure only; no deployment cutover or real SSO/SQL claim.
The approved backend safety change is separately committed as
`de31a7ca5ca5841e93e550463f7abc5ac89c7b6b`.

The native `/app/admin/users` page consumes the real `{users,total,page,pageSize}`
and `{roles}` contracts through the normal RBI typed transport, server-side token
session and application culture handler. There is no copied platform UI,
frontend database access, provider-role editor or generic metadata CRUD layer.
The source table fields are retained: names/username, email, application roles,
effective permissions and active state. Native paging exposes the existing API's
20-row pages beyond the old screen's first page; it does not invent a new query.
Unknown returned role labels remain encoded source text rather than fake grants.

The old Admin route restriction is preserved using the actual database-backed
profile, with Administration view/manage permissions controlling API use/actions.
Provider Admin/permission/marker claims cannot open the route or real API for any
of the five other current DB roles. The backend remains the authorization and
last-Admin safety authority. Hiding own deactivate/delete buttons is additional
UX only; direct protected HTTP requests remain independently rejected.

Creation retains required names, the corporate email domain, at least one actual
catalog role ID, multiple independent accesses and the source's active=true.
Role editing changes roles only, not identity fields. Empty roles retain the
existing removal-of-local-mapping/Guest behavior for non-final users, with an
explicit explanation. Final-Admin removal is rejected in the form without losing
access. No password, direct Keycloak role or invented permission is submitted.

Deactivate/reactivate and permanent delete use the existing endpoints and native
confirmation dialogs. Cancel does not mutate. The permanent-delete wording
distinguishes the local application mapping from the Keycloak identity, which
these backend endpoints do not delete. Successful changes reload actual records
and preserve backend audit actions/actors. Errors use safe owned BS/EN wording,
not upstream raw exception/SQL details. Mutations are not automatically retried.

## Evidence and bounded corrections

- **145 frontend + 210 backend unit + 95 integration = 450/450 .NET PASS**,
  no skips. Fifteen frontend cases added: typed list/catalog/create/mutation and
  safe-error contracts, draft validation/multi-role/removal semantics, and a
  signed-out route. All28 backend users-safety HTTP cases remain included.
- Nine native Admin browser cases cover accessibility, actual create/reload and
  duplicate rejection, required/domain/role validation without requests,
  role replacement and empty-role removal, last-Admin conflict, confirmation
  cancel/deactivate/reactivate/delete and exact audits, server paging, outage
  recovery, 375px and BS/EN. Existing five safety browser cases remain intact.
- Denied/forged-provider coverage additionally runs once for each of the five
  other current DB roles. Full totals: Admin41, gost16, kapital24, PL20, limiti18,
  FL22 = **141/141 browser PASS**, workers=1/retries=0.
- Mobile visual inspection caught an inner dialog overflow that page-only width
  checks missed. App-owned role-choice grid and scoped border-box sizing on this
  form/dialog fix it without changing platform or unrelated component geometry.
  The test now also checks dialog bounds, body overflow and input bounds. The
  final mobile image was inspected; long forms use the native dialog scroll area.
- A direct automated click on the styled checkbox's one-pixel native input was
  intercepted by its decorative control. Tests use the real label pointer path
  and native keyboard Space, assert visibility/enabled state and exact checked
  state, and verify persisted results. No force click or assertions were removed.
- An initial Axe scan during dialog fade-in observed temporary reduced contrast.
  It now waits for the actual Web Animations completion and opacity=1 before the
  unchanged full Axe scan. There are no sleeps, added retries/timeouts, hidden
  content or color/contrast overrides. Failure receipts remain in ignored logs.

Full .NET/browser receipts are `artifacts/migration-template/users-native-*.log`;
browser JSON/traces/images are under `artifacts/migration-browser/`. Use the
isolated `--artifacts-path artifacts/users-safety --no-restore` build and set
BROWSER_SAFETY_BUILD=true for browser reproduction. Running user hosts are not
restarted. Changed-C# format verification, Release publish and diff checks pass;
the existing formatter workspace warning and Pkcs NU1902/NU1903 advisories remain
unsuppressed and unresolved.

The test executable runs real Users/Role controllers, AppUserService/RoleService,
database permission transformation and AuditService on unique InMemory stores.
Keycloak administration is explicitly disabled there and its HTTP client rejects
outbound calls. This proves the current local backend branch, not an integrated
realm lookup: when enabled, the existing backend looks up an already-existing
Keycloak identity before creating the local mapping. No realm account was created,
and no real SQL, SMTP, Keycloak credentials or business records were used.
Remaining period/code-list/guide modules and environment gates are still open.
