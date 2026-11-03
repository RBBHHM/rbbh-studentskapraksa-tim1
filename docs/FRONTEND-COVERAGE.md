# Frontend requirement coverage

Source: `[Related Parties Register] Dokumentacija.pdf` (pp. 3-20) and `docs/POPRAVKE.txt`.
Status: VERIFIED (implemented and covered by unit or browser tests), PARTIAL (frontend done, one part open), BLOCKED (needs a backend or external contract), BACKEND (owned by Codex, no frontend work), OPEN (frontend work remaining).

| PDF p. | Requirement | Status | Evidence / dependency |
|---|---|---|---|
| 4 | Performance (Lighthouse) | OPEN | Deferred by the requester to the end |
| 5 | Every request needs a token | BACKEND | API side; the frontend always sends the bearer token |
| 5 | Every page protected per role | VERIFIED | Six-role Playwright suites, access-denied cases |
| 5 | Roles not hardcoded | VERIFIED | Roles come from the profile and `T.Role`; guard tests |
| 5 | 8-hour session | BLOCKED | Needs the Keycloak and cookie contract |
| 5 | Automatic Guest role | VERIFIED | Guest shell and read-only pages tested (gost suite) |
| 5-6 | "Zatraži rolu" modal, "Promijeni rolu", admin requests page, emails | BLOCKED | Role-request API and mail contract |
| 6 | Login form opens immediately | VERIFIED | Middleware challenge plus `ProtectedRouteStartsTheIdentityChallengeWithoutSignInScreen` |
| 6 | AppUsers removed, duplicate protection | BACKEND | Frontend shows the duplicate error from the API |
| 7 | Clear errors and logging | PARTIAL | Frontend error states are localized; global handler and logs are backend |
| 7 | Database ERD, created/modified fields | BACKEND | |
| 7 | Legal entities: remove "Povezano lice", Datum od required | VERIFIED | Legal entity form and tests |
| 7-8 | Legal entity field rules (residency, FBA_ID, Porez_broj, MB, name, basis, dates) | VERIFIED | Legal form tests and Model.Validate unit tests |
| 8 | GCC number search and automatic GCC name | BLOCKED | Needs a GCC lookup API |
| 8 | Large-exposure DA/NE flag (p. 10) | BLOCKED | Needs a field in the API contract |
| 8 | Case-insensitive search | VERIFIED | List search tests |
| 8, 11 | Excel import/export content, created-by/modified-by, JMBG, basis description, deep validation | BLOCKED | Business logic is backend; the import flow and result dialog are VERIFIED |
| 8-9 | Next disabled and immediate field messages | VERIFIED | `LiveValidation` on all forms (legal, physical, users, limits, capital, code lists, period note) |
| 9 | No temporary messages, enough room | VERIFIED | Toasts for success; messages sit inline |
| 9 | Single focus ring | VERIFIED | Axe and focus tests |
| 9 | Messages not repeated at the bottom | VERIFIED | `FormSummary` shows form-level messages only; guard test forbids `<ValidationSummary>` |
| 10 | Four-eyes verification: disabled with a hover hint for the creator | PARTIAL | UI VERIFIED for legal and physical persons; server enforcement is backend |
| 10-11 | Physical persons: toggle contrast, disabled-state contrast | VERIFIED | Contrast CSS and Axe in light and dark |
| 12 | Limits: entity list visible on focus | VERIFIED | `LimitEntityPicker` tests |
| 12 | Limit types from a code list | VERIFIED | Guard test blocks literal options |
| 12 | Capital history table, add capital for several dates | BLOCKED | Needs a capital history API |
| 13-14 | Regulatory reporting mapping | BLOCKED | Mapping and Finrep source are backend |
| 14 | Notification page, bell, mail, lock/role events | BLOCKED | Needs the notification API |
| 14 | Users: "Rola" column | VERIFIED | Users tests |
| 14 | Users: email suggestion, editable | VERIFIED | `UsersApiTests`, users spec |
| 15 | Add user works, user update | PARTIAL | Create and edit work against the API; the earlier "service unavailable" error is backend |
| 15 | Period management mail/notifications | BLOCKED | Backend |
| 16 | Code lists: dropdown search, cards, values on click | VERIFIED | `code-lists.spec` |
| 16 | Code lists: created-by and last-modified | BLOCKED | Needs the metadata in the API |
| 16 | Code-list import/export | PARTIAL | UI flow VERIFIED; business logic is backend |
| 16 | Audit: dynamic coverage for every action | BACKEND | |
| 16 | Audit: red/green/yellow badges and a details modal | VERIFIED | `AuditChangeTests`, audit spec |
| 16 | User guide with more detail | PARTIAL | `guide.before-title` and sections added; extra detail text still to write |
| 16-17 | Localization of all text, two code lists, runtime catalogue | PARTIAL | BS/EN JSON with key parity and no-literal-text guard VERIFIED; runtime localization code lists need the API |
| 17 | Uniform API responses | BACKEND | |
| 17 | Tests: unit, integration, E2E, permission matrix | PARTIAL | 223 unit tests and Playwright for six roles |
| 17 | Monitoring and health | BACKEND | |
| 18 | Single README and onboarding file | OPEN | Docs consolidation not done; many legacy documents remain |
| 18 | Release notes page from GitHub | BLOCKED | Needs a server-side GitHub source |
| 18 | Mapping server `bosnadb\HEAD` | BACKEND | |
| 18 | Created/modified by as username | BACKEND | |
| 18 | My profile layout | VERIFIED | Profile spec |
| 19 | Sidebar active highlight | VERIFIED | `NavMenu` location listener and spec |
| 19 | Favicon is the Raiffeisen logo | VERIFIED | `App.razor` icon link |
| 19 | No hardcoded select/checkbox options | VERIFIED | Guard test; two entries remain on a tracked allowlist (Period `Statuses`, Guide `States`) |
| 19 | No hardcoded text | VERIFIED | `MarkupContainsNoLiteralVisibleText`, `EveryLiteralResourceKeyExistsInBothCatalogs` |
| 19-20 | Enter toggles checkboxes | VERIFIED | `app.js` handler |
| 19-20 | Enter toggles accordions | OPEN | Not yet checked against the RBI accordion |
| 20 | Pagination footer: total records only, modern style | PARTIAL | Text shows total only; the visual restyle is still to do |
| - | Tracked Keycloak client secret | PARTIAL | Removed from the tracked config (`cf2f745`) and documented as a release blocker; it remains in git history, so the owner must rotate it |

## Verification on `c3a2bbd`
Unit 223/223 (also without the identity environment variables). Playwright, workers=1, retries=0: admin 108, FL 48, PL 46, kapital 51, limiti 43, gost 38, all passed. Axe runs in light and dark on every route.
