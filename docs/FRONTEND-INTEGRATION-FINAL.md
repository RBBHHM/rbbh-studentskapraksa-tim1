# Frontend integration (final)

This describes how the Blazor frontend is wired to the finalized backend on branch `main`, what was verified, and what is still open. The per-requirement view is in [FINAL-REQUIREMENTS-COVERAGE.md](FINAL-REQUIREMENTS-COVERAGE.md); the backend contract is [BACKEND-FRONTEND-CONTRACT-FINAL.md](BACKEND-FRONTEND-CONTRACT-FINAL.md).

## Integrated endpoints

All clients are typed over the RBI transport (`IRbiApiTransport` / `IRbiApiFileTransport`) in `Frontend/Api/`; no endpoint outside the backend contract is called and there are no permanent mocks (an `Unconfigured*` client only reports "service unavailable" when no API base address is configured).

| Area | Frontend client | Backend routes used |
|---|---|---|
| Profile / session | `ProfileApi` | `GET api/auth/profile` |
| Users | `UsersApi` | `api/users` (list, create), `PUT api/users/{id}` (profile), `{id}/role`, `{id}/reactivate`, `{id}/permanent` |
| Role requests | `RoleRequestsApi` | `api/role-requests` (own list, create, admin review, `{id}/approve|reject` with `decisionId` + `expectedVersion`), `api/roles` |
| Role catalog | `RoleCatalogApi` | `api/role-catalog` (list, permission catalog, create, optimistic update); Admin-only editing UI |
| Notifications | `NotificationsApi` | `GET api/notifications`, `POST api/notifications/{id}/read` |
| Legal persons | `LegalEntitiesApi` | `api/legal-entities` (incl. `{id}/limit-form-data`), `api/bank-reference/gcc`, `api/bank-reference/customers`, `api/verification/legal-person/{id}`, workbook import/export |
| Physical persons | `PhysicalPersonsApi` | `api/related-persons` (incl. `check-duplicate`, `{id}/relationship-tree`), `api/verification/physical-person/{id}`, workbook import/export |
| Limits | `LimitsApi` | `api/limiti`, `api/limiti/import`, export |
| Capital | `CapitalApi` | `api/capital`, `/history`, `/as-of` |
| Period | `PeriodApi` | `api/period-lock/unlock-requests` (own list, `/review` queue, respond) |
| Code lists | `CodeListsApi` | `api/code-lists/{category}` (values, import), `api/code-lists/dropdown/{category}`, `categories/definitions` |
| Reports | `ReportsApi` | `api/reports` (daily, monthly, list, export all/client/generated) |
| Audit | `AuditApi` | `api/audit-logs` |
| Localization | `LocalizationApi` | `api/localization/languages`, `api/localization/resources/{language}` |
| Release notes | `ReleaseNotesApi` | `GET api/release-notes?environment=uat|prod`, cached public GitHub Releases source |

Not consumed although the contract has it: `api/auth/modules` (sidebar uses effective profile permissions).

## Behaviours worth knowing

* **Errors**: RBI transport now extracts only bounded, syntactically valid backend `code` values. Legal/physical verification maps the approved four-eyes denials (`verification.self_denied`, `verification.provenance_required`) to safe, localized text. Unapproved codes and untrusted upstream details are never displayed.
* **Role-request decisions** derive `decisionId` deterministically from request id, version and action, so a repeated click replays instead of creating a second decision.
* **GCC** returns 503 while no bank source is configured; the picker says the source is unavailable and offers nothing. The BrowserHost registers a *test-only* fixture source to exercise the available path.
* **Reports** show `dataSource` as an origin label. `isAuthoritative` means origin, not regulatory acceptance, and the UI never says "reconciled".
* **Capital** writes append immutable revisions; the page shows history and an as-of lookup (204 means "no value then").
* **Session**: the RBI cookie is absolute 8 h with no sliding renewal; logout is a POST with antiforgery. The frontend does not call the backend logout.
* **Localization**: all 608 UI texts in BS/EN/DE have parity and placeholder checks. `SeedFrontendLanguagePackResources` migrates them to the existing database `LanguagePacks` without replacing approved translations; runtime resource versions invalidate caches immediately. Static resources are safe fallbacks.
* **Code-list metadata** (created-by, last modified) is fetched separately and is advisory: if that call fails, the lists still render.

## Test harness and what it proves

`tests/Web.Blazor.BrowserHost` hosts the real frontend and the real backend controllers, services and validators in one process on an **InMemory** database with fixture identities, one per role (admin, FL, PL, kapital, limiti, gost). It registers services and named API clients by hand, so a new backend service needs an explicit registration there.

It proves: UI ↔ real controller/validator contracts, authorization policies per role, accessibility, keyboard behaviour and responsive layout.
It does **not** prove: real SQL behaviour, Keycloak, the bank reference source, Finrep, mail, or deployment. Backend integration tests against LocalDB (`RBBH_TEST_SQLSERVER`) cover the real SQL path for the backend only.

Two pitfalls found during this integration:

* `dotnet run --no-build` serves static assets recorded at build time. After editing `Frontend/wwwroot/app.css` the host **must be rebuilt**, otherwise pages load without the application stylesheet and layout/visibility tests fail in confusing ways.
* The host build fails if a frontend type name collides with a backend entity namespace imported by `BrowserHostProgram.cs` (e.g. `BankGroup`, `CodeListDefinition`); a failed build leaves the previous host in place and silently stale.

### Commands

```powershell
dotnet build RelatedPartiesRegister.slnx -c Release -warnaserror
dotnet test tests/Web.Blazor.Tests -c Release
dotnet test tests/Backend.UnitTests -c Release
$env:RBBH_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true'
dotnet test tests/Backend.IntegrationTests -c Release -m:1 -nr:false
dotnet build tests/Web.Blazor.BrowserHost/Web.Blazor.BrowserHost.csproj
cd tests/Web.Blazor.Browser
$env:BROWSER_FIXTURE_ROLE = 'admin'   # then FL, PL, kapital, limiti, gost, one at a time
npx playwright test
python scripts/security/scan_git_secrets.py --ref HEAD
```

## Mismatches between the old frontend and the final contract (all resolved)

| Was | Now |
|---|---|
| Role requests `type: Request/Change` | `purpose` `new`/`change`, role GUIDs, `clientRequestId`, lowercase statuses |
| Notification list with `unread` | `{items,total,page,pageSize,unreadCount}`; mark-read returns 204 |
| One unlock-request list | own list vs capital-read review queue |
| Mutable capital | immutable revisions plus history and as-of |
| Corporate-domain email restriction | any valid email; profile editable |
| GCC/large exposure absent | `gccNumber`, `gccName`, nullable `isLargeExposure` |

## Open items and dependencies

1. **Credential rotation.** The old Keycloak client secret is still reachable from public `main`. The owner must rotate it and approve historic exposure remediation. Current tracked configuration is blank, but the full-history main scan correctly fails and image publication is blocked.
2. **Production Keycloak** (client, realm session policy, audience) and the **8-hour session** need a real login test.
3. **Real data sources**: bank reference (`bosnadb\HEAD`) for GCC/customers, Finrep monthly history, a mail provider.
4. **Release notes**: UAT `uat-vX.Y.Z` tags generate a published prerelease automatically. PROD `vX.Y.Z` tags must point to the exact same commit and reuse the original UAT notes verbatim. No releases have been published yet.
5. **Structured errors**: four-eyes codes are now mapped safely; a full-domain message mapping is still open.
6. **DE frontend catalog** has all 608 keys and automated browser/unit/SQL codebook tests. Specialist legal/financial wording needs bank approval; role-catalog and bank-customer UIs are already implemented.
7. **Static status lists** (Period statuses, Guide states) stay allow-listed until the backend exposes catalogs.
8. A **frontend → deployed API → SQL** browser smoke test needs a provisioned environment.
9. **Lighthouse** runs on the isolated BrowserHost: dashboard/guide desktop 99/99 and mobile 79/82; all accessibility and best practices 100. An authenticated, deployed mobile performance audit is still required.
