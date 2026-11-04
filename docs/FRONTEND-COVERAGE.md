# Frontend requirement coverage

Source: `[Related Parties Register] Dokumentacija.pdf` (pp. 3-20) and `docs/POPRAVKE.txt`.
Status: VERIFIED (implemented and covered by unit or browser tests), PARTIAL (frontend done, one part open), BLOCKED (needs a backend or external contract), BACKEND (owned by Codex, no frontend work), OPEN (frontend work remaining).

| PDF p. | Requirement | Status | Evidence / dependency |
|---|---|---|---|
| 4 | Performance (Lighthouse) | OPEN | Deferred by the requester to the end |
| 5 | Every request needs a token | BACKEND | API side; the frontend always sends the bearer token |
| 5 | Every page protected per role | VERIFIED | Six-role Playwright suites, access-denied cases |
| 5 | Roles not hardcoded | VERIFIED | Roles come from the profile and `T.Role`; guard tests |
| 5 | 8-hour session | NOT TESTED | Absolute 8 h RBI cookie, no sliding renewal (code-verified); no expiry test, real realm policy unverified |
| 5 | Automatic Guest role | VERIFIED | Guest shell and read-only pages tested (gost suite) |
| 5-6 | "Zatraži rolu" modal, "Promijeni rolu", admin requests page | VERIFIED | `role-requests.spec.ts`, `RoleRequestsApiTests`; mail delivery is BLOCKED (needs a real provider) |
| 6 | Login form opens immediately | VERIFIED | Middleware challenge plus `ProtectedRouteStartsTheIdentityChallengeWithoutSignInScreen` |
| 6 | AppUsers removed, duplicate protection | BACKEND | Frontend shows the duplicate error from the API |
| 7 | Clear errors and logging | PARTIAL | Frontend error states are localized; global handler and logs are backend |
| 7 | Database ERD, created/modified fields | BACKEND | |
| 7 | Legal entities: remove "Povezano lice", Datum od required | VERIFIED | Legal entity form and tests |
| 7-8 | Legal entity field rules (residency, FBA_ID, Porez_broj, MB, name, basis, dates) | VERIFIED | Legal form tests and Model.Validate unit tests |
| 8 | GCC number search and automatic GCC name | PARTIAL | `GccPicker` and `legal-entities.spec.ts` against a test-only source; real bank source not configured |
| 8 | Large-exposure DA/NE flag (p. 10) | VERIFIED | `legal-entities.spec.ts` GCC test |
| 8 | Case-insensitive search | VERIFIED | List search tests |
| 8, 11 | Excel import/export content, created-by/modified-by, JMBG, basis description, deep validation | BLOCKED | Business logic is backend; the import flow and result dialog are VERIFIED |
| 8-9 | Next disabled and immediate field messages | VERIFIED | `LiveValidation` on all forms (legal, physical, users, limits, capital, code lists, period note) |
| 9 | No temporary messages, enough room | VERIFIED | Toasts for success; messages sit inline |
| 9 | Single focus ring | VERIFIED | Axe and focus tests |
| 9 | Messages not repeated at the bottom | VERIFIED | `FormSummary` shows form-level messages only; guard test forbids `<ValidationSummary>` |
| 10 | Four-eyes verification: disabled with a hover hint for the creator | PARTIAL | UI VERIFIED; server enforces `self_denied`; the UI cannot show the specific code (RBI transport exposes only the status) |
| 10-11 | Physical persons: toggle contrast, disabled-state contrast | VERIFIED | Contrast CSS and Axe in light and dark |
| 12 | Limits: entity list visible on focus | VERIFIED | `LimitEntityPicker` tests |
| 12 | Limit types from a code list | VERIFIED | Guard test blocks literal options |
| 12 | Capital history table, add capital for several dates | VERIFIED | `capital.spec.ts` (BrowserHost) |
| 13-14 | Regulatory reporting mapping | BLOCKED | Mapping and Finrep source are backend |
| 14 | Notification page, bell, lock/role events | VERIFIED | `notifications.spec.ts`; mail delivery is BLOCKED (real provider) |
| 14 | Users: "Rola" column | VERIFIED | Users tests |
| 14 | Users: email suggestion, editable | VERIFIED | `UsersApiTests`, users spec |
| 15 | Add user works, user update | VERIFIED | Create, roles and profile edit (`users.spec.ts`) |
| 15 | Period management mail/notifications | BLOCKED | Backend |
| 16 | Code lists: dropdown search, cards, values on click | VERIFIED | `code-lists.spec` |
| 16 | Code lists: created-by and last-modified | VERIFIED | `code-lists.spec.ts` |
| 16 | Code-list import/export | PARTIAL | UI flow VERIFIED; business logic is backend |
| 16 | Audit: dynamic coverage for every action | BACKEND | |
| 16 | Audit: red/green/yellow badges and a details modal | VERIFIED | `AuditChangeTests`, audit spec |
| 16 | User guide with more detail | VERIFIED | Guide gains four-eyes, legal residency rules, limit picker, audit colours and a general section (BS/EN); `shell-profile-guide.spec` and `GuideContentTests` |
| 16-17 | Localization of all text, two code lists, runtime catalogue | PARTIAL | BS/EN catalogs VERIFIED; backend runtime pack consumed (unit-tested only); DE has no frontend catalog |
| 17 | Uniform API responses | BACKEND | |
| 17 | Tests: unit, integration, E2E, permission matrix | PARTIAL | 234 frontend unit tests and Playwright for six roles on BrowserHost (InMemory); no deployed-API/SQL browser run |
| 17 | Monitoring and health | BACKEND | |
| 18 | Single README and onboarding file | VERIFIED | `README.md` and `docs/ONBOARDING.md` replace all legacy documents; no reference to the removed files remains |
| 18 | Release notes page from GitHub | BLOCKED | The backend has no route; needs a server-side GitHub source |
| 18 | Mapping server `bosnadb\HEAD` | BACKEND | |
| 18 | Created/modified by as username | BACKEND | |
| 18 | My profile layout | VERIFIED | Profile spec |
| 19 | Sidebar active highlight | VERIFIED | `NavMenu` location listener and spec |
| 19 | Favicon is the Raiffeisen logo | VERIFIED | `App.razor` icon link |
| 19 | No hardcoded select/checkbox options | VERIFIED | Guard test; two entries remain on a tracked allowlist (Period `Statuses`, Guide `States`) |
| 19 | No hardcoded text | VERIFIED | `MarkupContainsNoLiteralVisibleText`, `EveryLiteralResourceKeyExistsInBothCatalogs` |
| 19-20 | Enter toggles checkboxes | VERIFIED | `app.js` handler |
| 19-20 | Enter and Space toggle accordions, Tab moves on | VERIFIED | Native `<details>`; `shell-profile-guide.spec` Space/Enter and Tab tests |
| 20 | Pagination footer: total records only, modern style | VERIFIED | Pill style `.page-total`; `users.spec` asserts the style and absence of "Stranica x od y" |
| - | Tracked Keycloak client secret | PARTIAL | Removed from the tracked config (`cf2f745`) and documented as a release blocker; it remains in git history, so the owner must rotate it |

## Verification on the final integration branch
Frontend unit 234/234. Playwright on BrowserHost (workers=1, retries=0): admin 142, FL 60, PL 59, kapital 64, limiti 56, gost 50, all passed, including Axe in light and dark and keyboard checks. Backend unit 337/337; backend integration 395/395 on LocalDB. See [FINAL-REQUIREMENTS-COVERAGE.md](FINAL-REQUIREMENTS-COVERAGE.md) for per-requirement evidence and [FRONTEND-INTEGRATION-FINAL.md](FRONTEND-INTEGRATION-FINAL.md) for the integrated endpoints.

## Contracts that were requested earlier
The backend contracts requested in the earlier version of this file (role requests, notifications, GCC, large exposure, capital history, runtime localization, code-list metadata) now exist and are integrated. Still without a backend route or decision: release notes, status catalogs for Period and Guide, structured error codes through the RBI transport, DE frontend strings, role-catalog UI and bank-customer lookup UI.
