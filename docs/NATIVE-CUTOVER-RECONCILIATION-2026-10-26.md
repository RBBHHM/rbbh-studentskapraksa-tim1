# Final native integration and React source retirement â€” 2026-10-26

Base for this continuation:43d51b605b35785798cdc211447bce7ed2a92318.
The owner explicitly confirmed Claude's final visual batch and requested integration.
No push, production deployment, production schema application or data backfill occurred.

## Integrated commits and semantic review

- Original Claude:ff2d173db834f583db21d18872c2080eeec35093.
- Cherry-pick:4d821d983ce7e7a40c74c316c4f3c12b227e9782.
- Semantic follow-up:ccca8c5319e1a730f14dfcb0fd8084c5909fb602.
- Source retirement/canonical solution:373e1cb453ec55418b153e023f5a69054393af47.
- The following evidence commit records the final checks; its HEAD is reported separately.

Conflicts occurred in LegalEntities, Limits, PhysicalPersons and Users Razor pages.
The resolution keeps Claude's header/search/table/indicator/icon composition while
preserving virtual Guest display and own-row protection. The semantic review also
covered auto-merges in Capital/Reports, BS/EN resources and browser specs.

The visual batch moved create/import controls into HeroBar and converted row
actions to icons. The original moved controls lacked the new global lock guards.
The follow-up restores them without changing the visual composition. Native
forms keep MutationBlocked, handler guards, state-unavailable fail-closed behavior
and explicit rerender after stale423. Existing physical/legal locked/unavailable
tests now also require disabled Edit/Deactivate. The common period UX cases seed
legal/limit rows and require actual controls to exist and be disabled; there are
no vacuous zero-row button assertions.

Guest is virtual for authenticated identities with no explicit mapping, including
no AppUser row; disabled identities remain denied. Reporting read/download and
Kapital/Admin generate/persist remain distinct. Own-role/self-destructive guards,
confirmed-subject Admin continuity, trusted actor email, additive subject schema
and audit are unchanged by the visual merge. Backend source/business workflows
were not changed in this continuation.

## Gates before and after source retirement

The integrated gate passed624 .NET and247 browser tests BEFORE removing React.
After retirement and canonical-reference updates, fresh checks passed again:

| Gate | Final result |
|---|---|
| Full Release .NET suite |624 PASS:265 backend unit +149 integration +210 frontend; zero failed/skipped |
| Backend subset |414 PASS, included above |
| SQL Server/LocalDB |7/7 PASS: real last-Admin/concurrency, placeholder continuity and schema-upgrade/durable-link audit |
| Native browser |247/247 PASS:Admin77, gost27, Kapital40, PL35, Limiti31, FL37 |
| Browser execution |workers1, retries0; zero unexpected/flaky/skipped in final per-role JSON |
| Axe |37 passing cases,45 scans with no violations; subset of247 |
|375px |23 passing cases; subset of247 |
| Release/analyzers |PASS,0 warnings/errors |
| Release publish |web and API PASS |
| Published native startup |14 routes200 honestly signed-out; live/ready200; RBI CSS200; unconfigured login404 |
| Unchanged backend runtime |existing final isolated API on5293:live/ready200 |
| Workflow validation |actionlint PASS;18 Bash blocks parse;exactly3 versioned root workflows |
| Canonical solution |dotnet sln list discovers all6 maintained projects;no old solution remains |
| git diff --check |PASS |
| Pkcs9.0.21 |retained;fresh public NuGet audit reports no vulnerable packages |
| Corporate audit |BLOCKED:artifacts.rbi.tech DNS/service-index unavailable;no warning suppression |
| Docker |BLOCKED:daemon pipe unavailable;no successful image build claimed |

There was no interrupted or failed run in this continuation's integrated or
post-retirement six-role browser gates. Earlier interrupted/red runs remain
documented in the preceding hardening checkpoint; they are not counted as passes.

Receipts under artifacts/migration-template:
integrated-dotnet.log, integrated-browser-<role>.log,
native-final-build.log, native-final-dotnet.log, native-final-browser-<role>.log,
native-final-publish-{web,api}.log, native-final-startup-{web,api}.log,
native-final-actionlint.log, native-final-audit-{public,corporate}.log,
native-final-docker.log. Full final TRX:artifacts/native-final-test-results.
Final browser JSON/traces/screenshots:artifacts/migration-browser/<role>.
Publish outputs:artifacts/native-final-publish/{web,api}. These are ignored
local verification artifacts, not deployed releases.

A375px dashboard screenshot from the integrated run was visually inspected.
Existing automated mobile and Axe cases pass against the new composition.
Claude's original manual visual qualifications (approximate icons and unmeasured
dark/pixel comparisons) remain in the historical visual report. These functional
results do not claim pixel-identical or complete assistive-technology certification.

## Retirement boundary

275 tracked files under the retired React reference tree were removed only AFTER the integrated gate.
This includes React source/assets, retired JS package tooling manifests/lock, retired bundler/Tailwind/i18next
tooling, static IIS/runtime-config packaging and their obsolete React-only Node
contract tests. Maintained backend and native .NET/Playwright test projects remain.
Prior source is recoverable from Git, including the pre-retirement ccca8c5 tree.

The final root helpers the retired API wrapper and scripts/testing/Test-LocalApi.ps1
were removed with their only remaining React package.json consumer. Their native
replacement is the direct dotnet test/run workflow already documented in README.
There are no tracked root scripts/ files and no new wrapper scripts were added.
Ignored workstation caches/data were not cleaned or staged. Retired cache ignores
remain deliberately to avoid exposing local residue.

RelatedPartiesRegister.Migration.slnx was renamed100% unchanged to
RelatedPartiesRegister.slnx. The old backend-only
Backend/RelatedPartiesRegister.sln was removed. CI build/CodeQL,
README/native instructions and all four test-root discovery sites were updated
together. The solution contains backend, native app, backend unit/integration,
frontend tests and the native browser host. No React build is required.

Exactly these workflow files remain:

- .github/workflows/build-and-push-image.yml
- .github/workflows/codeql.yml
- .github/workflows/deploy-to-ocp.yml

The build publishes native web/API separately after gates, refuses overwriting
commit tags and records digests. Deploy remains manual and digest-pinned with
mandatory configuration and verified TLS. No registry/cluster workflow was run.

## Preserved user state and external gates

User file tests/Web.Blazor.BrowserHost/Properties/launchSettings.json remains
untracked, unstaged and byte-identical:
SHA25607DAA11FF2D5D953B78D8FE4C7209AF625C3D76F2FBEC747986EB6F8DF39EC93.
No git clean, rebase, push, real credentials lookup or unrelated-host stop occurred.

The new published signed-out native smoke host is on loopback5294.
Earlier isolated probes5291/5292 and the unchanged final API5293 remain;
existing user hosts5000/5080 and the occupied5273 host were not stopped/replaced.
Source retirement is complete; no further work waits for Claude's commit delivery.

Production cutover remains external:real realm email_verified/sub/export approval,
one-time subject manifest/backfill, historical global-period reconciliation,
deployed SQL migration/concurrency permissions, reviewed tracked RBI source,
real confidential OIDC/session/logout/refresh, shared cache/DataProtection,
SMTP/HTTP recipients/delivery, OCP/proxy/WebSocket/TLS/runner/registry configuration,
corporate advisory-feed and runtime-servicing acceptance. F3 stays PARTIAL for
production data although code/schema and durable-link tests are ready.
