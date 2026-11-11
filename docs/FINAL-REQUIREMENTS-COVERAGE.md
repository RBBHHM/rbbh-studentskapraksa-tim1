# Final requirements coverage

Sources: `[Related Parties Register] Dokumentacija.pdf` (preserved at the repository root), `docs/POPRAVKE.txt`, and the backend matrix in `docs/BACKEND-FINALIZATION.md`.

## How to read the statuses

| Status | Meaning |
|---|---|
| VERIFIED | Implemented and covered by an automated test that ran green on this branch. |
| PARTIAL | Part of the requirement is implemented and tested; the remainder is named in the last column. |
| MISSING | No implementation, and none can be added without a decision or a contract that does not exist. |
| BLOCKED | Needs an external system, an owner decision, or a backend contract that is not approved. |
| NOT TESTED | Implemented, but no automated test exercises it. |

**What "tested" means here.** All browser (Playwright) evidence runs against `tests/Web.Blazor.BrowserHost`: the real Blazor frontend calling the **real backend controllers, services and validators in-process**, but on an **EF InMemory database** with a fixture identity. It does **not** prove real SQL Server behaviour, real Keycloak, the bank reference database, Finrep, or a real mail provider. Real-SQL evidence exists only for the backend integration suite (disposable SQL Server 2022 in CI, optionally LocalDB on Windows). A true frontend → deployed API → SQL browser run was **not** executed and is listed as a gap.

## Test results merged to main via PR #6 (11 November 2026)

CI evidence: https://github.com/RBBHHM/rbbh-studentskapraksa-tim1/actions/runs/43080936106

The .NET solution build, all three .NET suites, SQL workbook roundtrips, and every role-specific Playwright job passed on commit `496ed2b2` ([verified run](https://github.com/RBBHHM/rbbh-studentskapraksa-tim1/actions/runs/43080936106)). The security history scan failed on the pre-existing exposed credential; image publication was correctly skipped. These tests use disposable SQL and an InMemory BrowserHost, not real Keycloak, bank data, Finrep or OCP.

| Suite | Result |
|---|---|
| Release build, whole solution, `-warnaserror` | 0 warnings, 0 errors |
| Frontend unit (`tests/Web.Blazor.Tests`) | **253 passed, 0 failed, 0 skipped** (PR #6 CI) |
| Backend unit (`tests/Backend.UnitTests`) | **357 passed, 0 failed, 0 skipped** (PR #6 CI) |
| Backend integration, without disposable SQL configuration | 309 passed, 0 failed, **86 skipped** (SQL-only tests; separate from current CI) |
| Backend integration, isolated SQL Server 2022 (CI) | **400 passed, 0 failed, 0 skipped** (real SQL; includes two workbook transfers and SQL search) |
| Playwright admin (BrowserHost) | **143 passed** (includes unconfirmed-identity banner and reload) |
| Playwright FL | 60 passed |
| Playwright PL | 60 passed |
| Playwright kapital | 64 passed |
| Playwright limiti | 56 passed |
| Playwright gost | 50 passed |
| Publication guard unit tests (`scripts/security`) | 12 passed |
| Secret scan of the full reachable history | **FAIL: inherited Keycloak credential in public main history.** Current tracked configuration is blank; rotation and approved history remediation remain required. |
| `git diff --check` on the integration range | clean |

Every role's Playwright run also executes the shared specs: authorization matrix, shell/profile/guide, **Axe (WCAG 2 A/AA/2.1 AA) in light and dark themes**, keyboard focus order, notifications, role requests, and 375 px responsive checks. Suites were run one at a time with one worker.

Not executed: a deployed-API browser run, real Keycloak login and a manual screen-reader pass. Lighthouse now runs on the isolated fixture only (not production).

## Requirements

Frontend paths are under `Frontend/`; backend paths under `Backend/`. "BH" means BrowserHost evidence (real controllers, InMemory DB).

### Access, identity and roles (PDF pp. 5-6)

| Requirement | Frontend | Backend | Test evidence | Status | Remaining |
|---|---|---|---|---|---|
| Every request carries the user token | `Api/AppApi.cs` (RBI transport) | Auth policies on all controllers | Backend integration (real SQL) authorization tests | PARTIAL | Frontend token forwarding runs only through the RBI transport; not exercised against real Keycloak |
| Every page protected per role | route guards, `NavMenu.razor`, pages check `Profile` grants | `ApplicationPolicies`, role-permission grants | Six-role Playwright incl. `*-denied` and `*-read-only` specs (BH) | VERIFIED | none |
| Roles not hardcoded | `T.Role`, profile-driven | DB-backed roles | `Web.Blazor.Tests` guard tests | VERIFIED | none |
| Automatic Guest role | gost shell | virtual `gost` for unknown subjects | gost suite 50 (BH); backend integration | VERIFIED | none |
| Login opens immediately | RBI middleware challenge | OIDC | `ProtectedRouteStartsTheIdentityChallengeWithoutSignInScreen` | PARTIAL | Real Keycloak redirect not run |
| 8-hour session | RBI cookie, absolute 8 h, no sliding (`RbiAuthenticationDefaults.SessionLifetime`) | Backend 8 h absolute cap | `FrontendSessionConfigurationTests`, `SessionCookieContractTests`, `AbsoluteSessionHttpTests` | PARTIAL | Configuration and controlled-clock expiry tested; real Keycloak refresh/SSO in an authenticated browser remains owner/environment validation |
| Logout is a POST with antiforgery | RBI logout form | CSRF POST logout (legacy GET deprecated) | Shell specs click logout | VERIFIED | The frontend never calls the backend logout; the local cookie ends the session |
| "Zatraži rolu" / "Promijeni rolu" modal | `Components/Registry/RoleRequestDialog.razor`, `RoleRequestButton.razor`, `MyProfile.razor`; `Api/RoleRequestsApi.cs` | `api/role-requests` | `role-requests.spec.ts`, `RoleRequestsApiTests.cs` (BH) | VERIFIED | none |
| Admin approve/reject role requests | `Pages/RoleRequests.razor`; deterministic `decisionId` for safe replay | decision with `decisionId` + `expectedVersion` | `role-requests.spec.ts` (BH) | VERIFIED | none |
| Role-request mail | none (server side) | outbox + dispatcher | Backend tests | BLOCKED | Needs a real mail provider |
| Role catalog UI | `Pages/RoleCatalog.razor`, dynamic DB permissions and safe admin editor | `api/role-catalog` GET/POST/PUT with protected system roles and optimistic version | `RoleCatalogApiTests`, six-role Playwright | VERIFIED | Production role grants must be provisioned by the owner |
| User profile edit (username, names, email) | `Pages/Users.razor`, `UserDraft.cs`, `UserForm.razor`, `UsersApi.UpdateProfileAsync` | `PUT api/users/{id}` | `users.spec.ts` "profile fields ... editable", `UsersApiTests.cs` (BH) | VERIFIED | none |
| Email suggestion editable, any domain | `UserDraft.cs` | email `maxLength 200` | `UsersApiTests.cs`, users spec | VERIFIED | none |
| Unconfirmed-identity badge | `Users.razor`, identity status from DB | `identityConfirmed` | `users.spec.ts` with unconfirmed fixture and reload | VERIFIED IN FIXTURE | Actual Keycloak subject-reconciliation remains an external identity test |
| User deactivate/reactivate/delete, last-admin safety | `Users.razor` | mutation boundary + SQL app lock | users specs (BH); `UsersLastAdminSqlServerTests` (real SQL) | VERIFIED | none |
| Duplicate user protection | form shows API error | unique indexes | users specs, backend integration | VERIFIED | none |

### Notifications (PDF p. 14)

| Requirement | Frontend | Backend | Test evidence | Status | Remaining |
|---|---|---|---|---|---|
| Notification page and header bell with unread count | `Pages/Notifications.razor`, `Layout/NotificationBell.razor`, `Components/NotificationState.cs`, `Api/NotificationsApi.cs` | `api/notifications` inbox, `POST {id}/read` | `notifications.spec.ts`, `NotificationsApiTests.cs` (BH) | VERIFIED | none |
| Role/lock events produce notices | rendered from `titleKey`/`bodyKey` + parameters | atomic inbox/outbox | notifications spec | PARTIAL | Only the fixture-produced events are covered |
| Notification mail | none | dispatcher with leases and retries | backend tests | BLOCKED | Real provider; mail previews are not a production inbox |

### Legal and physical persons (PDF pp. 7-11)

| Requirement | Frontend | Backend | Test evidence | Status | Remaining |
|---|---|---|---|---|---|
| Legal form: field rules, Datum od required, no "Povezano lice" | `LegalEntityForm.razor`, `LegalEntityDraft.cs` | `LegalEntityService` validation | `legal-entities.spec.ts`, `Model.Validate` unit tests | VERIFIED | none |
| Physical persons CRUD, family links | `PhysicalPersons.razor` | `RelatedPersonService` | `physical-persons.spec.ts` (FL) | VERIFIED | none |
| GCC search and automatic GCC name | `Registry/GccPicker.razor`, `LegalEntitiesApi.GccAsync` | `api/bank-reference/gcc` (503 when unconfigured) | `legal-entities.spec.ts` GCC test; both the unavailable and the available source (BH fixture source) | PARTIAL | Source is a **test fixture**. Real `bosnadb\HEAD` access and "mandatory GCC" are blocked until a source is configured |
| Large-exposure DA/NE | `LegalEntityForm`, column in `LegalEntities.razor` | nullable `isLargeExposure` | GCC test saves and displays it | VERIFIED | Business meaning/default for existing rows is a backend decision |
| Bank customer lookup | `BankCustomerPicker.razor`, `LegalEntityForm`, `LegalEntitiesApi.CustomersAsync` | `api/bank-reference/customers` SELECT-only | `BankCustomerApiTests`, browser fixture and unavailable-source tests | PARTIAL | Real bank source and mapping must be configured and verified externally |
| Four-eyes: creator cannot verify, hover hint | creator disabled buttons and localized safe server codes | `verification.self_denied` 403, `verification.provenance_required` 409 | physical/legal browser specs and `StructuredBusinessErrorTests` | VERIFIED | Deployed Keycloak subject binding requires live environment validation |
| Case-insensitive search | list search | Explicit case-insensitive SQL collation | `RegistrySearchSqlTests`, BH list search (real isolated SQL + BrowserHost) | VERIFIED | Bank source-specific collation still requires source-owner verification |
| Excel export/import with result dialog | `ImportFeedback`, result dialogs | `{imported,failed,errors,details}`; physical v3 description column, legacy v1/v2 accepted | `RegistryWorkbookSqlRoundtripTests` transfers legal/physical exports between independent migrated real-SQL databases; `RegistryWorkbookHttpTests` tests legacy formats and invalid rows | PARTIAL | Business owner approval of new 26th column and real bank data are external; SQL test of all historic workbook variants still desirable |
| Next disabled, immediate field messages, no repeated bottom messages | `LiveValidation`, `FormSummary` | n/a | guard tests, form specs | VERIFIED | none |
| Single focus ring, toggle contrast | `app.css` | n/a | Axe light/dark in every role | VERIFIED | none |

### Limits and capital (PDF p. 12)

| Requirement | Frontend | Backend | Test evidence | Status | Remaining |
|---|---|---|---|---|---|
| Entity list visible on focus | `LimitEntityPicker` | paged lookup | `limits.spec.ts` | VERIFIED | none |
| Limit types from a code list | no literal options (guard) | code list | guard test | VERIFIED | none |
| Limits CRUD, export | `Limits.razor` | limits controller | `limits.spec.ts` (limiti) | VERIFIED | none |
| Limits workbook import | `Limits.razor`, `LimitsApi.ImportAsync` | `api/limiti/import` | `limits.spec.ts` "workbook import" (rejects non-xlsx; round-trips an exported workbook) | PARTIAL | The test asserts a result dialog and Axe, not specific row counts of a hand-built workbook |
| Capital history, several dates, as-of | `Capital.razor`, `CapitalApi.HistoryAsync/AsOfAsync` | immutable revisions, `/history`, `/as-of` | `capital.spec.ts` (BH; revision numbers not asserted because InMemory) | VERIFIED | Revision numbering on real SQL is covered only by backend integration |

### Reporting (PDF pp. 13-14)

| Requirement | Frontend | Backend | Test evidence | Status | Remaining |
|---|---|---|---|---|---|
| Generate/download reports, permissions | `Reports.razor` | `ReportService` | `reports.spec.ts`, `reports-read-only.spec.ts` (BH) | VERIFIED | none |
| Data origin shown, not a reconciliation claim | `Reports.razor` source column | `dataSource`, `isAuthoritative` | `reports.spec.ts` "data origin" | VERIFIED | none |
| Finrep mapping | none | optional SELECT-only adapter | backend tests with a stand-in | BLOCKED | Real Finrep access and approved mapping |

### Period, code lists, audit (PDF pp. 15-16)

| Requirement | Frontend | Backend | Test evidence | Status | Remaining |
|---|---|---|---|---|---|
| Period lock, unlock request, own list vs review queue | `Period.razor`, `PeriodApi` (`review` route) | owner-scoped lists, owner-only respond | `period.spec.ts`, `PeriodApiTests.cs` (BH) | VERIFIED | none |
| Period status list | static enum (allow-listed) | no catalog | guard allow-list | PARTIAL | Backend offers no status catalog |
| Code lists: dropdown search, cards, values | `CodeLists.razor` | code-list controller | `code-lists.spec.ts` | VERIFIED | none |
| Code lists: created-by / last modified | card meta line, `CodeListsApi.DefinitionsAsync` | `categories/definitions` | `code-lists.spec.ts` "category cards show who created" | VERIFIED | none |
| Code-list import/export | dialog flow | import/preview | `code-lists.spec.ts` | PARTIAL | Business format owned by backend |
| Audit: badges, details modal, dynamic coverage | `Audit.razor` | audit service, pagination overflow + max-date fixes | `audit.spec.ts`, `AuditChangeTests` | VERIFIED | Backend audit-coverage gaps (AUD-01) are not a frontend item |

### Localization, guide, shell (PDF pp. 16-20)

| Requirement | Frontend | Backend | Test evidence | Status | Remaining |
|---|---|---|---|---|---|
| All UI messages localized (BS/EN/DE) | `Localization/bs.json`, `en.json`, `de.json`, 608 aligned keys and numeric placeholders | Existing RBI culture picker discovers Deutsch | Three-catalog parity tests; all-role Playwright language switching | VERIFIED IN BRANCH | Check linguistic/business signoff for specialist financial terminology |
| Runtime language packs | `RuntimeResources` overrides translated strings, static locales are fallback | `api/localization/*`; `SeedFrontendLanguagePackResources` migration adds 1,824 strings (608 × BS/EN/DE) to existing DB codebooks, preserving edits; `Put` bumps version | `LocalizationSqlTests.FrontendResourcesAreEditableFromAllThreeRuntimeLanguageCodebooks` | VERIFIED IN BRANCH | Real bank migration and business-approved DE wording require owner signoff |
| User guide with detail | `Guide.razor` | n/a | `GuideContentTests`, guide spec | VERIFIED | The guide's "States" list is an allow-listed static list |
| Enter toggles checkbox; Enter/Space toggle accordion | `app.js`, native `<details>` | n/a | shell and guide specs | VERIFIED | none |
| Pagination footer: total only | `.page-total` | n/a | `users.spec.ts` | VERIFIED | none |
| Sidebar highlight, favicon, profile layout | `NavMenu`, `App.razor`, `MyProfile` | n/a | profile and shell specs | VERIFIED | none |
| No hardcoded options/text | guard tests | n/a | `MarkupContainsNoLiteralVisibleText` | VERIFIED | Two tracked static lists (Period, Guide) |
| Responsive 375 px | `app.css` | n/a | 375 px spec in each page suite | VERIFIED | none |
| Single README and onboarding | `README.md`, `docs/ONBOARDING.md` | n/a | review | VERIFIED | none |
| Release notes page from GitHub | `Pages/ReleaseNotes.razor`, UAT/PROD selector | `GET api/release-notes` with 5-min cache, matching UAT body on PROD | API tests, six-role Playwright, unit parity and no-phantom-version tests | VERIFIED IN BRANCH | UAT tag `uat-vX.Y.Z` creates notes; PROD tag `vX.Y.Z` requires approved matching UAT release and copies identical text. Tagging must follow a verified deployment and cannot itself prove the version was deployed |
| Performance / Lighthouse | isolated native Blazor BrowserHost | `.github/workflows/lighthouse.yml` records 4 independent Lighthouse reports and enforces baseline budgets (mobile ≥70, desktop ≥90; accessibility and best-practices ≥95; CLS ≤0.1) | [CI 43080935740](https://github.com/RBBHHM/rbbh-studentskapraksa-tim1/actions/runs/43080935740) passed fixture baseline; desktop 99/99, mobile ~77–83, accessibility/best-practices 100 | VERIFIED IN FIXTURE | Production/mobile throttling, full screen-reader review and real authenticated LCP require deployed infra; performance experiment that failed CI was reverted |

### Platform and security

| Requirement | Evidence | Status | Remaining |
|---|---|---|---|
| Structured error codes in the UI | Transport exposes bounded and validated `code`; selected four-eyes errors have safe localized UI messages | PARTIAL | Other business codes still use generic HTTP status messages |
| Secret-free publication history | Current tracked settings are blank; full-history CI guard detects the previously committed credential. | BLOCKED | Owner must rotate/revoke the affected Keycloak credential and coordinate historical exposure remediation. Image publication stays blocked. |
| Real Keycloak / SQL-through-frontend / bank / Finrep / mail | none run | NOT TESTED / BLOCKED | Needs an environment and credentials the owner controls |
| Deployment (OCP/3scale, shared sessions, DP keys) | none | BLOCKED | Backend OPS-02 |

## Gaps in one place

1. No frontend → deployed API → SQL browser run (BrowserHost is InMemory).
2. Eight-hour frontend OIDC/cookie configuration, backend expiry, and cookie non-sliding behavior are regression-tested; actual Keycloak expiry/refresh in a real browser remains an owner environment task.
3. Role catalog and bank customer UI are implemented; production RBAC/catalog data and real bank source remain external.
4. Four-eyes backend codes now reach localized UI via a validated typed transport; other domain errors remain status-based.
5. DE frontend catalog is complete (608/608), but bank-approved terminology and real migration remain to verify.
6. Release-note publishing is automated by UAT/PROD version tag workflows; version tags must follow deployment approval. No actual GitHub Releases are published yet.
7. GCC mandatory mode, Finrep mapping, mail delivery and bank data need real external sources.
8. Credential rotation and explicit owner-approved remediation of the already-public Git history are still required.


## Complete PDF and POPRAVKE.txt review — pages 1–20 (11 November 2026)

This register covers **every page** of `[Related Parties Register] Dokumentacija.pdf`, not only the earlier handoff matrix. "Source/test verified" means the code is present and tested on a fixture; it does not assert production integrations that have not been run.

| PDF page | Area and requested behaviour | Implementation/evidence | Residual or ownership |
|---|---|---|---|
| 1 | Original specification cover and project identity | Original PDF preserved at repository root | No application change |
| 2 | Overview and purpose of the related-party register | Main dashboard, profiles, business areas and protected navigation | Bank business owner accepts terminology |
| 3 | Module organisation and scope | Blazor UI, ASP.NET Core API, EF Core model, integration and unit/browser test suites | External hosting depends on OCP |
| 4 | Quality, architecture, automation, performance and Lighthouse | Solution builds under CI; Lighthouse dashboard/guide mobile and desktop, complete JSON reports | Fixture mobile 79/82; deployed performance/real-screen-reader still unmeasured |
| 5 | Authentication, permissions and initial page | Keycloak/OIDC contract, backend application grants, virtual Guest, no separate sign-in page | Actual realm, subject/audience, token expiry and 8-hour session are external |
| 6 | Database roles, requests, multi-role assignments, administrator flow and email | DB roles, role-request actions/inbox, protected role-catalog UI, last-admin guards | Real identity mapping, mail provider, requested historical AppUsers refactor requires bank DB migration decision |
| 7 | Stable business data, logging, legal-person form | Legal CRUD, validation, safe errors, audit and source-aware fields | Production lookup source and data mapping are external |
| 8 | Bank client/GCC lookup, form rules and workbook flows | Read-only bank customers/GCC pickers, no invented records, Excel validation/import/export | Actual `bosnadb` and legal tax/identifier source signoff external |
| 9 | Legal status, verify, change/delete, immediate validation and UX | Four-eyes server author check; edit, delete, import; inline validation + focus | Browser with real SQL & production legal sample data not executed |
| 10 | Physical identities, employees and related/family links | Physical CRUD, JMBG/passport/FBA validation, relatives, family tree and duplicate checks | HR reference/approved identities external |
| 11 | Physical four-eyes, special relationship details and Excel columns | Controlled verification and 26-column v3 export with special-basis description read from codebook, plus creator/modified and linked ID columns; retains older imports | Real SQL round-trip and bank-approved 26th-column schema signoff |
| 12 | Limits, codebook types, capital and historical dates | Limit picker/import/export and capital effective-date immutable revision history | Actual bank data and regulatory acceptance external |
| 13 | Daily/monthly regulatory reports and Finrep | Role-scoped local generation/download/history; data origin explicitly displayed | Actual `Finrep_db.Monthlyhistory` schema/mapping and source tests external |
| 14 | Role/period events, notifications, mail, report visibility | Recipient-scoped inbox + unread bell, workflow events; outbox/dispatcher tested with fixture | Real SMTP/HTTP provider and email delivery external |
| 15 | User management and period locking/unlocking | Role assignment, status/last-admin controls; period approval/rejection/clarification, owner-only queue | Real production users, expected decision policies external |
| 16 | Codebooks, search/cards/import, audit and user help | Generic code-list service, admin CRUD, audit badges/details, permission-aware guide | Audit coverage for every possible business action needs systematic production reconciliation |
| 17 | API errors, test strategy, operational health and monitoring | Safe ProblemDetails, bounded structured codes, CI unit/SQL/E2E tests, DB readiness checks | Real Keycloak/mail/cache availability and alerting not established (operational task) |
| 18 | Layout, accessibility, responsive navigation and keyboard support | Sidebar current page, mobile 375px, focus, light/dark Axe tests | Manual screen-reader testing on deployed environment outstanding |
| 19 | No hardcoded labels, language configuration and runtime text editing | BS/EN/DE 608-key catalogs, SQL-language-codebook migration, runtime overrides with version invalidation | Bank-approved native German legal/financial terminology; additional legacy backend literals require curated cleanup |
| 20 | Onboarding, release notes and final delivery | README, onboarding, UAT release generator, PROD verbatim reuse, Lighthouse workflow | Owner must publish tags after an approved deploy, provide external runtime integrations, and accept release procedures |

### POPRAVKE.txt cross-check

The POPRAVKE items were cross-checked with the earlier requirements matrix and this full-page review: legal entity identifier/residency, special relationship basis and associated description, additional Excel metadata and linked IDs, GCC and large-exposure fields, four-eyes, local language/runtime codebook editing, exact role-request authorizations, notification inbox, codebook/audit UX, navigation/guide/keyboard behavior, and version notes. Functions are marked **PARTIAL** above when only BrowserHost fixture or known internal evidence exists.

**Controlled release gate (owner-owned):** Current tracked secrets remain blank; Git history scanner intentionally detects historical exposure, and image deployment is blocked until authorized remediation. Do not disable the gate or use fake production credentials.
