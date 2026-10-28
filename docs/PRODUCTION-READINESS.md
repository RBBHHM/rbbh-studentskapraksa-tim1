# Production readiness — 2026-10-28

Audited baseline: `5ad555448a175790fbf2b9d8d856ffd870328d03`, branch `release-v1.0.6`. **Verdict: BLOCKED for production deployment.** Local verification is green; the authenticated native production host still cannot start without a selected/registered shared session provider. Frontend visual acceptance is independent and still pending. No push or live deployment was performed.

## Findings and release blockers

| Severity | Finding / required disposition |
| --- | --- |
| BLOCKER | **Native shared session provider is absent in application registration.** Production auth calls RBI.Authentication, which requires IDistributedCache before registration. The published production smoke confirms rejection. An environment variable alone cannot add this provider: approve the RBBH provider, implement its registration, shared Data Protection key storage/encryption/application discriminator, then prove SSO, refresh, restart and revocation across pods. No production memory-cache fallback was added. |
| HIGH | Native RBI.Hosting currently trusts framework loopback only. Real OCP proxy trust is not bound from deployment configuration, so HTTPS redirects/callbacks behind remote proxies remain unverified. Approve actual proxy addresses/CIDRs/hop count, bind native hosting options, and test public scheme/host/IP and WebSockets. API trust-all was fixed separately below; it does not fix the native host. |
| HIGH | API/SSO/SQL/mail endpoints, CA chains, AllowedHosts, real audience, redirect URIs, email_verified, subject-link approval/backfill and realm reconciliation remain environment-owner gates. Native Apis__BaseUrl is not startup-required today; blank configuration gives unavailable clients, not a usable application. Confirm it explicitly and add a critical native readiness dependency once the shared-provider integration is chosen. |
| HIGH | OCP template has one replica, default RollingUpdate behavior, no explicit resource requests/limits, termination budget, PDB, persistent/shared state, Route or network policy. Native circuits, cookie/session continuity, surge/overlap with the API scheduler, WebSocket affinity and capacity need owner-approved design/values. No resource numbers or storage/provider assumptions were invented. |
| RESOLVED | RBI runtime dependencies are now tracked source projects in Frontend/Platform. Only normal third-party packages require a public source or approved mirror; no private RBI publication is a prerequisite. |
| HIGH | No Docker daemon or live OCP/registry access here. Linux image/SCC/CA behavior, real production startup, public-route health, rollout and SIGTERM drain are NOT_RUN_INFRASTRUCTURE. These are release gates, not local PASS results. |
| MEDIUM | Actions/base images/SQL test image use moving version tags; there is no SDK global.json or NuGet lock file. Commit-tag refusal and digest deployment help promotion, but strict clean-build reproducibility/provenance and registry write-once policy still need owner controls. Deploy accepts an operator-supplied tag/digest pair, not a verified signed build attestation. |
| MEDIUM | SQL readiness checks connectivity, not migration completion, permissions, role/reference seed correctness or mail/SSO readiness. Native readiness has no critical application dependency registered. Schema/data/SSO acceptance must be checked before traffic cutover; an HTTP 200 alone is insufficient. |
| MEDIUM | API /metrics is middleware-exposed; restrict scrape/ingress through approved network/auth policy. Logs/audit carry business identities; approve retention, access, redaction and alerts. No new telemetry vendor was added. |
| MEDIUM | Exports/full physical lists/user candidate sorting are intentionally unbounded; workbooks are memory-resident. Imports have 10 MB / 5,000-row limits. Measure exports, shutdown and SQL pools under agreed load before selecting pod resources. No speculative optimization or backend refactor was repeated. |

## Safe fixes in this audit

- API now rejects missing/disabled/incomplete Keycloak outside Development, validates named JWT/OIDC options at host startup, and rejects missing non-development DB configuration at registration. Existing audience/signature/lifetime/issuer validation, zero clock skew, Guest/application permissions and business HTTP responses remain unchanged.
- API no longer clears proxy trust to accept everyone. Production requires explicit ReverseProxy KnownProxies/KnownNetworks, positive ForwardLimit and non-wildcard AllowedHosts. Invalid IP/CIDR, /0 networks and missing settings fail closed. Development keeps framework loopback defaults. HTTP tests prove peer trust, host filtering, forwarded scheme and audit address behavior.
- API health endpoints explicitly allow anonymous probes, retain 200/503 semantics and return only status. Dependency failure does not change process liveness. SQL connectivity is exercised against a test-owned GUID LocalDB database.
- OCP startup probe now uses /health/live, not dependency readiness. Readiness still gates traffic through /health/ready. Probe Host is explicitly owner-configured to match AllowedHosts, instead of using the changing pod IP. Deployment credentials use a job-scoped KUBECONFIG removed with an always cleanup step.
- Docker context and Git ignore patterns cover local env/secrets/private keys/tooling. The root NuGet.config is outside both application publish inputs. Final published trees contain no NuGet config, source/project files, dotenv or private keys. No credential was introduced or printed.

## Required configuration inventory

Names below use environment-variable syntax. Values belong to the environment owner; no actual secrets are included.

| Class | Component / variables |
| --- | --- |
| REQUIRED_STARTUP | API: KeycloakSettings__Enabled=true, KeycloakSettings__Issuer, KeycloakSettings__ClientId, KeycloakSettings__ClientSecret, KeycloakSettings__Audience. Issuer/metadata must use HTTPS outside the narrowly permitted Development loopback exception. PublicIssuer is optional and must match an approved token issuer. |
| REQUIRED_STARTUP | API: Database__ServerName, Database__Name; Database__IntegratedSecurity plus Database__User/Database__Password when not using integrated authentication. Production SQL must have a trusted CA/hostname, working network and least-privilege runtime login. Do not configure LocalDB in OCP. |
| REQUIRED_STARTUP | API: ReverseProxy__KnownProxies__N and/or ReverseProxy__KnownNetworks__N, ReverseProxy__ForwardLimit, explicit semicolon-separated AllowedHosts. Networks must be bounded CIDRs. Set the actual topology; do not copy test addresses, clear trust lists, use /0, or blindly enable ASPNETCORE_FORWARDEDHEADERS_ENABLED. Include the selected probe Host in AllowedHosts. |
| REQUIRED_STARTUP | Native web: Authentication__Authority, Authentication__ClientId, Authentication__ClientSecret, plus the approved shared IDistributedCache registration. The provider registration/key-ring work is not implemented by current environment variables. Native proxy trust also needs implementation/approved topology. |
| REQUIRED_STARTUP / release gate | Both hosts: ASPNETCORE_ENVIRONMENT=Production (UAT for UAT), HTTP listener 8080 behind approved TLS termination, TZ=Europe/Sarajevo, trusted enterprise CAs. Native Apis__BaseUrl must be a real HTTPS backend URL ending in /; current code validates a supplied URL but does not reject a blank one. Production must not use Development. |
| OPTIONAL_FEATURE | KeycloakSettings__AdminClientId / AdminClientSecret enable provider-side lookup for user creation when Keycloak administration is enabled. Provision least privileges for actual lookup calls; DB role management does not require granting realm-role write permissions. |
| OPTIONAL_FEATURE | Email__Provider and transport: httpapi uses Email__SendEmailWithHtmlBodyUrl (HTTPS), Email__From; smtp uses Email__Smtp__Host/Port/EnableSsl/FromAddress/FromName and relay-approved Username/Password if required. Email__AdminEmail/HrEmail and HTTPS Email__AppBaseUrl must be real approved recipients/public web URL before notification workflows are accepted. Existing non-demo startup validation checks transport/sender/recipients. Demo is not delivery proof; the checked-in localhost link is not a production setting. |
| OPTIONAL_FEATURE | Cors__AllowedOrigins__N: explicit browser origins only if browser-to-API access is required. Native token transport is server-to-server. No wildcard/credential combination. Health/scrape endpoints, telemetry retention and alerts need environment policy. |
| DEVELOPMENT_ONLY | Development authentication and InMemory fallback, local memory session cache, loopback HTTP metadata exception, local dotenv/user-secrets and BROWSER_FIXTURE_* test settings. Never pass browser-fixture identities or RBBH_TEST_SQLSERVER into production. Database__ApplyMigrations is not an automatic migration switch; startup does not execute it. |

Current runtime secrets: API/client/admin credentials, SQL credentials, SMTP credentials when required, native client secret and shared-state/key encryption credentials. Inject through approved Secret stores, not image layers. There is no report-output directory setting: current exports are generated byte arrays, not persisted filesystem reports.

## CI/CD, containers and OCP

Exactly three root workflows remain: build-and-push-image.yml, codeql.yml, deploy-to-ocp.yml. All were read completely. PRs validate; manual/tag builds publish only after full .NET/SQL and six-role browser gates. No release-branch push trigger exists: use the intended manual/tag process. CodeQL runs PR/manual/weekly with explicit solution build; security-events write is scoped to analysis, other jobs use contents read. Fork PRs do not receive TEST_SQL_SA_PASSWORD and will fail the required SQL configuration check; approve a safe organization CI strategy instead of exposing secrets to untrusted PR code.

Web build context: Frontend, Dockerfile there. API context: repository root, Backend/Dockerfile, tests/local files excluded. Both are multi-stage .NET 10 builds, publish --no-restore, final aspnet runtime, non-root APP_UID, exec-form dotnet entrypoint, port 8080. Web relies on the official base HTTP-port default; OCP explicitly sets ASPNETCORE_URLS on both. No runtime/framework upgrade was made. Build caches are not runtime copies; no credential build ARG exists. Enterprise mirror availability/contents/APP_UID/ICU/timezone/cert compatibility require actual Linux image verification. Publish carries existing API compilation references by project design; they were not removed as a speculative optimization.

Images are distinct web/API names tagged sha-<full commit>. Publishing rejects existing tags/non-404 registry responses and serializes same-commit/component work. Registry owners must enforce immutability against other writers. Deployment validates full commit tag/digest and always pins @sha256. Concurrency serializes environment/component, cancel-in-progress=false; job timeouts and failure propagation remain enabled. Current image provenance is traceability to the supplied digest/build summary, not signed attestation.

GitHub environment variables: ARTIFACTORY_REGISTRY, ARTIFACTORY_REPOSITORY, WEB_IMAGE_NAME, API_IMAGE_NAME, OCP_SERVER, OCP_PROJECT_NAME, OCP_PULL_SECRET, **WEB_HEALTH_HOST, API_HEALTH_HOST**. Host values must be actual allowed hostnames for probes, not URLs. Secrets: ARTIFACTORY_USERNAME, ARTIFACTORY_TOKEN, TEST_SQL_SA_PASSWORD (disposable CI SQL only), OCP_TOKEN. Provision protected UAT/PROD approvals, approved runner/feed/registry networks, trusted oc CLI on rbbh-ghr-0, cluster TLS CA, namespace/pull secret, ca-certificates ConfigMap and mandatory <image-name>-config / <image-name>-secrets. No TLS bypass is used.

The generated Deployment has replicas=1 and defaults to RollingUpdate; Service maps 8080 to named http port. Even one replica may overlap old/new API processes during rollout; the period scheduler has no cross-replica coordination, so its overlap behavior requires an approved release strategy/rehearsal. Security context is runAsNonRoot, allowPrivilegeEscalation=false, drop ALL; CA mount is read-only. No resource limits/requests, explicit terminationGracePeriodSeconds, readOnlyRootFilesystem, seccomp or availability policy are set. Do not enable read-only root blindly before choosing key/temp storage. Owner must set resource/drain budgets, filesystem writes, token/volume policy, Route TLS/WebSocket/affinity, scrape access and egress. Workflow waits for pod rollout for five minutes; readiness checks the pod endpoint. Public route/SSO health is a separate post-deploy check, not currently automated by this workflow.

## Build, migrations and rollback runbook

1. Approve native cache/key-ring/proxy implementation and all environment values. Confirm real realm audience/claims/redirects, subject linkage, SQL/SMTP and CA chains. Complete Claude visual acceptance and the integrated final gate.
2. Clean checkout needs .NET SDK 10 (local verification: 10.0.401), approved NuGet access and the repository-local dotnet-ef 10.0.3 tool. Node 22/npm/Chromium are only for native Playwright, not runtime/product publish. No React/retired JS package tooling build remains. SQL Server/LocalDB is needed for disposable SQL tests; Docker/BuildKit and corporate mirror access for images; oc plus cluster access for deployment. Lack of SDK/transitive lock files is reproducibility debt; promote the tested image digest instead of rebuilding it per environment.
3. Restore/build/test and publish:

```powershell
dotnet restore RelatedPartiesRegister.slnx --configfile NuGet.config
dotnet build RelatedPartiesRegister.slnx -c Release --no-restore -p:RunAnalyzers=true
# RBBH_TEST_SQLSERVER must name a disposable test instance without Initial Catalog.
dotnet test RelatedPartiesRegister.slnx -c Release --no-build --no-restore
dotnet publish Backend/RelatedPartiesRegister.csproj -c Release --no-restore -o artifacts/publish/api
dotnet publish Frontend/RBBH.RelatedPartiesRegister.Web.csproj -c Release --no-restore -o artifacts/publish/web
docker build -f Backend/Dockerfile -t <local-api-tag> .
docker build -f Frontend/Dockerfile -t <local-web-tag> .
```

4. Migration model is **manual/controlled DB deployment**, never application startup. Review an idempotent EF script using dotnet tool restore / dotnet tool run dotnet-ef migrations script --idempotent with the actual project/context; submit through the central DB change workflow. Back up and rehearse restore, confirm __EFMigrationsHistory, seed/reference/role data, subject-link and historical global-period reconciliation. Do not run EF database update or F3 backfill automatically in this audit/deploy workflow. SQL connectivity alone does not prove schema readiness.
5. Deploy compatible API/native digests in the owner-approved order. Record previous and new digest, config/Secret versions, schema/migration state and protected approval. Wait for rollout, then verify /health/live, /health/ready, real public TLS/redirects, SSO/permissions, deep links, static assets, SQL and notifications. Configuration updates may need an explicit reviewed rollout; ConfigMap/Secret env changes are not automatically copied into an existing process.
6. Rollback re-dispatches deploy-to-ocp with the **previous accepted security baseline's** full commit tag and digest, not latest or a rebuilt tag. Ensure the registry retains it and re-check config/session/schema compatibility. The workflow does not down-migrate SQL. Subject-confirmation downgrade removes security metadata; earlier column/drop/data migrations can be destructive. Restoring an older unsafe identity image is not an acceptable rollback even if tables happen to match. No live rollback rehearsal was possible here.

## Hosting, errors, logs and shutdown

API liveness checks the process only; readiness checks SQL CanConnect, with failure 503. Both are anonymous and now expose status only. SMTP/SSO do not restart the process through liveness/startup. Native RBI health responses are minimal, but readiness currently has no application checks. Production auth without shared cache is confirmed fail-closed, not deployment-ready. Owner must address remote-proxy handling before relying on native HTTPS redirection/callbacks or probe results. Framework behavior references: [trusted forwarding](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0), [health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0), [Kubernetes probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/).

API production uses HSTS, nosniff, Referrer-Policy, frame protection and an API-only default-src self CSP. Native uses production HSTS and secure auth cookies through RBI; it has no application-wide CSP/header policy equivalent. Do not copy the API CSP onto Blazor scripts. A native nonce/hash policy and public TLS/header verification need separate compatibility evidence. No visual/markup/CSS change was made.

Active GlobalExceptionHandler returns safe production ProblemDetails/trace IDs, without developer page or raw exception text. Existing 401/403, 423 lock and 409 business conflicts remain covered by full tests. The alternate ProblemDetails configurator still has raw exception-message/inner-exception construction; the active handler writes its own safe response, but future use of that alternate path needs redaction review. This audit did not rewrite business response contracts.

API logs are structured console logs with correlation IDs and deliberate warning filtering; mutation traceability also lives in SQL audit. Tokens/passwords are not explicitly logged by reviewed paths. Some email failure logs include recipient addresses and exceptions; protect log access/retention and avoid raising sensitive-body diagnostics in production. Metrics need private scrape policy. Alerting/log collection are not configured by this repository; optional future telemetry can use approved infrastructure without introducing a new vendor here.

Static native hosting uses MapStaticAssets for framework/RBI assets and Razor deep links. Published Development smoke returned 200 for native /app, /app/profile, /app/physical-persons and an actual published RBI CSS endpoint. This proves artifact/deep-link delivery, not authenticated production acceptance. No application response-compression middleware or arbitrary subpath/PathBase support is configured; verify ingress compression/cache headers/public base path and invalidate assets by immutable release, not by copying old React fallback rules.

Exec-form ENTRYPOINT delivers container termination to dotnet. AutoLock's wait honors host cancellation; database/mail work after a check starts does not propagate cancellation throughout. Large exports/broadcasts can exceed default drain budgets. Select/measure an OCP termination budget and rehearse SIGTERM while requests run; local Windows child cleanup is not a Linux shutdown proof. No background/shutdown refactor was repeated.

## Secret audit and verification receipts

Current tracked-tree scan found no identified REAL_SECRET_RISK. The 17 assignment candidates were configuration reads, empty settings, GitHub Secret references, documentation placeholders (DEVELOPMENT_SAMPLE), or synthetic test credentials (TEST_FAKE). No private-key headers, known token signatures, credential URLs or tracked private certificates were found. This is a current-tree review, not a full Git-history/secret-rotation attestation. Existing ignored dotenv was not read or changed; ignore/container exclusions were expanded. Never paste or print secret values. Final publish excludes feed configs and private-key/local-secret/source files.

| Verification on 2026-10-28 | Result |
| --- | --- |
| Full .NET, Debug and fresh Release | 650 passed each, 0 failed/skipped: 278 unit + 162 integration + 210 Blazor |
| Backend subset | 440 passed, includes SQL below |
| SQL/LocalDB | 8 actual passes: prior seven safety/subject-link cases + new readiness connection case |
| Release/analyzer build | PASS, 0 warnings/errors |
| Backend/native Release publish | PASS; final folders api-final / web-final |
| Published local startup / live / ready | PASS in isolated Development only; both endpoints 200 |
| Production misconfiguration smoke | PASS_FAIL_CLOSED: API missing auth, native missing auth, native missing shared cache |
| Production authenticated startup, real SQL/SSO/mail/Route | NOT_RUN_INFRASTRUCTURE; shared-cache registration is also an explicit code blocker |
| Workflow YAML / embedded manifest / shell syntax | PASS: exactly 3 workflows, Deployment+Service, 19 bash -n scripts; not live GitHub execution or full actionlint |
| Docker build/run, Linux SIGTERM, live OCP rollout/rollback | NOT_RUN_INFRASTRUCTURE: Docker named pipe unavailable; no cluster credentials used |
| git diff --check | PASS |

Receipts: ignored artifacts/production-audit/{release-full,focused,startup-smoke.json,workflow-validation.json,rendered-deployment.fixture.yml}; baseline secret-location metadata under artifacts/backend-refactor/ops-secret-locations.json contains no values. No package upgrade, schema/backfill, backend service refactor, frontend layout/CSS/Razor or 325-whitespace cleanup was performed. Browser/Axe/responsive/theme acceptance is not claimed by this audit; the complete integrated gate remains after Claude's visual commits. Prior accepted commits and the existing untracked launchSettings.json remain intact. No push.

Local implementation commits: `6816ff8244652f4e8e8890fbc7d712951dc74f5c` (API startup/proxy/health guards and tests), `024d6ebcd3e49d1a622d768a3ef9592b21178e8d` (probes, job credentials, ignore/publish exclusions). This runbook and the retired deployment pointer/SQL startup clarification are committed separately afterward; the final documentation commit/HEAD is reported in the handoff.
