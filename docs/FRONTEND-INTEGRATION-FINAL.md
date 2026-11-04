# Frontend integration (final)

This describes how the Blazor frontend is wired to the finalized backend on branch `integration/full-stack-final`, what was verified, and what is still open. The per-requirement view is in [FINAL-REQUIREMENTS-COVERAGE.md](FINAL-REQUIREMENTS-COVERAGE.md); the backend contract is [BACKEND-FRONTEND-CONTRACT-FINAL.md](BACKEND-FRONTEND-CONTRACT-FINAL.md).

## Integrated endpoints

All clients are typed over the RBI transport (`IRbiApiTransport` / `IRbiApiFileTransport`) in `Frontend/Api/`; no endpoint outside the backend contract is called and there are no permanent mocks (an `Unconfigured*` client only reports "service unavailable" when no API base address is configured).

| Area | Frontend client | Backend routes used |
|---|---|---|
| Profile / session | `ProfileApi` | `GET api/auth/profile` |
| Users | `UsersApi` | `api/users` (list, create), `PUT api/users/{id}` (profile), `{id}/role`, `{id}/reactivate`, `{id}/permanent` |
| Role requests | `RoleRequestsApi` | `api/role-requests` (own list, create, admin review, `{id}/approve|reject` with `decisionId` + `expectedVersion`), `api/roles` |
| Notifications | `NotificationsApi` | `GET api/notifications`, `POST api/notifications/{id}/read` |
| Legal persons | `LegalEntitiesApi` | `api/legal-entities` (incl. `{id}/limit-form-data`), `api/bank-reference/gcc`, `api/verification/legal-person/{id}`, workbook import/export |
| Physical persons | `PhysicalPersonsApi` | `api/related-persons` (incl. `check-duplicate`, `{id}/relationship-tree`), `api/verification/physical-person/{id}`, workbook import/export |
| Limits | `LimitsApi` | `api/limiti`, `api/limiti/import`, export |
| Capital | `CapitalApi` | `api/capital`, `/history`, `/as-of` |
| Period | `PeriodApi` | `api/period-lock/unlock-requests` (own list, `/review` queue, respond) |
| Code lists | `CodeListsApi` | `api/code-lists/{category}` (values, import), `api/code-lists/dropdown/{category}`, `categories/definitions` |
| Reports | `ReportsApi` | `api/reports` (daily, monthly, list, export all/client/generated) |
| Audit | `AuditApi` | `api/audit-logs` |
| Localization | `LocalizationApi` | `api/localization/languages`, `api/localization/resources/{language}` |

Not consumed although the contract has them: role catalog mutations, bank customer lookup, `api/modules`.

## Behaviours worth knowing

* **Errors** reach the UI as an HTTP status only. The RBI transport does not expose the backend `code` extension, so four-eyes denials (`verification.self_denied`, `provenance_required`) display the generic forbidden/conflict text. The server still enforces them.
* **Role-request decisions** derive `decisionId` deterministically from request id, version and action, so a repeated click replays instead of creating a second decision.
* **GCC** returns 503 while no bank source is configured; the picker says the source is unavailable and offers nothing. The BrowserHost registers a *test-only* fixture source to exercise the available path.
* **Reports** show `dataSource` as an origin label. `isAuthoritative` means origin, not regulatory acceptance, and the UI never says "reconciled".
* **Capital** writes append immutable revisions; the page shows history and an as-of lookup (204 means "no value then").
* **Session**: the RBI cookie is absolute 8 h with no sliding renewal; logout is a POST with antiforgery. The frontend does not call the backend logout.
* **Localization**: the BS/EN catalogs in `Frontend/Localization` are authoritative for UI chrome and are guarded by key-parity and no-literal-text tests; the backend language pack (BS/EN/DE) supplies server-owned text such as notification titles. The language picker has no DE entry.
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

1. **Credential rotation.** The old Keycloak client secret is still reachable from public `main`. The owner must rotate it; this branch does not contain it and its scan is clean. Do not merge to `main` before rotation and a decision on how to retire the old history.
2. **Production Keycloak** (client, realm session policy, audience) and the **8-hour session** need a real login test.
3. **Real data sources**: bank reference (`bosnadb\HEAD`) for GCC/customers, Finrep monthly history, a mail provider.
4. **Release notes**: needs a backend route that reads GitHub server-side.
5. **Structured errors**: extend the RBI transport to surface the backend `code`.
6. **DE frontend catalog**, **role-catalog UI**, **bank-customer lookup UI** need a design decision.
7. **Static status lists** (Period statuses, Guide states) stay allow-listed until the backend exposes catalogs.
8. A **frontend → deployed API → SQL** browser smoke test needs a provisioned environment.
9. **Lighthouse** performance pass is deferred.
