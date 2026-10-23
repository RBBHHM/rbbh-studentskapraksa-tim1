# Native shell, profile, guide and localization closure

2026-10-23. Local evidence only; live authentication and deployment acceptance
are separate gates. This does not authorize retiring the production React app.

The native template shell now links every migrated module. Home has no invented
financial metrics. It uses real backend profile/permission reads for business
cards and the DB-owned Admin role for administration cards; reporting archive
access remains authenticated even without generation permissions. Provider
application-role/permission claims never substitute for backend grants.

My Profile retains the template's AuthenticationState/ClaimsPrincipal model for
identity, with the current backend profile for application roles/permissions and
username fallback when the corresponding identity claim is absent. Failure keeps
the actual identity but displays no fabricated grants. Explicit retry refreshes
the actual endpoint; cancellation follows component lifetime. Long permission
codes wrap inside the scoped profile card at375px.

The application-owned guide keeps source business sections, status legend,
collapsible keyboard-operable sections and before-confirm guidance. Content is
selected by actual module-view permissions, not an obsolete four-Keycloak-role
model. Limits and Capital are separate; reporting explicitly documents approved
authenticated read/download versus Kapital/Admin generate/persist. Administration
documents self/final-Admin safety, period10..490 clarification, existing lock scope,
business codebooks, and read-only audit. Unknown persistence outcomes instruct
refresh before resubmission rather than falsely promising no data was saved.

All business strings remain in the application's RBI.Localization BS/EN catalogs,
BS default. No i18next/React provider or DesignSystem implementation was copied.
No German product support is advertised. The guide fails safely when effective
profile lookup is unavailable; it does not invent section access.

## Evidence and scope

Seven guide/resource unit cases plus signed-out guide HTTP coverage are added.
Six common browser cases run per current DB role: dashboard/navigation, actual
identity/grants, permission-service failure/retry, guide keyboard/accessibility
and approved reporting copy,375px home/profile/guide, BS/EN cookie/reload/backend
header. Full Axe WCAG2A/2AA/2.1AA scans are unchanged. Profile and guide mobile
screenshots were visually inspected, including wrapped permission text.

The first focused run's recovery failure used a stale test-host binary without
the new profile-available fixture control. The host was explicitly rebuilt and
both restoration controls now assert204; no production behavior, retry, timeout
or intended assertion was changed to mask it. The next focused run passed6/6.
The original receipt is guide-stale-host-red.log in ignored artifacts.

Final integrated local receipts and totals are recorded in
REACT-TO-BLAZOR-FINAL-HANDOFF.md. Browser identity/store are isolated test fixtures,
not a production fake-auth mode. Production source has no fixture endpoints,
synthetic principal, frontend DB/Excel library or provider-role grant path.
Actual SSO/login/logout/token-refresh, shared cache/DataProtection, SQL locking,
SMTP, private package publication and OCP deployment remain external gates.

Final gate:520/520 .NET,209/209 browser;36 common shell/profile/guide cases are
included (6 per role). Admin69, gost24, Kapital32, PL28, Limiti26, FL30 all pass.
The first complete FL attempt was29 PASS/1 FAIL in another manual identity-setup
call site. Its trace confirmed the first identity step remained visible: the
blur-triggered FBA validation raced the Continue click, as found at the earlier
period checkpoint. That call now uses the same existing nonresident setup helper
and exact input values, with explicit204 checks on hold/release controls. The
real backend lookup still executes; no production physical-person code or
business/focus/accessibility assertion changed. Full FL then passes30/30 with
workers1/retries0. Original log/trace remain as
final-FL-first-step-setup-red.log/.zip. The other5 passing role suites do not
include that physical-person test file and were not invalidated by its setup fix.
