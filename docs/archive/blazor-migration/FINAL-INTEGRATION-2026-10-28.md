# Final integration — 2026-10-28

**Result: SAFE_TO_PUSH_FOR_PR. No push performed. Production deployment remains blocked by the accepted infrastructure prerequisites.**

Starting HEAD: `339ab3b7be3ce19266529d81584fe517b34fd557`, branch `release-v1.0.6`.
Accepted backend/refactor/operations commits `a5265fb`, `9b7338a`, `b2019b1`, `5ad5554`, `6816ff8`, `024d6eb`, `339ab3b` remain intact in ancestry. No squash, rebase or backend redesign occurred.

## Integrated commits, in requested order

| Original Claude commit | Cherry-picked commit | Scope |
| --- | --- | --- |
| 87f76ed | f496dba | Shell, icons, immediate language apply |
| 88e4755 | 5604101 | Tables, forms, dialogs, import presentation |
| 472186b | b4f2899 | Limits typeahead |
| 1a6af3d | 2472116 | Capital and guide presentation |
| 95ffbb3 | bd87b7b | Responsive/theme styling and matching tests |
| 9621b27 | 726fd66 | Dark fields and compact tables |
| 896cea9 | 4383e7b | Second visual pass and evidence |
| d687011 | f572ac8 | Verify/toast/dialog/date presentation |
| 2cc2c9e | e97f6c2 | Capital empty state and upload-error toast |
| 3e54a13 | 8659a2d | Import/reactivation confirmation and remaining states |

Optional `2dd254f` was not separately cherry-picked: its initial evidence content was already retained in the richer incoming document during reconciliation. Applying it again would duplicate/regress the later document.

One Git conflict: **modify/delete of `docs/CLAUDE-FINAL-VISUAL-ACCEPTANCE.md` at 896cea9**. It was absent from the accepted base, while this commit expected the earlier optional evidence file. Retained the complete incoming document and allowed later commits to extend it. No production-code conflicts occurred.

One necessary integration correction: `5077566b16bc24da1d10cb6edfc2b3859e60a8c0`, `fix(web): retain accessible guide contrast in dark theme`. The additional gate found Guide eyebrow contrast 3.99:1 on its dark brand-tinted background at both widths. A single dark-only CSS rule now uses the existing primary-text token. No layout, copy, permissions, background or accepted parity decision changed. A persistent browser regression runs at desktop/375px for every role; no assertion, Axe rule, retry or timeout was weakened.

This evidence document is committed separately after that correction. Its final documentation commit/branch HEAD is supplied in the handoff and git history.

## Semantic preservation and API contracts

Backend production source, DbContext/models/migrations, role/identity logic, application services, startup/proxy/health safeguards, Docker contexts and all workflows are byte-equivalent to the starting base according to scoped git diff. Frontend API clients and security adapters are also unchanged. Relative to Claude's final frontend tree, the only changes are the retained NuGet publish exclusion, the narrow contrast correction and its regression test.

Checked all client families against maintained controllers/DTOs and their .NET/real-pipeline browser contracts:

| Family | Preserved contract |
| --- | --- |
| Physical persons | Related-person list/detail/tree/duplicate routes; integer residency/status fields; CRUD and separate verification; multipart import and XLSX download |
| Legal persons | `items,total,page,pageSize`; GUID detail/verification routes, residency/identifiers, lookup/form data, multipart import/export |
| Limits | `api/limiti` list/detail/write/delete/export; GUID legal-entity selection and search/form-data endpoints; decimal amounts and global capital fields |
| Capital | Optional singleton: JSON null/204 distinct from error; typed decimal/date upsert and binary export; no fabricated zero record |
| Reports | `items,total,page,pageSize`; bounded monthly inputs, persisted generation and generated/client/all exports; authenticated non-inactive reads, Kapital/Admin generation |
| Notifications | `provider,emails`; recorded purpose/status/body are scalar data, never executed HTML or fictional delivery evidence |
| Users | `users,total,page,pageSize`; role catalog, multiple/zero explicit role IDs, local GUID actions, lifecycle and audit; self/final confirmed-active-Admin protection retained |
| Period | Current global state, `requests,total,page,pageSize`, lock/unlock/request-info/reject/respond bodies; 10–490 response limit, backend 423 and uncertainty recovery |
| Code lists | Categories/value envelopes and existing definitions/values; immutable codes; preview `dryRun`/confirm multipart flow; history and reference propagation |
| Audit | `logs,total,page,pageSize` and human display fields; server paging reaches beyond 200; no first-page-only React contract |
| Profile | Identity/email plus DB-owned roles/permissions/applicationUserId; provider claims cannot grant application access; outage remains explicit |

No client was adapted to an obsolete React response shape. Self-role/delete/deactivate guards, SQL transaction-owned last-Admin serialization, subject binding, virtual Guest and inactive denial, reporting permissions, global lock and production fail-closed startup remain intact. SQL/HTTP/browser results below exercise these paths on the integrated tree.

Accepted differences remain: stronger RBI input borders; field validation plus summary and required asterisks; wrapping code-list actions at 375px; disabled self row; hidden Admin navigation for Guest; period notice/disabled mutations; redacted imports; zero explicit roles retaining virtual Guest; shell plus inline retry during identity outage; native Audit server pager. Immediate language apply, removed Refresh buttons, forms/typeahead/BAM/guide/mobile/toasts remain as accepted.

## Fresh final gates

Final source was rebuilt, retested and republished after the contrast correction. Browser suites ran sequentially in the requested order, workers=1, retries=0. No skipped test is counted as passed.

| Gate | Final result |
| --- | --- |
| Backend unit | **278 passed**, 0 failed/skipped |
| Backend integration | **162 passed**, 0 failed/skipped |
| SQL/LocalDB | **8 actual passes**, included in integration: six last-Admin/concurrency/lock cases, subject-link upgrade/audit, readiness SQL connection |
| Blazor .NET | **210 passed**, 0 failed/skipped |
| Complete .NET solution | **650/650 PASS** (278 + 162 + 210; backend subset 440) |
| Release/analyzer build | PASS, 0 warnings, 0 errors |
| Backend Release publish | PASS, `artifacts/final-integration/final-publish/api` |
| Native Release publish | PASS, `artifacts/final-integration/final-publish/web` |
| Admin browser | **78/78 PASS** |
| FL browser | **38/38 PASS** |
| PL browser | **36/36 PASS** |
| Kapital browser | **41/41 PASS** |
| Limiti browser | **32/32 PASS** |
| Guest browser | **28/28 PASS** |
| Six-role browser total | **253/253**, 0 skipped/unexpected/flaky; original 247 plus one new regression per role |
| Additional theme/route matrix | **52/52 PASS**: 13 routes × light/dark × 1440px/375px |
| All final browser executions | **305 PASS** (253 + 52), zero retries |
| Axe | All assertions pass: 43 role-suite cases containing Axe plus 52 additional matrix analyses; zero violations in final matrix |
| 375px | 29 role-suite cases plus 26 additional light/dark route cases; no page overflow; dialogs/focus covered in role suites |
| Light / dark | 26 additional cases each, real theme-button interaction, persisted preference after reload and screenshots; dark Guide regression also passes for all six roles |
| Published startup/live/ready | Both isolated Development hosts started; `/health/live` and `/health/ready` returned 200; native deep links and a published RBI CSS endpoint returned 200 |
| Production misconfiguration checks | Expected fail-closed for API missing auth, native missing auth and native missing shared IDistributedCache |
| Authenticated production startup | NOT_RUN_INFRASTRUCTURE; accepted shared-state/SSO/proxy prerequisites remain unresolved |
| Workflow YAML | Exactly three root YAMLs parsed; 19 embedded scripts pass `bash -n`; source hashes recorded |
| Generated deployment manifest | Deployment + Service parse and pass digest/probe/Host/security/port/rollout invariants; structural check, not live admission validation |
| Docker | **NOT_RUN_INFRASTRUCTURE**, named pipe/daemon unavailable |
| OCP | **NOT_RUN_INFRASTRUCTURE**, oc unavailable; no cluster credentials or deployment used |
| git diff --check | PASS, including integration commit range |

The first additional matrix run had two dark-Guide failures (50/52); this evidence is retained. The one-line correction was followed by fresh full .NET/SQL, build/publish, all six role suites, the entire additional matrix and startup checks. Final totals above are from the corrected tree, not retries hiding the initial failure. Axe tags remain wcag2a, wcag2aa and wcag21aa. This is integration verification of the user's accepted visual classification, not a new claim that every pixel matches React.

## Repository and artifact hygiene

- Sole canonical solution: `RelatedPartiesRegister.slnx`.
- Exactly `.github/workflows/build-and-push-image.yml`, `codeql.yml`, `deploy-to-ocp.yml`; no nested workflows.
- No tracked legacy React source, React/retired JS package tooling product build, retired root wrappers or migration-only tooling were reintroduced. Node remains limited to native browser tests.
- No package, schema, backfill, production infrastructure values or mass formatting changes. Existing unrelated style debt remains separate. Pkcs 9.0.21 and warning behavior are unchanged.
- Existing untracked `tests/Web.Blazor.BrowserHost/Properties/launchSettings.json` was never staged/edited. SHA-256 before/after: **07DAA11FF2D5D953B78D8FE4C7209AF625C3D76F2FBEC747986EB6F8DF39EC93**.

Ignored receipts under `artifacts/final-integration/`: `final-dotnet/`, `dotnet-summary.json`, `final-browser/`, `final-browser-summary.json`, `theme/results.json` and screenshots/Axe attachments, `startup-smoke.json`, `workflows/result.json` and `manifest.fixture.yml`, `repository-contract.json`. Initial contrast failure evidence is under `theme/initial-red*`. Supplemental matrix/smoke scripts are ignored test evidence, not product tooling or production authentication.

## Remaining deployment prerequisites

Unchanged from [the production-readiness audit](PRODUCTION-READINESS.md): approved shared session cache registration, persisted/encrypted Data Protection keys, real proxy trust values, OCP HTTPS/SSO/WebSocket validation, resource requests/limits, termination/drain policy, scheduler overlap strategy, private-feed production access, real SQL/mail/certificate validation, approved Keycloak subject-link backfill/data reconciliation and actual-infrastructure rollback rehearsal. No local substitute or invented RBBH values were introduced.

**Branch classification: SAFE_TO_PUSH_FOR_PR. It is not approved for production deployment. No push was performed.**
