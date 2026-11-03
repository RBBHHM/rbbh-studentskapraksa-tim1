# Backend findings for Claude integration

Backend input: `22f0376ccb6cc79d4d79ada1c967dc7d776d6465`. No changes to the integration worktree are authorized or performed here. Use [the exact final contract](BACKEND-FRONTEND-CONTRACT-FINAL.md) and [integration handoff](INTEGRATION-HANDOFF-CODEX.md) for full shapes and ancestry.

## Audit query boundary defect (RESOLVED, backend-owned)

`Backend/API/Controllers/AuditLogController.cs`, GET `/api/audit-logs`:

- An authorized Admin requesting `?page=2147483647&pageSize=200` overflows the int32 SQL offset and receives500 instead of a successful empty far page.
- `?dateFrom=9999-12-31&dateTo=9999-12-31` adds an unrepresentable day and receives500 instead of matching the final calendar day.
- Fix commit: `bddc9136f948c7d8a3c1c3f141d1e461fcc09f5d`. Regression: `tests/Backend.IntegrationTests/Tests/Operations/AuditQuerySqlTests.cs`, real HTTP/DB permissions and disposable SQL. Before correction2 FAIL / 1 PASS; after correction 3 PASS. The patch saturates offset using int64 and avoids adding a day beyond DateTime.MaxValue. Full backend regression337 unit + 395 integration PASS. No route, DTO, role/permission, business calendar rule, migration or normal filter behavior change. Frontend action: retain current query contract and test ordinary inclusive dates after integrating the backend fix; do not add a workaround.

## Tracked secret and rotation (frontend/security owner)

`Frontend/appsettings.json`, `Authentication:ClientSecret`, is still nonempty in this backend branch. This is a redacted finding; do not copy the value into chat/logs/docs/tests. Claude's source cleanup must survive the eventual integration. Verify the merged tracked configuration is empty/externalized and obtain realm-owner confirmation of rotation/revocation of the historical credential before deployment. Deleting a current value does not revoke history. Codex will not edit this frontend file, rewrite history or rotate a production credential.

## Existing contract actions still required

- `Frontend/Api/NotificationsApi.cs` currently consumes `/api/email-log`; production inbox/bell must consume `/api/notifications` with `unreadCount`, `route`, parameters and single-item read204. No `/unread-count` or `/read-all` exists.
- Role requests use `purpose`, GUID `roleIds`, `clientRequestId`; decisions require `decisionId`/`expectedVersion`. Choices `/api/role-requests/options`, not administrative `/api/roles` for Guest.
- Processor period list is `/api/period-lock/unlock-requests/review`; own list remains `/unlock-requests`. Preserve Capital processing and locked capital/verification UX. Respond10–490.
- GCC uses `/api/bank-reference/gcc`; capital history appends via existing PUT `/api/capital`; runtime pack uses `/api/localization/resources/{language}` with items/version, not a flat map. Imports keep string errors plus structured details. Definition metadata uses modifiedAt/modifiedBy.
- `UserDTO.identityConfirmed` is read-only; do not use application row id as provider sub. Hide own-row role controls. Four-eyes creator failure code is `verification.self_denied`, historical provenance409, not `verification.same-user`. Creator display-name comparisons are advisory only.
- Release-notes API/GitHub adapter is MISSING. Runtime PeriodRequestStatus/RecordState categories are not guaranteed seeds. Legacy display labels are only partially localized. Do not invent DTOs, endpoints, catalogs or translate invariant workflow codes into writes.
- Legal controller comment claims identifier immutability but current service validates and assigns replacement residency/tax/FBA. Do not claim a backend immutability guarantee from that comment. No policy change was introduced.

## Pending changes requiring coordination, not frontend workarounds

- `Backend/IoC/Extensions/Authentication/AuthenticationServiceExtensions.cs` accepts cookie-authenticated API calls; `AuthenticationEndpoints.cs` validates antiforgery for POST logout only and retains deprecated GET logout. Coordinate bearer-only API versus cookie+CSRF before changing backend requirements. Frontend RBI logout has its own origin/cookie/token contract; do not forward the wrong origin's antiforgery token.
- `Backend/BL/Services/RelatedPersonService.cs`, GET `/api/related-persons/{id}/relationship-tree`, loads all active people and recursively builds the response. Deep/large-tree acceptance is unverified; agree a flat/paged/bounded contract before claiming unlimited depth. Current DTO remains unchanged.
- `Backend/BL/Services/ReportService.cs`, GET `/api/reports/export/all-clients` and `/export/client/{identifier}`, has no row bound in its current-export query. Coordinate limits/admission/streaming and error handling before restricting existing downloads. Generated snapshots have a different20,000-row bound; registry exports5,000.
- Direct administration through POST `/api/users`, POST `/{userId}/roles` or compatibility PUT `/{userId}/role`, DELETE deactivate/permanent currently audits changes without staging user-lifecycle inbox/outbox events. Role-request decisions and period actions do stage notices. Do not promise welcome/change/removal notifications for every direct Admin mutation until event/recipient policy is agreed and implemented.

## BrowserHost incompatibility in the backend checkout

`tests/Web.Blazor.BrowserHost/BrowserHostProgram.cs:25,26,192` fails with CS0234/CS0246 while frontend/backend both declare DATA_CORE. Preserve Claude's `59b8d382ce5cd192e32e9a3a51c472adf8ea960a` frontend assembly-name fix and frontend entrypoint on integration. No backend assembly rename or test workaround is needed. Backend-only tests do not establish six-role browser success; keep Claude's integration assertions intact.
