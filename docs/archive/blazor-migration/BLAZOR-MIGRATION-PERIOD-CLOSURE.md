# Native period workflow checkpoint

2026-10-23. Local contract/browser closure, not production deployment acceptance.
The response storage blocker is RESOLVED under the user's explicit10..490 policy;
see BLAZOR-MIGRATION-PERIOD-RESPONSE-BLOCKER.md for the red/green boundary proof.

The native `/app/admin/period` route preserves the source Admin-only UI restriction
using the actual DB-backed profile. The backend's existing CapitalRead/CapitalWrite
policies remain unchanged: e.g. Kapital remains authorized by the actual API but
does not acquire a newly invented Admin UI route. All five other DB roles are
denied that route even with forged provider grants. Application permissions are
not converted into direct Keycloak roles.

Real typed current-state and paged `{requests,total,page,pageSize}` contracts use
central RBI transport, private token propagation and culture. No local fallback
pretends an unavailable API means an open period. Status filters retain actual
backend enums; localization converts only the display resource key. Numeric
month/year preserves source behavior, without replacing backend period rules.

Existing lock/unlock, request-unlock, reject, ask-info, respond and approve/unlock
endpoints are retained. Approval uses the selected request's period, not blindly
the current date. Responding creates a new PENDING request with the source prefix
and preserves the original NEEDS_INFO record. Unlock approves all currently
PENDING records for that period as before. No new owner/state/permission policy,
background email sender, schema or business calculation was introduced.

Responses use10..490; other note/request inputs retain10..500. The existing note
counter pattern is retained with live input updates, accessible errors and safe
BS/EN text. Native dialogs use their regular focus/close behavior and scoped
host border-box sizing. The 375px test checks the page, dialog bounds and inner
body, and the final screenshot was visually inspected. Typed reference/unknown
values are encoded text, not rendered HTML.

## Evidence

- **478/478 .NET**:164 frontend,210 backend unit,104 integration. This adds19
  frontend cases (18 typed API/draft cases + signed-out route) and9 HTTP backend
  regressions; previous user/admin/reporting safety coverage is retained.
- **12 Admin period browser cases**: current/empty/accessibility, five exact
  response boundaries, full lock/request/info/respond/approve workflow and exact
  audit order, rejection/filter/cancel,100-row server paging, outage recovery,
  uncertain post-persist recovery,375px and BS/EN limits/count text.
- One denied/forged-provider case additionally runs for each other DB role,
  preserving the distinct current Kapital backend authorization contract.
- Full browser gate: Admin53, gost17, kapital25, PL21, limiti19, FL23 =
  **158/158 PASS**, workers=1/retries=0. All earlier suites remain included.
- Release publish, changed-new-C# format verification and git diff --check pass.
  Existing backend-controller formatting is preserved outside the narrow guard;
  the formatter's workspace-load warning and Pkcs NU1902/NU1903 advisories remain
  unresolved/unsuppressed.

The source backend persists requests before later audit/email steps. A test-only
audit fault after save demonstrates that an upstream failure does not prove no
record exists. Native feedback explicitly warns to refresh before resubmitting;
refresh shows the stored response and does not issue another POST. This is owned
recovery copy, not a rewrite of existing transaction/email semantics or an
automatic retry. Normal successful audit actions/actors remain exact.

The full FL runs exposed a pre-existing fixture race: focus/mobile tests clicked
Continue while the final identity field's blur-triggered duplicate lookup could
disable it, leaving the identity step unchanged. The unchanged case passed in
isolation. The shared nonresident setup now holds the real FBA lookup in the test-only host, blurs
the field, asserts the pending disabled state, releases the actual request and
waits for enabled state before clicking. The original business-heading focus,
Axe, Escape and trigger-focus assertions remain unchanged. No physical-person
production code or platform package was changed; no sleeps, retries or timeout
increases were added. Original failure log/trace receipts are retained as
period-physical-focus-failure.log/.zip and
period-physical-mobile-setup-failure.log/.zip under the ignored receipt directory.
The final complete FL run passes all 23 cases, with the unchanged assertions.

The test host uses real controllers/repositories/services/authorization/audit and
unique InMemory stores with synthetic identity and mocked email delivery. It does
not execute backend Program/.env, SQL, real Keycloak or SMTP. Therefore broadcast
delivery and SQL deployment behavior are still external acceptance gates, not
inferred from browser success. No user host was restarted or real data altered.

Receipts: `artifacts/migration-template/period-*.log`, browser JSON/traces/images
under artifacts/migration-browser. Reproduce with the migration solution using
`--artifacts-path artifacts/users-safety --no-restore`, and npm test per role with
BROWSER_SAFETY_BUILD=true. Native restore/build prerequisites remain documented in
the earlier checkpoints. Code-list administration, guide/profile-wide closure and
integrated deployment acceptance remain open. React/deployment are unchanged.
