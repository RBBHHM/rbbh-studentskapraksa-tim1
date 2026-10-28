# Native pipeline — 2026-10-26

Only these workflow files remain anywhere in the repository:

- `.github/workflows/build-and-push-image.yml`
- `.github/workflows/codeql.yml`
- `.github/workflows/deploy-to-ocp.yml`

Build runs Release/analyzers/full .NET including disposable SQL tests and the
six-role native browser matrix (workers1/retries0). It rejects skipped SQL tests.
Pull requests validate only. Manual/tag runs publish separate web/API images
after validation; neither build context includes the React application runtime.
Both image contexts are the repository root; Dockerfiles are Frontend/Dockerfile and Backend/Dockerfile.
CodeQL scans C# with an explicit maintained-solution build and native JS/TS tests;
React reference/artifacts/generated dependencies are excluded from JS analysis.
TLS/security checks are not bypassed.

## Owner-provisioned configuration

GitHub UAT/PROD environments require protected review and approved network access:

- Variables ARTIFACTORY_REGISTRY, ARTIFACTORY_REPOSITORY, WEB_IMAGE_NAME,
  API_IMAGE_NAME. Web/API names must be distinct lowercase OCP-compatible names.
- Secrets ARTIFACTORY_USERNAME, ARTIFACTORY_TOKEN for publishing.
- TEST_SQL_SA_PASSWORD for an ephemeral CI SQL Server; no production DB connection.
- OCP_SERVER (HTTPS), OCP_PROJECT_NAME, OCP_PULL_SECRET; OCP_TOKEN secret.
  Runner rbbh-ghr-0 must have trusted cluster CA and approved oc CLI.
- Existing namespace pull secret, ca-certificates ConfigMap, and nonoptional
  `<image-name>-config` ConfigMap / `<image-name>-secrets` Secret.
  Runtime API/SSO/SQL/email/shared-state configuration is environment-owned.

All seven RBI runtime libraries are tracked source ProjectReferences. The root
NuGet.config restores only public third-party dependencies; an approved corporate
mirror may replace that source where required. Public runner network access and
registry/cluster credentials remain owner-provisioned. No credentials or live
deployment were used to verify these workflows locally.

## Immutable deployment and rollback

Successful build emits `image:sha-<40-character-commit>@sha256:<digest>`.
Publishing refuses an existing commit tag, fails on unexpected registry responses
and serializes same-commit/component jobs. Registry owners must also enforce
write-once tag policy against external writers; deployment always pins a digest.
Select web or api and supply the exact full commit tag plus digest from that
successful build. OCP deploy pins the digest, retains the full tag as an
annotation, and never uses latest, mutable ImageStream rollout or disabled TLS.
Only UAT/Production environments are supported. ConfigMaps/Secrets are mandatory.
Both native web and API use their existing /health/ready and /health/live
contracts; API readiness includes its actual database check. Real SQL/SSO
acceptance still requires separate integrated-environment tests.
Backend and web are separate deploys; owner controls compatibility and ordering.

Deployment does not apply EF migrations or identity backfill. Before switching
routes, approve subject-link/period reconciliation, real realm data, SQL/SMTP,
shared cache/DataProtection, proxy WebSockets and vulnerability feeds. Roll back
to a previously accepted digest only after checking schema compatibility; a
schema downgrade discards subject confirmation metadata and needs explicit review.

Removed: old artifact/deploy .yaml workflows, nested template workflows, root
Makefile, build/restore/test/dev-web .cmd wrappers, IIS deploy and candidate
publish/startup helpers. README now uses direct dotnet/docker commands.
After final Claude integration, the remaining the retired API wrapper and
scripts/testing/Test-LocalApi.ps1 were retired with their React package.json
consumer. No tracked files remain under root scripts/. No new wrapper was added.
The canonical solution is RelatedPartiesRegister.slnx; the temporary migration
name and old backend-only .sln were retired. Workflow and test-root discovery
references were updated together. Node/npm remains only for native Playwright.
