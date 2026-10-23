# Related Parties Register — native Blazor handoff

2026-10-23. **LOCAL PARITY VERIFIED; production cutover BLOCKED.**
This is an honest handoff of a side-by-side native implementation, not a claim
that live SQL/SSO/OCP acceptance has passed. Never deploy two canonical frontends.

## 1. Original React routes

`/` → `/app`; `/app/physical-persons`, `legal-persons`, `limits`, `capital`,
`reports`, `notifications`, `admin/users`, `admin/period`, `admin/code-lists`,
`admin/audit`, `profile`, `guide`. Full source contracts are in
BLAZOR-MIGRATION-INVENTORY.md, inspected baseline `68d3e7f`.

## 2. Native Blazor routes

All listed `/app` paths exist under src/Web.Blazor/Components/Pages.
`/` also opens the dashboard; `/profile` remains a template alias. Error/not-found
pages and FocusOnNavigate remain template-owned. No business endpoint was deleted.

## 3. Module parity matrix

See BLAZOR-MIGRATION-PARITY.md and individual module closure reports. PARITY means
local API/behavior/browser proof, not pixel reproduction or live environment
approval. Local module flows are closed; real SSO acceptance remains explicitly
BLOCKED in ledger item29, not reclassified as local browser success.

## 4. Backend changes

Approved report read/download versus persisted-generation policy; DB-only grant
transformation, including provider-claim stripping/revocation; specific root
business-validation HTTP400/privacy handling; self/final-active-Admin protections;
period response10..490 storage guard; two code-list mutation error branches that
now return actual400/404 rather than serializing ActionResult as200. Each change
has focused regression evidence. API/BL/DL architecture and remaining workflows
are preserved. No backend administration was moved to Blazor.
The final security pass also pins transitive Pkcs to patched9.0.21 and restricts
JWT/OIDC HTTP metadata to real Development loopback. See
BLAZOR-MIGRATION-COMPLETION-SECURITY.md; no EF/TFM/schema or role-store redesign.

## 5. API contract changes

Domain payload shapes/endpoints remain source-backed. Intentional status/policy
corrections above are documented. Native typed clients handle actual envelopes,
nullable capital and backend-generated files, not generic legacy-object guessing.
The approved platform transport extension adds multipart and bounded binary
responses through the existing private token session; no financial/Excel logic
was copied into the frontend.

## 6. Authorization mapping

Keycloak = identity. Current application DB = application roles/permissions.
Baseline Gost read/export remains as defined by that DB. FL/PL/Limiti/Capital
mutations follow actual backend permission codes; all four Admin UI routes retain
the DB Admin guard. Reports read/download = authenticated; generate/persist =
KAPITAL_CREATE (Kapital/Admin), an approved side-effect exception. The existing
period API CapitalRead/Write distinction is not silently replaced by reporting
read or by its narrower Admin UI guard. See the authorization matrix.

## 7. Localization

App-owned BS/EN resources through RBI.Localization, BS default, persisted culture
cookie and actual backend Accept-Language. No inherited DE product commitment,
i18next providers or business translation packs inside DesignSystem. Business
codebooks and optional RBI.Localization.Sifrarnik remain distinct concepts.
Two completion tests additionally verify equal BS/EN key sets, no duplicates or
empty values, actual platform catalog discovery/default, missing-key behavior and
unsupported-culture fallback. This is not a hardcoded duplicate runtime registry.

## 8. Design system usage

Genuine finalized rbbh-webapp generation with auth/authz/localization. Runtime
RBI.DesignSystem/Hosting/ApiCore/Authentication/Authorization/Forms/Localization
are NuGet dependencies; no copied platform implementation or source references.
Native tables, dialogs/alert dialogs, fields, controls, validation and layout
compose application-specific features. String/int/GUID selects are used;
nullable business booleans use typed radio groups because the known upstream
boolean-select conversion finding is not fixed by this migration.

## 9. Deployment changes

No active deployment workflow changed. Current React static/IIS packaging remains
the retained deployment reference; actual production consumers are not proven
retired. The candidate is a server-hosted .NET10
interactive Blazor app, not a static React artifact. Its replacement deployment
must serve published assets and interactive circuits through the approved proxy,
with the organization's shared-session/cache/DataProtection model. Nested template
workflow examples are not active root CI. Do not activate them without review.
The explicit native candidate script now publishes an app/manifest/ZIP with file
hashes, refuses overwrite and has an actual published-DLL startup probe. See
BLAZOR-MIGRATION-RETIREMENT-AND-DEPLOYMENT.md for tested commands, inventory and
the operator-controlled replacement/rollback procedure. A local ZIP is not cutover.

## 10. DB impact

No EF entity, migration or SQL schema change. Existing SQL nvarchar(500) response
storage is preserved through validation, never truncation. Existing migrations,
indexes and startup migration/seeding policy remain. Tests use unique InMemory
stores; they are not SQL-length/transaction/concurrency deployment proof. The
users mutation boundary requires its documented live SQL app-lock acceptance.

## 11. Keycloak impact

No realm-role redesign or frontend Admin API. Confidential server-side OIDC,
private session/token propagation and current backend identity mapping remain.
Actual login/logout/refresh/audience and user lookup require an approved test
realm/environment; isolated test identity cannot certify them. No credentials
were read into chat, committed or logged.

## 12. Test totals

**528/528 .NET**: 203 frontend + 216 backend unit + 109 integration, no skips.
Backend total is325. The completion batch adds2 localization and6 metadata tests.
The period focus suite is21/21; five exact boundaries are regression-protected.
Fresh focused real-HTTP safety regression is61/61: users28 + period21 + reporting12;
receipt completion-focused-safety.log. These are included in109 integration tests,
not added again to the528 total.
**215/215 browser**: Admin 70, gost 25, Kapital 33, PL 29, Limiti 27, FL 31;
workers=1, retries=0. Final per-role JSON stats have 0 failures/flaky/skips.
The added six-role matrix verifies actual controller authorization, not only UI
visibility, and makes no business writes. All prior module assertions remain.

Historical a24e239 checkpoint:520 .NET/209 browser. Its first FL attempt had
29 PASS/1 FAIL due to the confirmed identity-blur fixture setup race;
its unchanged business assertions pass after using the existing settled setup
helper. This was not an assertion relaxation or a product-code change. The
original failure log/trace remain alongside final receipts.

Build with analyzers, changed-new-C# format verification and Release publish PASS.
An actual published DLL was started on a temporary loopback port with explicitly
empty identity/API config: 14 routes 200 and honestly signed out/BS, RBI CSS 200,
unconfigured login 404. Only that owned smoke process was stopped; user hosts
were untouched. No SQL or identity service was contacted.

Current receipts: artifacts/migration-template/{completion-restore,
completion-build,completion-regression,completion-publish,completion-startup,
completion-format}.log and completion-browser-{admin,gost,kapital,PL,limiti,FL}.log.
Earlier final-* and period-bound-focused receipts remain historical evidence.
JSON/screenshots/traces are
under artifacts/migration-browser by role. The formatter retains its existing
workspace-load warning; the fresh analyzer build has0 warnings/errors. The prior
Pkcs NU1902/NU1903 advisories are fixed by9.0.21, with no warning suppression.
No production security-release clearance is implied.

## 13. E2E evidence

Real protected controllers/services/repos/audit and unique InMemory DB profiles
behind a test-only loopback host. Existing module specs plus
shell-profile-guide.spec.ts exercise all required business flows; workers1,
retries0. Never execute backend Program/.env, SQL, live SSO, SMTP or jobs in
browser tests. See the parity ledger for every required flow and environment gap.

## 14. Accessibility

WCAG2A/2AA/2.1AA Axe scans, native labels/errors, keyboard/focus return, dialog
behavior and guide disclosure keyboard checks. Animations settle before unchanged
scans; no assertion weakening, forced-click bypasses or timeout increases.
This is automated/local evidence, not a complete assistive-technology audit.

## 15. Responsive evidence

Desktop1440 and375px module coverage; critical tables scroll horizontally rather
than forcing page overflow. Scoped host dialog border-box/control sizing, inner
overflow assertions and inspected mobile screenshots cover native forms. Receipt
screenshots/traces are under ignored artifacts/migration-browser by role.

## 16. Intentional changes

Approved reporting side-effect policy, database-authoritative grants, user safety
guards, period10..490 input bound, safe redacted failures, real code-list failure
HTTP statuses, explicit unknown-save recovery, and correct source-backed guide
wording. Native components are not intended to reproduce React pixels. Source
business decisions, backend calculations and import partial-row semantics remain.

## 17. Remaining release blockers

Owner-provisioned/approved Keycloak confidential client and issuer/audience/redirect
settings; actual SSO/login/logout/refresh and identity↔application-user acceptance;
approved shared IDistributedCache registration/provider and shared DataProtection;
live SQL concurrency/constraints and existing migration policy acceptance; SMTP
delivery; private publication of approved RBI transport packages; OCP/proxy/circuit
deployment and replacement-workflow proof; corporate dependency/runtime servicing
acceptance. The Pkcs NU1902/NU1903 findings are locally fixed, not still unresolved.
Existing OCP insecure certificate verification/UAT Development selection and
forwarded-header network trust require explicit deployment acceptance, not reuse
as approved native production defaults. No credential guessing, live destructive
data tests or production cutover is authorized by local green tests.

## 18. Removed React dependencies/files

**None removed from src/Web or active deployment.** Its new MIGRATION_REFERENCE
marker is the only React-tree change in final completion. Native src/Web.Blazor has no
React/TanStack/Radix/i18next/Tailwind/keycloak-js dependency. Retiring/archive of
the old frontend is deferred until parity/environment acceptance; preserving
reference source is intentional, not two active production implementations.
The274 previously tracked files and exact package/tool consumers are inventoried
in BLAZOR-MIGRATION-RETIREMENT-AND-DEPLOYMENT.md. Current legacy keycloak-js is
absent; no imaginary dependency removal is claimed.

## 19. Repository structure

RelatedPartiesRegister/{API,BL,DL,...} and existing backend tests remain intact.
src/Web is the React reference; src/Web.Blazor contains Components/{Layout,Pages,
Registry}, Api, Security, Localization, wwwroot and Program.cs. tests contains
Web.Blazor.Tests, Web.Blazor.Browser and test-only Web.Blazor.BrowserHost.
RelatedPartiesRegister.Migration.slnx groups local migration verification; docs
contains inventory, authorization, parity, closure and handoff. artifacts is
ignored. No frontend dependency on backend EF/ClosedXML projects.

## 20. Local run

See src/Web.Blazor/README.md. Restore with an explicit approved local/private feed
and isolated package cache (no machine paths committed). ApiCore/Authentication
0.1.1-migration.1 consume approved platform commita38b94c; other runtime packages
remain0.1.0. Use isolated --artifacts-path when a user host locks default output.
Missing auth/API in Development is honestly signed out, with no login handler or
outbound anonymous proxy. No fake production account is supplied.

## 21. Production deployment procedure — gated, not executed

Provision approved packages, server runtime/proxy/circuit settings, confidential
OIDC secrets via approved secret storage, HTTPS backend URL ending in '/', shared
cache/provider registration and DataProtection. Missing auth outside Development
fails startup; do not bypass that gate. Validate real issuer/audience/session,
SQL and SMTP workflows, final-Admin concurrency, browser/accessibility/responsive
flows and secure logs in UAT. Obtain owner/security/deployment approval. Only then
prove/activate the replacement frontend workflow, switch the canonical route and
archive/remove React-only dependencies and static packaging in a separate bounded
cutover commit. Keep rollback to the accepted artifact; never silently reseed or
migrate production data, and never run both frontends as canonical production apps.
